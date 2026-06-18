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
    Type StateType,
    Func<ILuaState> CreateState,
    Func<ILuaState, LuaFunction> CreateAdderFunction,
    Action<ILuaState, LuaModuleLoader> UseModuleLoader,
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
            typeof(Lua51State),
            Lua51State.Create,
            CreateAdderFunction<Lua51State>,
            UseModuleLoader<Lua51State>,
            SupportsArithmetic: false,
            SupportsUserValuePayload: false
        );
        yield return new(
            "Lua 5.2",
            typeof(Lua52State),
            Lua52State.Create,
            CreateAdderFunction<Lua52State>,
            UseModuleLoader<Lua52State>,
            SupportsUserValuePayload: false
        );
        yield return new(
            "Lua 5.3",
            typeof(Lua53State),
            Lua53State.Create,
            CreateAdderFunction<Lua53State>,
            UseModuleLoader<Lua53State>
        );
        yield return new(
            "Lua 5.4",
            typeof(Lua54State),
            Lua54State.Create,
            CreateAdderFunction<Lua54State>,
            UseModuleLoader<Lua54State>
        );
        yield return new(
            "Lua 5.5",
            typeof(Lua55State),
            Lua55State.Create,
            CreateAdderFunction<Lua55State>,
            UseModuleLoader<Lua55State>
        );
        yield return new(
            "LuaJIT",
            typeof(LuaJitState),
            LuaJitState.Create,
            CreateAdderFunction<LuaJitState>,
            UseModuleLoader<LuaJitState>,
            SupportsArithmetic: false,
            SupportsUserValuePayload: false
        );
        yield return new(
            "Luau",
            typeof(LuauState),
            LuauState.Create,
            CreateAdderFunction<LuauState>,
            UseModuleLoader<LuauState>,
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

    static void UseModuleLoader<TState>(ILuaState state, LuaModuleLoader loader)
        where TState : ILuaState<TState>
    {
        ((TState)state).UseModuleLoader(loader);
    }
}
