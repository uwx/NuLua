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
public abstract class LuaObject : ILuaObject
{
    readonly ILuaState state;
    readonly LuaReference reference;
    bool disposed;

    protected LuaObject(ILuaState state, LuaReference reference)
    {
        this.state = state;
        this.reference = reference;
    }

    public LuaReference Reference => reference;

    public void Dispose()
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
                // Defer to the state's owning thread instead of touching the native state from
                // whatever thread this runs on: a finalizer runs on the GC thread, and calling
                // lua_unref from there races the owner's VM execution and corrupts the stack.
                state.EnqueueUnref(reference);
            }
        }
        catch (ObjectDisposedException)
        {
            // The state was closed (lua_close) between the IsDisposed check and the enqueue,
            // so the registry is gone — there is nothing left to release.
        }

        GC.SuppressFinalize(this);
    }

    ~LuaObject()
    {
        // Last-resort release for callers that never disposed this wrapper explicitly.
        Dispose();
    }
}
