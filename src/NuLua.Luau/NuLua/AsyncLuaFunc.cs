using NuLua.Luau;

namespace NuLua;

public delegate ValueTask<int> AsyncLuaFunc(
    LuauState state,
    LuaFuncArguments args,
    CancellationToken cancellationToken
);