using System.Runtime.InteropServices;

namespace NuLua.Interop.Lua55;

public static unsafe partial class NativeMethods
{
    // lua_gc is declared as `int lua_gc(lua_State*, int, ...)` on Lua 5.5. bindgen drops the
    // variadic tail, so we add a sibling P/Invoke that exposes the `data` argument used by
    // LUA_GCSTEP and other parameterized operations.
    [DllImport(
        "lua55",
        EntryPoint = "lua_gc",
        CallingConvention = CallingConvention.Cdecl,
        ExactSpelling = true
    )]
    public static extern int lua_gc(lua_State* L, int what, int data);
}
