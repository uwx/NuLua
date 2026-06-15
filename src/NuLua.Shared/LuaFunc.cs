namespace NuLua;

public delegate int LuaFunc<TState>(TState state, ReadOnlySpan<LuaValue> args)
    where TState : ILuaState;
