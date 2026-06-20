namespace NuLua;

public readonly struct LuaDebugInfo
{
    public string? Name { get; init; }
    public string? NameWhat { get; init; }
    public string? What { get; init; }
    public string? Source { get; init; }
    public string? ShortSource { get; init; }
    public int CurrentLine { get; init; }
    public int LineDefined { get; init; }
    public int LastLineDefined { get; init; }
    public int Upvalues { get; init; }
    public int Parameters { get; init; }
    public bool IsVararg { get; init; }
    public bool IsTailCall { get; init; }
    public int FirstTransferred { get; init; }
    public int TransferredCount { get; init; }
}
