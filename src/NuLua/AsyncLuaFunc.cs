namespace NuLua;

public delegate ValueTask<int> AsyncLuaFunc<TState>(
    TState state,
    LuaFuncArguments args,
    CancellationToken cancellationToken
)
    where TState : ILuaState;
