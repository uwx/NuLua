namespace NuLua.Tests;

public class LuaStateTests
{
    static int CountCachedStates(LuaStateCase lua)
    {
        var cache = lua.StateType.GetField(
            "ptrToState",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static
        )!.GetValue(null)!;
        return (int)cache.GetType().GetProperty("Count")!.GetValue(cache)!;
    }

    [Test]
    [MethodDataSource(typeof(LuaStateCases), nameof(LuaStateCases.All))]
    public async Task NewStateStartsWithEmptyStack(LuaStateCase lua)
    {
        using var state = lua.CreateState();

        await Assert.That(state.AsPointer()).IsNotEqualTo(0);
        await Assert.That(state.GetTop()).IsEqualTo(0);
    }

    [Test]
    [MethodDataSource(typeof(LuaStateCases), nameof(LuaStateCases.All))]
    public async Task PushAndPopRoundTripsPrimitiveValues(LuaStateCase lua)
    {
        using var state = lua.CreateState();

        state.PushNil();
        state.PushBoolean(true);
        state.PushInteger(42);
        state.PushNumber(3.5);
        state.PushString("hello");

        await Assert.That(state.GetTop()).IsEqualTo(5);
        await Assert.That(state.GetType(1)).IsEqualTo(LuaValueType.Nil);
        await Assert.That(state.ToBoolean(2)).IsTrue();
        await Assert.That(state.ToNumber(3)).IsEqualTo(42);
        await Assert.That(state.ToNumber(4)).IsEqualTo(3.5);
        await Assert.That(state.ToString(5)).IsEqualTo("hello");

        var value = state.Pop();

        await Assert.That(value.Type).IsEqualTo(LuaValueType.String);
        await Assert.That(value.Read<string>()).IsEqualTo("hello");
        await Assert.That(state.GetTop()).IsEqualTo(4);
    }

    [Test]
    [MethodDataSource(typeof(LuaStateCases), nameof(LuaStateCases.All))]
    public async Task DoStringReturnsValuesAndRestoresStack(LuaStateCase lua)
    {
        using var state = lua.CreateState();

        var results = state.DoString("return 20 + 22, 'ok', true");

        await Assert.That(results).Count().IsEqualTo(3);
        await Assert.That(results[0].Read<double>()).IsEqualTo(42);
        await Assert.That(results[1].Read<string>()).IsEqualTo("ok");
        await Assert.That(results[2].Read<bool>()).IsTrue();
        await Assert.That(state.GetTop()).IsEqualTo(0);
    }

    [Test]
    [MethodDataSource(typeof(LuaStateCases), nameof(LuaStateCases.All))]
    public async Task GlobalsCanBeReadAndWrittenThroughIndexer(LuaStateCase lua)
    {
        using var state = lua.CreateState();

        state["answer"] = LuaValue.FromNumber(42);
        var value = state["answer"];

        await Assert.That(value.Type).IsEqualTo(LuaValueType.Number);
        await Assert.That(value.Read<double>()).IsEqualTo(42);
        await Assert.That(state.GetTop()).IsEqualTo(0);
    }

    [Test]
    [MethodDataSource(typeof(LuaStateCases), nameof(LuaStateCases.All))]
    public async Task StackHelpersCopyRotateInsertRemoveAndReplaceValues(LuaStateCase lua)
    {
        using var state = lua.CreateState();

        state.PushString("a");
        state.PushString("b");
        state.PushString("c");

        await Assert.That(state.GetAbsIndex(-1)).IsEqualTo(3);

        state.Copy(1, 3);
        await Assert.That(state.ToString(3)).IsEqualTo("a");

        state.Rotate(1, 1);
        await Assert.That(state.ToString(1)).IsEqualTo("a");
        await Assert.That(state.ToString(2)).IsEqualTo("a");
        await Assert.That(state.ToString(3)).IsEqualTo("b");

        state.PushString("inserted");
        state.Insert(2);
        await Assert.That(state.ToString(2)).IsEqualTo("inserted");
        await Assert.That(state.GetTop()).IsEqualTo(4);

        state.Remove(2);
        await Assert.That(state.ToString(2)).IsEqualTo("a");
        await Assert.That(state.GetTop()).IsEqualTo(3);

        state.PushString("replacement");
        state.Replace(1);
        await Assert.That(state.ToString(1)).IsEqualTo("replacement");
        await Assert.That(state.GetTop()).IsEqualTo(3);
    }

    [Test]
    [MethodDataSource(typeof(LuaStateCases), nameof(LuaStateCases.All))]
    public async Task PushLightUserDataRoundTripsPointer(LuaStateCase lua)
    {
        using var state = lua.CreateState();

        state.PushLightUserData(0x1234);

        await Assert.That(state.GetType(-1)).IsEqualTo(LuaValueType.LightUserData);
        await Assert.That(state.ToUserDataPointer(-1)).IsEqualTo(0x1234);
    }

    [Test]
    [MethodDataSource(typeof(LuaStateCases), nameof(LuaStateCases.All))]
    public async Task CheckStackReturnsTrueForSmallAdditionalCapacity(LuaStateCase lua)
    {
        using var state = lua.CreateState();

        await Assert.That(state.CheckStack(8)).IsTrue();
    }

    [Test]
    [MethodDataSource(typeof(LuaStateCases), nameof(LuaStateCases.All))]
    public async Task TableIndexesLengthEnumerationAndRawAccessWork(LuaStateCase lua)
    {
        using var state = lua.CreateState();
        using var table = state.CreateTable();

        table[1] = "one";
        table[2] = "two";
        table["name"] = "table";

        await Assert.That(table.Length).IsEqualTo(2);
        await Assert.That(table[1].Read<string>()).IsEqualTo("one");
        await Assert.That(table["name"].Read<string>()).IsEqualTo("table");

        var entries = new Dictionary<string, string>();
        foreach (var (key, value) in table)
        {
            entries[key.ToString()] = value.ToString();
        }

        await Assert.That(entries["1"]).IsEqualTo("one");
        await Assert.That(entries["2"]).IsEqualTo("two");
        await Assert.That(entries["name"]).IsEqualTo("table");

        state.PushValue(table.Reference);
        state.PushInteger(1);
        await Assert.That(state.RawGet(-2)).IsEqualTo(LuaValueType.String);
        await Assert.That(state.ToString(-1)).IsEqualTo("one");
        state.Pop(1);

        state.PushInteger(3);
        state.PushString("three");
        state.RawSet(-3);
        await Assert.That(table[3].Read<string>()).IsEqualTo("three");
        await Assert.That(state.RawLen(-1)).IsEqualTo(3);
        state.Pop(1);
    }

    [Test]
    [MethodDataSource(typeof(LuaStateCases), nameof(LuaStateCases.All))]
    public async Task ReferencesCanBePushedAndUnreferenced(LuaStateCase lua)
    {
        using var state = lua.CreateState();

        state.PushString("referenced");
        var reference = state.Ref();
        await Assert.That(state.GetTop()).IsEqualTo(0);

        state.PushValue(reference);
        await Assert.That(state.ToString(-1)).IsEqualTo("referenced");
        state.Pop(1);

        state.Unref(reference);
    }

    [Test]
    [MethodDataSource(typeof(LuaStateCases), nameof(LuaStateCases.All))]
    public async Task RawEqualComparesStackValues(LuaStateCase lua)
    {
        using var state = lua.CreateState();

        state.PushString("same");
        state.PushString("same");
        state.PushString("different");

        await Assert.That(state.RawEqual(1, 2)).IsTrue();
        await Assert.That(state.RawEqual(1, 3)).IsFalse();
    }

    [Test]
    [MethodDataSource(typeof(LuaStateCases), nameof(LuaStateCases.All))]
    public async Task MetatablesCanBeAttachedAndCleared(LuaStateCase lua)
    {
        using var state = lua.CreateState();
        using var table = state.CreateTable();
        using var metatable = state.CreateTable();

        metatable["tag"] = "meta";
        state.PushValue(table.Reference);
        state.SetMetatable(-1, metatable);

        var hasMetatable = state.TryGetMetatable(-1, out var actual);

        await Assert.That(hasMetatable).IsTrue();
        await Assert.That(actual).IsNotNull();
        await Assert.That(actual!["tag"].Read<string>()).IsEqualTo("meta");

        state.SetMetatable(-1, null);

        await Assert.That(state.TryGetMetatable(-1, out _)).IsFalse();
        state.Pop(1);
        actual?.Dispose();
    }

    [Test]
    [MethodDataSource(typeof(LuaStateCases), nameof(LuaStateCases.All))]
    public async Task UserDataSupportsSizeMemoryAndUserValues(LuaStateCase lua)
    {
        using var state = lua.CreateState();
        using var userData = state.CreateUserData(sizeof(int), userValueCount: 1);

        userData.Write(123);

        await Assert.That(userData.Size).IsEqualTo(sizeof(int));
        await Assert.That(userData.Read<int>()).IsEqualTo(123);

        if (lua.SupportsUserValuePayload)
        {
            var userValues = userData.UserValue;
            userValues[1] = "payload";

            await Assert.That(userValues[1].Read<string>()).IsEqualTo("payload");
        }
    }

    [Test]
    [MethodDataSource(typeof(LuaStateCases), nameof(LuaStateCases.All))]
    public async Task ThreadsCanBeCreatedPushedAndMovedBetween(LuaStateCase lua)
    {
        using var state = lua.CreateState();
        using var thread = state.CreateThread();

        await Assert.That(state.GetType(-1)).IsEqualTo(LuaValueType.Thread);
        await Assert.That(thread.From).IsSameReferenceAs(state);

        thread.PushString("moved");
        thread.XMove(state, 1);

        await Assert.That(state.ToString(-1)).IsEqualTo("moved");
        await Assert.That(thread.GetTop()).IsEqualTo(0);
        await Assert.That(state.PushThread()).IsTrue();
    }

    [Test]
    [MethodDataSource(typeof(LuaStateCases), nameof(LuaStateCases.All))]
    public async Task DisposedThreadStateIsRemovedFromCache(LuaStateCase lua)
    {
        using var state = lua.CreateState();
        var thread = state.CreateThread();
        var threadPointer = thread.AsPointer();

        await Assert.That(threadPointer).IsNotEqualTo(0);

        thread.Dispose();

        await Assert.That(thread.AsPointer()).IsEqualTo(0);

        var recreated = state.ToThread(-1);

        await Assert.That(recreated.AsPointer()).IsEqualTo(threadPointer);
        await Assert.That(recreated).IsNotSameReferenceAs(thread);

        recreated.Dispose();
    }

    [Test]
    [NotInParallel]
    [MethodDataSource(typeof(LuaStateCases), nameof(LuaStateCases.All))]
    public async Task DisposingMainStateRemovesThreadStatesFromCache(LuaStateCase lua)
    {
        var initialCount = CountCachedStates(lua);

        var state = lua.CreateState();
        var childState = state.CreateThread();
        _ = childState.CreateThread();

        await Assert.That(CountCachedStates(lua)).IsEqualTo(initialCount + 3);

        state.Dispose();

        await Assert.That(CountCachedStates(lua)).IsEqualTo(initialCount);
    }

    [Test]
    [MethodDataSource(typeof(LuaStateCases), nameof(LuaStateCases.All))]
    public async Task ComparePushesBooleanResult(LuaStateCase lua)
    {
        using var state = lua.CreateState();

        state.PushNumber(1);
        state.PushNumber(2);
        state.Compare(LuaComparisonOperator.Less);

        await Assert.That(state.GetType(-1)).IsEqualTo(LuaValueType.Boolean);
        await Assert.That(state.ToBoolean(-1)).IsTrue();
    }

    [Test]
    [MethodDataSource(typeof(LuaStateCases), nameof(LuaStateCases.All))]
    public async Task ArithmeticExtensionsReturnExpectedResultsWhenSupported(LuaStateCase lua)
    {
        using var state = lua.CreateState();

        if (!lua.SupportsArithmetic)
        {
            await Assert.That(() => state.Add(1, 2)).Throws<NotSupportedException>();
            return;
        }

        await Assert.That(state.Add(20, 22).Read<double>()).IsEqualTo(42);
        await Assert.That(state.Sub(50, 8).Read<double>()).IsEqualTo(42);
        await Assert.That(state.Mul(6, 7).Read<double>()).IsEqualTo(42);
        await Assert.That(state.Div(84, 2).Read<double>()).IsEqualTo(42);
        await Assert.That(state.Mod(44, 43).Read<double>()).IsEqualTo(1);
        await Assert.That(state.Pow(2, 5).Read<double>()).IsEqualTo(32);
        await Assert.That(state.Unm(42).Read<double>()).IsEqualTo(-42);
        await Assert.That(state.BAnd(0b1100, 0b1010).Read<double>()).IsEqualTo(0b1000);
        await Assert.That(state.BOr(0b1100, 0b1010).Read<double>()).IsEqualTo(0b1110);
        await Assert.That(state.BXor(0b1100, 0b1010).Read<double>()).IsEqualTo(0b0110);
        await Assert.That(state.Shl(3, 4).Read<double>()).IsEqualTo(48);
        await Assert.That(state.Shr(48, 4).Read<double>()).IsEqualTo(3);
    }

    [Test]
    [MethodDataSource(typeof(LuaStateCases), nameof(LuaStateCases.All))]
    public async Task LengthConcatAndComparisonExtensionsWork(LuaStateCase lua)
    {
        using var state = lua.CreateState();

        await Assert.That(state.Len("hello").Read<double>()).IsEqualTo(5);
        await Assert.That(state.Concat("he", "llo").Read<string>()).IsEqualTo("hello");
        await Assert.That(state.Equals("x", "x")).IsTrue();
        await Assert.That(state.LessThan(1, 2)).IsTrue();
        await Assert.That(state.LessThanOrEqual(2, 2)).IsTrue();
    }

    [Test]
    [MethodDataSource(typeof(LuaStateCases), nameof(LuaStateCases.All))]
    public async Task LoadStringCallAndDumpWork(LuaStateCase lua)
    {
        using var state = lua.CreateState();

        state.LoadString("return 42", "chunk");
        await Assert.That(state.GetType(-1)).IsEqualTo(LuaValueType.Function);

        if (lua.SupportsDump)
        {
            var dump = state.Dump(-1, strip: false);
            await Assert.That(dump.Length).IsGreaterThan(0);
        }
        else
        {
            await Assert.That(() => state.Dump(-1, strip: false)).Throws<NotSupportedException>();
        }

        state.Call(0, 1);
        await Assert.That(state.ToNumber(-1)).IsEqualTo(42);
    }

    [Test]
    [MethodDataSource(typeof(LuaStateCases), nameof(LuaStateCases.All))]
    public async Task OpenLibrariesMakesStandardGlobalsAvailable(LuaStateCase lua)
    {
        using var state = lua.CreateState();

        state.OpenLibraries();
        var results = state.DoString("return type(print), type(math)");

        await Assert.That(results[0].Read<string>()).IsEqualTo("function");
        await Assert.That(results[1].Read<string>()).IsEqualTo("table");
    }

    [Test]
    [MethodDataSource(typeof(LuaStateCases), nameof(LuaStateCases.All))]
    public async Task LuaFunctionWrapperInvokesReferencedFunction(LuaStateCase lua)
    {
        using var state = lua.CreateState();

        state.LoadString("called = ...", "set-called");
        var function = state.ToLuaValue(-1).Read<LuaFunction>();
        state.Pop(1);

        function.Invoke([true]);

        await Assert.That(state["called"].Read<bool>()).IsTrue();
        function.Dispose();
    }

    [Test]
    [MethodDataSource(typeof(LuaStateCases), nameof(LuaStateCases.All))]
    public async Task DotNetFunctionsCanBeCalledFromLua(LuaStateCase lua)
    {
        using var state = lua.CreateState();
        using var add = lua.CreateAdderFunction(state);

        state["add"] = add;
        var results = state.DoString("return add(20, 22)");

        await Assert.That(results).Count().IsEqualTo(1);
        await Assert.That(results[0].Read<double>()).IsEqualTo(42);
    }
}
