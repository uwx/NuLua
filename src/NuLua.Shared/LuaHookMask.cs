namespace NuLua;

[Flags]
public enum LuaHookMask
{
    None = 0,
    Call = 1 << 0,
    Return = 1 << 1,
    Line = 1 << 2,
    Count = 1 << 3,
}
