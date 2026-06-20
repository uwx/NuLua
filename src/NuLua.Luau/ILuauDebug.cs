namespace NuLua.Luau;

public interface ILuauDebug : ILuaDebug<LuauState>
{
    int GetArgument(int level, int n);
    void SetSingleStep(bool enabled);
    int SetBreakpoint(int funcIndex, int line, bool enabled);
    string GetDebugTrace();
    void GetCoverage(int funcIndex, Action<LuauCoverageEntry> visit);
    void SetDebugBreakCallback(LuaHook<LuauState>? callback);
    void SetDebugStepCallback(LuaHook<LuauState>? callback);
    void SetDebugInterruptCallback(LuaHook<LuauState>? callback);
}
