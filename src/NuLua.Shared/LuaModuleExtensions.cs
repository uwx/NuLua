namespace NuLua;

public static class LuaModuleExtensions
{
    public static void UseModuleLoader<TState>(this TState state, LuaModuleLoader loader)
        where TState : ILuaState<TState>
    {
        var function = state.CreateFunction(
            (lua, args) =>
            {
                var path = args[0].Read<string>();
                if (loader.TryLoad(lua, path))
                {
                    return 1;
                }

                throw new LuaException(2, $"module '{path}' not found");
            }
        );

        state["require"] = function;
    }

    public static void UseFileSystemModuleLoader<TState>(
        this TState state,
        string workingDirectory,
        string extension = ".lua",
        IReadOnlyDictionary<string, string>? aliases = null
    )
        where TState : ILuaState<TState>
    {
        var loader = new FileSystemLuaModuleLoader
        {
            WorkingDirectory = workingDirectory,
            Extension = extension,
            Aliases = aliases,
        };
        state.UseModuleLoader(loader);
    }
}
