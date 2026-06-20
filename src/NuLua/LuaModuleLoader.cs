using System.Diagnostics.CodeAnalysis;

namespace NuLua;

public abstract class LuaModuleLoader
{
    const string CacheKey = "_NULUA_MODULES";

    public bool TryLoad(ILuaState state, string argument)
    {
        var cache = state[CacheKey];
        LuaTable cacheTable;

        if (cache.IsNil)
        {
            cacheTable = state.CreateTable();
            state[CacheKey] = cacheTable;
        }
        else
        {
            cacheTable = cache.Read<LuaTable>();
        }

        var fullPath = AliasToPath(argument);
        var cacheKey = GetCacheKey(fullPath);

        var cached = cacheTable[cacheKey];
        if (!cached.IsNil)
        {
            state.Push(cached);
            return true;
        }

        var baseTop = state.GetTop();
        var thread = state.CreateThread();

        if (!TryLoadModule(thread, fullPath, argument))
        {
            state.SetTop(baseTop);
            return false;
        }

        thread.XMove(state, 1);
        var moduleValue = state.ToLuaValue(-1);
        cacheTable[cacheKey] = moduleValue;
        state.SetTop(baseTop);
        state.Push(moduleValue);

        return true;
    }

    protected abstract bool TryLoadModule(ILuaState state, string fullPath, string requireArgument);

    protected abstract bool TryGetAliasPath(string alias, [NotNullWhen(true)] out string? path);

    protected virtual string GetCacheKey(string path) => path;

    string AliasToPath(string alias)
    {
        if (alias.Length <= 1 || alias[0] is not '@')
        {
            return alias;
        }

        var slashIndex = alias.IndexOf('/');
        var key = slashIndex == -1 ? alias[1..] : alias[1..slashIndex];

        if (!TryGetAliasPath(key, out var path))
        {
            return alias;
        }

        return slashIndex == -1 ? path : path + alias[slashIndex..];
    }
}
