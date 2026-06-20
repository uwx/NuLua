using NuLua.Interop.LuaJit;

namespace NuLua.LuaJit;

public enum LuaJitMode
{
    Engine = 0,
    Debug = 1,
    Func = 2,
    AllFunc = 3,
    AllSubFunc = 4,
    Trace = 5,
    WrapCFunc = 0x10,
}

[Flags]
public enum LuaJitModeFlag
{
    Off = 0x0000,
    On = 0x0100,
    Flush = 0x0200,
}

partial class LuaJitState
{
    public unsafe void OpenFfiLibrary()
    {
        CheckDisposed();
        NativeMethods.luaopen_ffi(ptr);
    }

    public unsafe void OpenBitLibrary()
    {
        CheckDisposed();
        OpenSingleLibrary(NativeMethods.luaopen_bit, "bit"u8);
    }

    public unsafe void OpenJitLibrary()
    {
        CheckDisposed();
        OpenSingleLibrary(NativeMethods.luaopen_jit, "jit"u8);
    }

    public unsafe void OpenStringBufferLibrary()
    {
        CheckDisposed();
        OpenSingleLibrary(NativeMethods.luaopen_string_buffer, "string.buffer"u8);
    }

    public unsafe bool TrySetJitMode(int index, LuaJitMode mode, LuaJitModeFlag flag)
    {
        CheckDisposed();
        return NativeMethods.luaJIT_setmode(ptr, index, (int)mode | (int)flag) != 0;
    }
}
