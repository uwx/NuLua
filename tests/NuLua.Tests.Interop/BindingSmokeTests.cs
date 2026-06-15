extern alias Lua51;
extern alias Lua52;
extern alias Lua53;
extern alias Lua54;
extern alias Lua55;
extern alias LuaJit;
extern alias Luau;

using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace NuLua.Tests.Interop;

public class BindingSmokeTests
{
    [Test]
    public async Task Lua51BindingWorks()
    {
        var result = RunLua51();

        await Assert.That(result.StateCreated).IsTrue();
        await Assert.That(result.Value).IsEqualTo(42);
        await Assert.That(result.StackTop).IsEqualTo(1);
    }

    [Test]
    public async Task Lua52BindingWorks()
    {
        var result = RunLua52();

        await Assert.That(result.StateCreated).IsTrue();
        await Assert.That(result.IsNumber).IsEqualTo(1);
        await Assert.That(result.Value).IsEqualTo(42);
        await Assert.That(result.StackTop).IsEqualTo(1);
    }

    [Test]
    public async Task Lua53BindingWorks()
    {
        var result = RunLua53();

        await Assert.That(result.StateCreated).IsTrue();
        await Assert.That(result.IsNumber).IsEqualTo(1);
        await Assert.That(result.Value).IsEqualTo(42);
        await Assert.That(result.StackTop).IsEqualTo(1);
    }

    [Test]
    public async Task Lua54BindingWorks()
    {
        var result = RunLua54();

        await Assert.That(result.StateCreated).IsTrue();
        await Assert.That(result.IsNumber).IsEqualTo(1);
        await Assert.That(result.Value).IsEqualTo(42);
        await Assert.That(result.StackTop).IsEqualTo(1);
    }

    [Test]
    public async Task Lua55BindingWorks()
    {
        var result = RunLua55();

        await Assert.That(result.StateCreated).IsTrue();
        await Assert.That(result.IsNumber).IsEqualTo(1);
        await Assert.That(result.Value).IsEqualTo(42);
        await Assert.That(result.StackTop).IsEqualTo(1);
    }

    [Test]
    public async Task LuaJitBindingWorks()
    {
        var result = RunLuaJit();

        await Assert.That(result.StateCreated).IsTrue();
        await Assert.That(result.Value).IsEqualTo(42);
        await Assert.That(result.StackTop).IsEqualTo(1);
    }

    [Test]
    public async Task LuauBindingWorks()
    {
        var result = RunLuau();

        await Assert.That(result.StateCreated).IsTrue();
        await Assert.That(result.IsNumber).IsEqualTo(1);
        await Assert.That(result.Value).IsEqualTo(42);
        await Assert.That(result.StackTop).IsEqualTo(1);
    }

    private static unsafe BindingResult RunLua51()
    {
        var state = Lua51::NuLua.Interop.Lua51.NativeMethods.luaL_newstate();
        if (state is null)
        {
            return BindingResult.NotCreated;
        }

        try
        {
            Lua51::NuLua.Interop.Lua51.NativeMethods.lua_pushinteger(state, 42);
            return new BindingResult(true, 1, Lua51::NuLua.Interop.Lua51.NativeMethods.lua_tointeger(state, -1), Lua51::NuLua.Interop.Lua51.NativeMethods.lua_gettop(state));
        }
        finally
        {
            Lua51::NuLua.Interop.Lua51.NativeMethods.lua_close(state);
        }
    }

    private static unsafe BindingResult RunLua52()
    {
        var state = Lua52::NuLua.Interop.Lua52.NativeMethods.luaL_newstate();
        if (state is null)
        {
            return BindingResult.NotCreated;
        }

        try
        {
            Lua52::NuLua.Interop.Lua52.NativeMethods.lua_pushinteger(state, 42);
            var isNumber = 0;
            var value = Lua52::NuLua.Interop.Lua52.NativeMethods.lua_tointegerx(state, -1, &isNumber);
            return new BindingResult(true, isNumber, value, Lua52::NuLua.Interop.Lua52.NativeMethods.lua_gettop(state));
        }
        finally
        {
            Lua52::NuLua.Interop.Lua52.NativeMethods.lua_close(state);
        }
    }

    private static unsafe BindingResult RunLua53()
    {
        var state = Lua53::NuLua.Interop.Lua53.NativeMethods.luaL_newstate();
        if (state is null)
        {
            return BindingResult.NotCreated;
        }

        try
        {
            Lua53::NuLua.Interop.Lua53.NativeMethods.lua_pushinteger(state, 42);
            var isNumber = 0;
            var value = Lua53::NuLua.Interop.Lua53.NativeMethods.lua_tointegerx(state, -1, &isNumber);
            return new BindingResult(true, isNumber, value, Lua53::NuLua.Interop.Lua53.NativeMethods.lua_gettop(state));
        }
        finally
        {
            Lua53::NuLua.Interop.Lua53.NativeMethods.lua_close(state);
        }
    }

    private static unsafe BindingResult RunLua54()
    {
        var state = Lua54::NuLua.Interop.Lua54.NativeMethods.luaL_newstate();
        if (state is null)
        {
            return BindingResult.NotCreated;
        }

        try
        {
            Lua54::NuLua.Interop.Lua54.NativeMethods.lua_pushinteger(state, 42);
            var isNumber = 0;
            var value = Lua54::NuLua.Interop.Lua54.NativeMethods.lua_tointegerx(state, -1, &isNumber);
            return new BindingResult(true, isNumber, value, Lua54::NuLua.Interop.Lua54.NativeMethods.lua_gettop(state));
        }
        finally
        {
            Lua54::NuLua.Interop.Lua54.NativeMethods.lua_close(state);
        }
    }

    private static unsafe BindingResult RunLua55()
    {
        var state = Lua55::NuLua.Interop.Lua55.NativeMethods.luaL_newstate();
        if (state is null)
        {
            return BindingResult.NotCreated;
        }

        try
        {
            Lua55::NuLua.Interop.Lua55.NativeMethods.lua_pushinteger(state, 42);
            var isNumber = 0;
            var value = Lua55::NuLua.Interop.Lua55.NativeMethods.lua_tointegerx(state, -1, &isNumber);
            return new BindingResult(true, isNumber, value, Lua55::NuLua.Interop.Lua55.NativeMethods.lua_gettop(state));
        }
        finally
        {
            Lua55::NuLua.Interop.Lua55.NativeMethods.lua_close(state);
        }
    }

    private static unsafe BindingResult RunLuaJit()
    {
        var state = LuaJit::NuLua.Interop.LuaJit.NativeMethods.luaL_newstate();
        if (state is null)
        {
            return BindingResult.NotCreated;
        }

        try
        {
            LuaJit::NuLua.Interop.LuaJit.NativeMethods.lua_pushinteger(state, 42);
            return new BindingResult(true, 1, LuaJit::NuLua.Interop.LuaJit.NativeMethods.lua_tointeger(state, -1), LuaJit::NuLua.Interop.LuaJit.NativeMethods.lua_gettop(state));
        }
        finally
        {
            LuaJit::NuLua.Interop.LuaJit.NativeMethods.lua_close(state);
        }
    }

    private static unsafe BindingResult RunLuau()
    {
        var state = Luau::NuLua.Interop.Luau.NativeMethods.luaL_newstate();
        if (state is null)
        {
            return BindingResult.NotCreated;
        }

        try
        {
            Luau::NuLua.Interop.Luau.NativeMethods.lua_pushinteger(state, 42);
            var isNumber = 0;
            var value = Luau::NuLua.Interop.Luau.NativeMethods.lua_tointegerx(state, -1, &isNumber);
            return new BindingResult(true, isNumber, value, Luau::NuLua.Interop.Luau.NativeMethods.lua_gettop(state));
        }
        finally
        {
            Luau::NuLua.Interop.Luau.NativeMethods.lua_close(state);
        }
    }

    private readonly record struct BindingResult(bool StateCreated, int IsNumber, long Value, int StackTop)
    {
        public static BindingResult NotCreated { get; } = new(false, 0, 0, 0);
    }
}
