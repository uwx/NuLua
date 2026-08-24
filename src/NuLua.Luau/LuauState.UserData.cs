using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NuLua.Interop.Luau;

namespace NuLua.Luau;

public sealed unsafe partial class LuauState
{
    /// <summary>
    /// Allocates a tagged userdata (payload size <paramref name="size"/>) with no metatable.
    /// Tags are 0..<c>LUA_UTAG_LIMIT</c>-1 (128); tag 0 is reserved for plain raw userdata.
    /// </summary>
    public unsafe void* NewUserDataTagged(int size, int tag)
    {
        CheckDisposed();
        return NativeMethods.lua_newuserdatatagged(ptr, (nuint)size, tag);
    }

    /// <summary>
    /// Allocates a tagged userdata whose metatable is the per-tag metatable registered via
    /// <see cref="SetUserDataMetatable"/> (must already be registered for <paramref name="tag"/>).
    /// </summary>
    public unsafe void* NewUserDataTaggedWithMetatable(int size, int tag)
    {
        CheckDisposed();
        return NativeMethods.lua_newuserdatataggedwithmetatable(ptr, (nuint)size, tag);
    }

    /// <summary>
    /// Registers the table on top of the stack as the one-time per-tag metatable for
    /// <paramref name="tag"/> (pops it). Reassignment for the same tag is not supported.
    /// </summary>
    public void SetUserDataMetatable(int tag)
    {
        CheckDisposed();
        NativeMethods.lua_setuserdatametatable(ptr, tag);
    }

    /// <summary>
    /// Pushes the per-tag metatable for <paramref name="tag"/>, if registered.
    /// Returns <see langword="false"/> (and restores the stack) when not registered.
    /// </summary>
    public bool TryGetUserDataMetatable(int tag, [NotNullWhen(true)] out LuaTable? metatable)
    {
        CheckDisposed();
        NativeMethods.lua_getuserdatametatable(ptr, tag);
        if (GetType(-1) == LuaValueType.Nil)
        {
            SetTop(GetTop() - 1);
            metatable = null;
            return false;
        }

        metatable = this.ToTable(-1);
        return true;
    }

    /// <summary>
    /// Registers a per-tag GC destructor, called by the VM immediately before freeing any
    /// userdata with this tag. The callback must NOT touch the Lua state.
    /// </summary>
    public void SetUserDataDtor(int tag, delegate* unmanaged[Cdecl]<lua_State*, void*, void> dtor)
    {
        CheckDisposed();
        NativeMethods.lua_setuserdatadtor(ptr, tag, dtor);
    }

    /// <summary>
    /// Registers the standard managed-object destructor (frees the GCHandle in the payload)
    /// for <paramref name="tag"/>. Used by <c>CreateUserData</c>.
    /// </summary>
    public void SetManagedUserDataDtor(int tag)
    {
        SetUserDataDtor(tag, &FreeGCHandle);
    }

    /// <summary>
    /// Frees the GCHandle stored in a managed-object userdata payload. Invoked by the VM during
    /// GC traversal; must never call back into the lua_State (only GCHandle operations).
    /// </summary>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    static void FreeGCHandle(lua_State* L, void* data)
    {
        _ = L;
        if (data == null)
        {
            return;
        }

        ref var header = ref Unsafe.AsRef<ManagedUserData>(data);
        if (header.Handle == 0)
        {
            return;
        }

        GCHandle.FromIntPtr(header.Handle).Free();
    }
}
