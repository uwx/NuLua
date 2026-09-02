using System.Diagnostics.CodeAnalysis;
using NuLua.Luau;

namespace NuLua;

public abstract class LuaModuleLoader
{
    public bool TryLoad(LuauState state, string argument)
    {
        var fullPath = AliasToPath(argument);
        var baseTop = state.GetTop();
        var thread = state.CreateThread();

        try
        {
            if (!TryLoadModule(thread, fullPath, argument))
            {
                state.SetTop(baseTop);
                return false;
            }

            thread.XMove(state, 1);
            state.Remove(-2);
            return true;
        }
        catch
        {
            state.SetTop(baseTop);
            throw;
        }
        finally
        {
            // CreateThread() registered the coroutine in the registry and the
            // state's child-state list; release that reference now that the
            // module has been moved back onto `state`'s stack.
            thread.Dispose();
        }
    }

    internal string ResolveCacheKey(string argument) => GetCacheKey(AliasToPath(argument));

    protected abstract bool TryLoadModule(LuauState state, string fullPath, string requireArgument);

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
