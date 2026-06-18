using System.Diagnostics.CodeAnalysis;

namespace NuLua;

public sealed class InMemoryModuleLoader(
    IReadOnlyDictionary<string, string> modules,
    IReadOnlyDictionary<string, string>? aliases = null
) : LuaModuleLoader
{
    readonly IReadOnlyDictionary<string, string> modules = modules;
    readonly IReadOnlyDictionary<string, string>? aliases = aliases;

    protected override bool TryLoadModule(ILuaState state, string fullPath, string requireArgument)
    {
        if (!modules.TryGetValue(fullPath, out var source))
        {
            return false;
        }

        state.LoadString(source, requireArgument);
        return true;
    }

    protected override bool TryGetAliasPath(string alias, [NotNullWhen(true)] out string? path)
    {
        if (aliases is not null && aliases.TryGetValue(alias, out path))
        {
            return true;
        }

        path = null;
        return false;
    }
}
