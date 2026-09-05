using NuLua.Interop.Luau;
using NuLua.Luau;

namespace NuLua;

/// <summary>
/// Base class for managed wrappers that own a Lua registry reference to a GC-managed object
/// (tables, functions, userdata, buffers, classes, ...).
/// </summary>
/// <remarks>
/// Calling <see cref="Dispose"/> releases the registry reference held by <see cref="Reference"/>.
/// A finalizer acts as a safety net: if the caller never disposes the wrapper, the reference is
/// still released as long as the owning <see cref="ILuaState"/> is still open. Once the state has
/// been closed (<c>lua_close</c>) the registry is gone, so disposal is a no-op.
/// </remarks>
public abstract class LuaObjectRef : ILuaObjectRef
{
    readonly LuauState state;
    readonly LuaReference reference;
    bool disposed;

    public int refCount;
    
#if DEBUG
    private readonly string _creationStackTrace;
#endif

    protected LuaObjectRef(LuauState state, LuaReference reference)
    {
        this.state = state;
        this.reference = reference;
        refCount = 1;
        
#if DEBUG
        _creationStackTrace = Environment.StackTrace;
#endif
    }

    public LuaReference Reference => reference;

    public void Free(bool isOnLuaThread = true)
    {
        if (disposed)
        {
            return;
        }
        disposed = true;

        try
        {
            // The reference lives in the Lua state's registry. If the state was already
            // disposed (ptr == null after lua_close), there is nothing left to unref.
            if (!state.IsDisposed)
            {
                if (!isOnLuaThread)
                {
                    // Defer to the state's owning thread instead of touching the native state from
                    // whatever thread this runs on: a finalizer runs on the GC thread, and calling
                    // lua_unref from there races the owner's VM execution and corrupts the stack.
                    state.EnqueueUnref(reference);
                }
                else
                {
                    state.Unref(reference);
                }
            }
        }
        catch (ObjectDisposedException)
        {
            // The state was closed (lua_close) between the IsDisposed check and the enqueue,
            // so the registry is gone — there is nothing left to release.
        }

#pragma warning disable CA1816
        GC.SuppressFinalize(this);
#pragma warning restore CA1816
    }

    void IDisposable.Dispose()
    {
        Return();
    }

    ~LuaObjectRef()
    {
        // Last-resort release for callers that never disposed this wrapper explicitly.
        Free(false);
        
#if DEBUG
        Console.WriteLine($"LuaObject leaked at {_creationStackTrace}.");
#else
        Console.WriteLine($"LuaObject leaked.");
#endif
    }

    public void Borrow()
    {
        if (disposed)
        {
            throw new ObjectDisposedException("Value has been freed.");
        }
        
        Interlocked.Increment(ref refCount);
    }

    public void Return()
    {
        if (Interlocked.Decrement(ref refCount) == 0)
        {
            Free();
        }
    }
    
    public override string ToString()
    {
        state.PushValue(Reference);
        var str = state.ToString(-1);
        state.Pop();
        return str;
    }
}
