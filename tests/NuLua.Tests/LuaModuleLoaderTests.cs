using System.Diagnostics.CodeAnalysis;

namespace NuLua.Tests;

public class LuaModuleLoaderTests
{
    static string WriteModule(string directory, string name, string content)
    {
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, name);
        File.WriteAllText(path, content);
        return path;
    }

    [Test]
    [MethodDataSource(typeof(LuaStateCases), nameof(LuaStateCases.All))]
    public async Task RequireLoadsModuleFromFileSystem(LuaStateCase lua)
    {
        var directory = Path.Combine(Path.GetTempPath(), $"nulua-require-{Guid.NewGuid():N}");
        WriteModule(directory, "answer.lua", "return 42");

        try
        {
            using var state = lua.CreateState();
            state.OpenLibraries();

            var requirer = new FileSystemLuaModuleLoader { WorkingDirectory = directory };
            lua.UseModuleLoader(state, requirer);

            var results = state.DoString("return require('answer')");

            await Assert.That(results).Count().IsEqualTo(1);
            await Assert.That(results[0].Read<double>()).IsEqualTo(42);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Test]
    [MethodDataSource(typeof(LuaStateCases), nameof(LuaStateCases.All))]
    public async Task RequireCachesModuleResultAcrossCalls(LuaStateCase lua)
    {
        var directory = Path.Combine(Path.GetTempPath(), $"nulua-require-{Guid.NewGuid():N}");
        WriteModule(
            directory,
            "counter.lua",
            "local t = {}; t.n = (_G._loaded_count or 0) + 1; _G._loaded_count = t.n; return t"
        );

        try
        {
            using var state = lua.CreateState();
            state.OpenLibraries();

            var requirer = new FileSystemLuaModuleLoader { WorkingDirectory = directory };
            lua.UseModuleLoader(state, requirer);

            var results = state.DoString(
                "local a = require('counter'); local b = require('counter'); return a == b, _G._loaded_count"
            );

            await Assert.That(results[0].Read<bool>()).IsTrue();
            await Assert.That(results[1].Read<double>()).IsEqualTo(1);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Test]
    [MethodDataSource(typeof(LuaStateCases), nameof(LuaStateCases.All))]
    public async Task RequireResolvesAliases(LuaStateCase lua)
    {
        var directory = Path.Combine(Path.GetTempPath(), $"nulua-require-{Guid.NewGuid():N}");
        var aliasDirectory = Path.Combine(directory, "lib");
        WriteModule(aliasDirectory, "hello.lua", "return 'hello'");

        try
        {
            using var state = lua.CreateState();
            state.OpenLibraries();

            var requirer = new FileSystemLuaModuleLoader
            {
                WorkingDirectory = directory,
                Aliases = new Dictionary<string, string> { ["lib"] = aliasDirectory },
            };
            lua.UseModuleLoader(state, requirer);

            var results = state.DoString("return require('@lib/hello')");

            await Assert.That(results[0].Read<string>()).IsEqualTo("hello");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    sealed class InMemoryRequirer(Dictionary<string, string> sources) : LuaModuleLoader
    {
        readonly Dictionary<string, string> sources = sources;

        protected override bool TryLoadModule(
            ILuaState state,
            string fullPath,
            string requireArgument
        )
        {
            if (!sources.TryGetValue(fullPath, out var source))
            {
                return false;
            }

            state.LoadString(source, fullPath);
            state.Call(0, 1);
            return true;
        }

        protected override bool TryGetAliasPath(string alias, [NotNullWhen(true)] out string? path)
        {
            path = null;
            return false;
        }
    }

    [Test]
    [MethodDataSource(typeof(LuaStateCases), nameof(LuaStateCases.All))]
    public async Task CustomRequirerSubclassIsInvoked(LuaStateCase lua)
    {
        using var state = lua.CreateState();
        state.OpenLibraries();

        var requirer = new InMemoryRequirer(
            new Dictionary<string, string> { ["greeting"] = "return 'hi from memory'" }
        );
        lua.UseModuleLoader(state, requirer);

        var results = state.DoString("return require('greeting')");

        await Assert.That(results[0].Read<string>()).IsEqualTo("hi from memory");
    }
}
