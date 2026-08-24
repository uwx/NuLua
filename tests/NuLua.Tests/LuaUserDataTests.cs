using System.Collections;
using NuLua.Luau;

namespace NuLua.Tests;

public class LuaUserDataTests
{
    [Test]
    public async Task CreateUserData_IndexNewIndexLenAndToStringWorkFromLua()
    {
        using var state = LuauState.Create();
        state.OpenLibraries();

        var arr = new TestArray();
        arr.Add(1.0);
        arr.Add(2.0);
        arr.Add(3.0);
        state["arr"] = LuaValue.FromUserData(state.CreateUserData(arr));

        state.DoString("assert(arr[1] == 1.0)");
        state.DoString("assert(arr[3] == 3.0)");
        state.DoString("assert(arr[4] == nil)");
        state.DoString("assert(#arr == 3)");
        state.DoString("assert(tostring(arr) == 'TestArray[3]')");

        state.DoString("arr[4] = 40.0");
        state.DoString("assert(#arr == 4)");
        state.DoString("assert(arr[4] == 40.0)");
    }

    [Test]
    public async Task CreateUserData_ForInIterationYieldsIndexedValues()
    {
        using var state = LuauState.Create();
        state.OpenLibraries();

        var arr = new TestArray();
        arr.Add(1.0);
        arr.Add(2.0);
        arr.Add(3.0);
        state["arr"] = LuaValue.FromUserData(state.CreateUserData(arr));

        var results = state.DoString(
            "local s = 0 for k, v in arr do s = s + k * v end return s"
        );
        await Assert.That(results[0].Read<double>()).IsEqualTo(1 * 1 + 2 * 2 + 3 * 3);
    }

    [Test]
    public async Task CreateUserData_ManagedObjectReadBackFromCSharpCallbackIsSameInstance()
    {
        using var state = LuauState.Create();
        state.OpenLibraries();

        var arr = new TestArray();
        arr.Add(7.0);
        arr.Add(8.0);

        TestArray? received = null;
        state.RegisterFunction(
            "capture",
            (s, args) =>
            {
                received = args[0].Read<TestArray>();
                s.PushNil();
                return 0;
            }
        );

        state["arr"] = LuaValue.FromUserData(state.CreateUserData(arr));
        state.DoString("capture(arr)");

        await Assert.That(ReferenceEquals(received, arr)).IsTrue();
    }

    [Test]
    public async Task CreateUserData_MetatableIsSharedPerTypeAndExposesTypeName()
    {
        using var state = LuauState.Create();
        state.OpenLibraries();

        state["a"] = LuaValue.FromUserData(state.CreateUserData(new TestArray()));
        state["b"] = LuaValue.FromUserData(state.CreateUserData(new TestArray()));

        var results = state.DoString(
            "return getmetatable(a) == getmetatable(b), getmetatable(a).__type"
        );

        await Assert.That(results[0].Read<bool>()).IsTrue();
        await Assert.That(results[1].Read<string>()).IsEqualTo(nameof(TestArray));
    }

    [Test]
    public async Task CreateUserData_GCHandleIsFreedWhenUserDataIsCollected()
    {
        using var state = LuauState.Create();
        state.OpenLibraries();

        var weak = CreateAndDrop(state);

        // Native Luau GC: collects the userdata → dtor frees the GCHandle.
        state.GarbageCollection.Collect();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        await Assert.That(weak.IsAlive).IsFalse();
    }

    static WeakReference CreateAndDrop(LuauState state)
    {
        var arr = new TestArray();
        arr.Add(1.0);

        var userData = state.CreateUserData(arr);
        state["obj"] = LuaValue.FromUserData(userData);

        var weak = new WeakReference(arr);

        state["obj"] = LuaValue.Nil;
        userData.Dispose(); // drop the registry ref so the native userdata can be collected

        return weak;
    }

    /// <summary>Minimal 1-based array-like ILuaUserData fixture for exercising the bridge.</summary>
    sealed class TestArray : ILuaUserData<TestArray>
    {
        readonly List<double> items = new();

        public void Add(double item) => items.Add(item);

        public LuaUserDataMetamethods SupportedMetamethods =>
            LuaUserDataMetamethods.Index
            | LuaUserDataMetamethods.NewIndex
            | LuaUserDataMetamethods.Length
            | LuaUserDataMetamethods.ToString
            | LuaUserDataMetamethods.Iter;

        public bool TryGetIndex(ILuaState state, LuaValue key, out LuaValue value)
        {
            if (
                key.TryRead<double>(out var num)
                && num == Math.Floor(num)
                && num >= 1
                && num <= items.Count
            )
            {
                value = LuaValue.FromNumber(items[(int)num - 1]);
                return true;
            }

            value = LuaValue.Nil;
            return false;
        }

        public bool TrySetIndex(ILuaState state, LuaValue key, LuaValue value)
        {
            if (key.TryRead<double>(out var num) && num == Math.Floor(num) && num >= 1)
            {
                var index = (int)num - 1;
                while (items.Count <= index)
                {
                    items.Add(0);
                }

                items[index] = value.Read<double>();
                return true;
            }

            return false;
        }

        public long? Length => items.Count;

        public string? ToLuaString() => $"TestArray[{items.Count}]";

        public IEnumerator<KeyValuePair<LuaValue, LuaValue>>? GetIterator(ILuaState state)
        {
            for (int i = 0; i < items.Count; i++)
            {
                yield return new KeyValuePair<LuaValue, LuaValue>(
                    LuaValue.FromNumber(i + 1),
                    LuaValue.FromNumber(items[i])
                );
            }
        }
    }
}
