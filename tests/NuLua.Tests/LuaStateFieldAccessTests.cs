namespace NuLua.Tests;

public class LuaStateFieldAccessTests
{
    [Test]
    [MethodDataSource(typeof(LuaStateCases), nameof(LuaStateCases.All))]
    public async Task ToIntegerReadsPushedIntegerValue(LuaStateCase lua)
    {
        using var state = lua.CreateState();

        state.PushInteger(42);
        await Assert.That(state.ToInteger(-1)).IsEqualTo(42L);

        state.PushInteger(-7);
        await Assert.That(state.ToInteger(-1)).IsEqualTo(-7L);
    }

    [Test]
    [MethodDataSource(typeof(LuaStateCases), nameof(LuaStateCases.All))]
    public async Task GetFieldAndSetFieldReadWriteTableMembers(LuaStateCase lua)
    {
        using var state = lua.CreateState();
        using var table = state.CreateTable();

        state.PushValue(table.Reference);
        state.PushString("value");
        state.SetField(-2, "key");

        state.GetField(-1, "key");
        await Assert.That(state.GetType(-1)).IsEqualTo(LuaValueType.String);
        await Assert.That(state.ToString(-1)).IsEqualTo("value");
        state.Pop(1);

        state.GetField(-1, "missing");
        await Assert.That(state.GetType(-1)).IsEqualTo(LuaValueType.Nil);
        state.Pop(2);
    }

    [Test]
    [MethodDataSource(typeof(LuaStateCases), nameof(LuaStateCases.All))]
    public async Task GetFieldOnGlobalsTableReadsStandardLibrary(LuaStateCase lua)
    {
        using var state = lua.CreateState();
        state.OpenLibraries();

        state.GetGlobal("string");
        await Assert.That(state.GetType(-1)).IsEqualTo(LuaValueType.Table);

        state.GetField(-1, "format");
        await Assert.That(state.GetType(-1)).IsEqualTo(LuaValueType.Function);
        state.Pop(2);
    }

    [Test]
    [MethodDataSource(typeof(LuaStateCases), nameof(LuaStateCases.All))]
    public async Task GetIAndSetIAccessArrayElementsByIntegerKeyWhenSupported(LuaStateCase lua)
    {
        if (lua.GetI is null || lua.SetI is null)
        {
            return;
        }

        using var state = lua.CreateState();
        using var table = state.CreateTable();

        state.PushValue(table.Reference);
        state.PushString("first");
        lua.SetI(state, -2, 1);
        state.PushString("second");
        lua.SetI(state, -2, 2);

        lua.GetI(state, -1, 1);
        await Assert.That(state.GetType(-1)).IsEqualTo(LuaValueType.String);
        await Assert.That(state.ToString(-1)).IsEqualTo("first");
        state.Pop(1);

        lua.GetI(state, -1, 2);
        await Assert.That(state.GetType(-1)).IsEqualTo(LuaValueType.String);
        await Assert.That(state.ToString(-1)).IsEqualTo("second");
        state.Pop(1);

        lua.GetI(state, -1, 99);
        await Assert.That(state.GetType(-1)).IsEqualTo(LuaValueType.Nil);
        state.Pop(2);
    }
}
