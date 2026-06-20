namespace NuLua;

public delegate void LuaHook(ILuaState state, LuaHookEvent hookEvent, int currentLine);
public delegate void LuaHook<TState>(TState state, LuaHookEvent hookEvent, int currentLine)
    where TState : ILuaState;
