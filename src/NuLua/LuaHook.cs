namespace NuLua;

public delegate void LuaHook<TState>(TState state, LuaHookEvent ev, int currentLine)
    where TState : ILuaState;
