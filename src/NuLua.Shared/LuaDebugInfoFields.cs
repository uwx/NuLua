namespace NuLua;

[Flags]
public enum LuaDebugInfoFields
{
    None = 0,
    Name = 1 << 0,
    Source = 1 << 1,
    CurrentLine = 1 << 2,
    Upvalues = 1 << 3,
    TailCall = 1 << 4,
    Function = 1 << 5,
    Lines = 1 << 6,
    Transfers = 1 << 7,
    All = Name | Source | CurrentLine | Upvalues | TailCall | Transfers,
}
