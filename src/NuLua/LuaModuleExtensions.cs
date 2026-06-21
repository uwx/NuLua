namespace NuLua;

public static class LuaModuleExtensions
{
    const string FallbackCacheKey = "_NULUA_MODULES";

    public static void UseModuleLoader<TState>(this TState state, LuaModuleLoader loader)
        where TState : ILuaState<TState>
    {
        if (TryRegisterSearcher(state, loader))
        {
            return;
        }

        UseRequireReplacement(state, loader);
    }

    static bool TryRegisterSearcher<TState>(TState state, LuaModuleLoader loader)
        where TState : ILuaState<TState>
    {
        var baseTop = state.GetTop();
        state.GetGlobal("package");
        if (state.GetType(-1) != LuaValueType.Table)
        {
            state.SetTop(baseTop);
            return false;
        }

        state.GetField(-1, "searchers");
        if (state.GetType(-1) != LuaValueType.Table)
        {
            state.SetTop(state.GetTop() - 1);
            state.GetField(-1, "loaders");
            if (state.GetType(-1) != LuaValueType.Table)
            {
                state.SetTop(baseTop);
                return false;
            }
        }

        state.PushValue(-1);
        var searchersRef = state.Ref();
        state.SetTop(baseTop);

        var searchers = new LuaTable(state, searchersRef);

        try
        {
            using var searcher = state.CreateFunction(
                (lua, args) =>
                {
                    var name = args[0].Read<string>();
                    if (!loader.TryLoad(lua, name))
                    {
                        lua.PushString($"\n\tno module '{name}' from custom loader");
                        return 1;
                    }

                    var moduleValue = lua.ToLuaValue(-1);
                    lua.NewFunction(
                        (inner, _) =>
                        {
                            inner.Push(moduleValue);
                            return 1;
                        },
                        0
                    );
                    return 1;
                }
            );

            searchers[searchers.Length + 1] = LuaValue.FromFunction(searcher);
        }
        finally
        {
            searchers.Dispose();
        }

        return true;
    }

    static void UseRequireReplacement<TState>(TState state, LuaModuleLoader loader)
        where TState : ILuaState<TState>
    {
        var requireFn = state.CreateFunction(
            (lua, args) =>
            {
                var name = args[0].Read<string>();

                var cacheValue = lua[FallbackCacheKey];
                LuaTable cacheTable;
                if (cacheValue.IsNil)
                {
                    cacheTable = lua.CreateTable();
                    lua[FallbackCacheKey] = cacheTable;
                }
                else
                {
                    cacheTable = cacheValue.Read<LuaTable>();
                }

                var cacheKey = loader.ResolveCacheKey(name);
                var cached = cacheTable[cacheKey];
                if (!cached.IsNil)
                {
                    lua.Push(cached);
                    return 1;
                }

                if (!loader.TryLoad(lua, name))
                {
                    throw new LuaException(2, $"module '{name}' not found");
                }

                var moduleValue = lua.ToLuaValue(-1);
                cacheTable[cacheKey] = moduleValue;
                lua.Push(moduleValue);
                return 1;
            }
        );

        state["require"] = requireFn;
    }
}
