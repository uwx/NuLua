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
    Func<ILuaState, string, LuaFunction> CreateUpvalueReaderFunction,
    Func<ILuaState, AsyncLuaFunc<ILuaState>, LuaFunction> CreateAsyncFunction,
    Action<ILuaState, LuaModuleLoader> UseModuleLoader,
    Action<ILuaState, int, long>? GetI = null,
    Action<ILuaState, int, long>? SetI = null,
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
            CreateUpvalueReaderFunction<Lua51State>,
            CreateAsyncFunction<Lua51State>,
            UseModuleLoader<Lua51State>,
            GetI: (s, i, n) => ((Lua51State)s).GetI(i, n),
            SetI: (s, i, n) => ((Lua51State)s).SetI(i, n),
            SupportsUserValuePayload: false
        );
        yield return new(
            "Lua 5.2",
            typeof(Lua52State),
            Lua52State.Create,
            CreateAdderFunction<Lua52State>,
            CreateUpvalueReaderFunction<Lua52State>,
            CreateAsyncFunction<Lua52State>,
            UseModuleLoader<Lua52State>,
            GetI: (s, i, n) => ((Lua52State)s).GetI(i, n),
            SetI: (s, i, n) => ((Lua52State)s).SetI(i, n),
            SupportsUserValuePayload: false
        );
        yield return new(
            "Lua 5.3",
            typeof(Lua53State),
            Lua53State.Create,
            CreateAdderFunction<Lua53State>,
            CreateUpvalueReaderFunction<Lua53State>,
            CreateAsyncFunction<Lua53State>,
            UseModuleLoader<Lua53State>,
            GetI: (s, i, n) => ((Lua53State)s).GetI(i, n),
            SetI: (s, i, n) => ((Lua53State)s).SetI(i, n)
        );
        yield return new(
            "Lua 5.4",
            typeof(Lua54State),
            Lua54State.Create,
            CreateAdderFunction<Lua54State>,
            CreateUpvalueReaderFunction<Lua54State>,
            CreateAsyncFunction<Lua54State>,
            UseModuleLoader<Lua54State>,
            GetI: (s, i, n) => ((Lua54State)s).GetI(i, n),
            SetI: (s, i, n) => ((Lua54State)s).SetI(i, n)
        );
        yield return new(
            "Lua 5.5",
            typeof(Lua55State),
            Lua55State.Create,
            CreateAdderFunction<Lua55State>,
            CreateUpvalueReaderFunction<Lua55State>,
            CreateAsyncFunction<Lua55State>,
            UseModuleLoader<Lua55State>,
            GetI: (s, i, n) => ((Lua55State)s).GetI(i, n),
            SetI: (s, i, n) => ((Lua55State)s).SetI(i, n)
        );
        yield return new(
            "LuaJIT",
            typeof(LuaJitState),
            LuaJitState.Create,
            CreateAdderFunction<LuaJitState>,
            CreateUpvalueReaderFunction<LuaJitState>,
            CreateAsyncFunction<LuaJitState>,
            UseModuleLoader<LuaJitState>,
            GetI: (s, i, n) => ((LuaJitState)s).GetI(i, n),
            SetI: (s, i, n) => ((LuaJitState)s).SetI(i, n),
            SupportsUserValuePayload: false
        );
        yield return new(
            "Luau",
            typeof(LuauState),
            LuauState.Create,
            CreateAdderFunction<LuauState>,
            CreateUpvalueReaderFunction<LuauState>,
            CreateAsyncFunction<LuauState>,
            UseModuleLoader<LuauState>,
            GetI: (s, i, n) => ((LuauState)s).GetI(i, n),
            SetI: (s, i, n) => ((LuauState)s).SetI(i, n),
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

    static LuaFunction CreateUpvalueReaderFunction<TState>(ILuaState state, string value)
        where TState : ILuaState<TState>
    {
        var typedState = (TState)state;
        typedState.PushString(value);
        return typedState.CreateFunction(
            (lua, _) =>
            {
                lua.PushString(lua.ToString(lua.GetUpvalueIndex(2)));
                return 1;
            },
            upvalueCount: 1
        );
    }

    static LuaFunction CreateAsyncFunction<TState>(
        ILuaState state,
        AsyncLuaFunc<ILuaState> function
    )
        where TState : ILuaState<TState>
    {
        return ((TState)state).CreateFunction((lua, args, ct) => function(lua, args, ct));
    }

    static void UseModuleLoader<TState>(ILuaState state, LuaModuleLoader loader)
        where TState : ILuaState<TState>
    {
        ((TState)state).UseModuleLoader(loader);
    }
}
