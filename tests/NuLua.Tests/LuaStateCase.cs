using NuLua.Lua51;
using NuLua.Lua52;
using NuLua.Lua53;
using NuLua.Lua54;
using NuLua.Lua55;
using NuLua.LuaJit;
using NuLua.Luau;

namespace NuLua.Tests;

public sealed record LuaStateCase(
    string Name,
    Func<ILuaState> CreateState,
    Func<ILuaState, LuaFunction> CreateAdderFunction,
    bool SupportsArithmetic = true,
    bool SupportsDump = true,
    bool SupportsUserValuePayload = true
)
{
    public override string ToString() => Name;
}

public static class LuaStateCases
{
    public static IEnumerable<LuaStateCase> All()
    {
        yield return new(
            "Lua 5.1",
            Lua51State.Create,
            CreateAdderFunction<Lua51State>,
            SupportsArithmetic: false,
            SupportsUserValuePayload: false
        );
        yield return new(
            "Lua 5.2",
            Lua52State.Create,
            CreateAdderFunction<Lua52State>,
            SupportsUserValuePayload: false
        );
        yield return new("Lua 5.3", Lua53State.Create, CreateAdderFunction<Lua53State>);
        yield return new("Lua 5.4", Lua54State.Create, CreateAdderFunction<Lua54State>);
        yield return new("Lua 5.5", Lua55State.Create, CreateAdderFunction<Lua55State>);
        yield return new(
            "LuaJIT",
            LuaJitState.Create,
            CreateAdderFunction<LuaJitState>,
            SupportsArithmetic: false,
            SupportsUserValuePayload: false
        );
        yield return new(
            "Luau",
            LuauState.Create,
            CreateAdderFunction<LuauState>,
            SupportsArithmetic: false,
            SupportsDump: false,
            SupportsUserValuePayload: false
        );
    }

    static LuaFunction CreateAdderFunction<TState>(ILuaState state)
        where TState : ILuaState<TState>
    {
        return ((TState)state).CreateFunction(
            (lua, args) =>
            {
                lua.PushNumber(args[0].Read<double>() + args[1].Read<double>());
                return 1;
            }
        );
    }
}
