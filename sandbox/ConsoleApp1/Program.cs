using System.Runtime.CompilerServices;
using NuLua;
using NuLua.Lua55;

using var state = Lua55State.Create();
state.OpenBaseLibrary();

var userData = state.CreateUserData(Unsafe.SizeOf<Point>());
Console.WriteLine(userData.Size);

userData.Write(new Point { X = 10, Y = 20 });
var point = userData.Read<Point>();
Console.WriteLine(point);

var newPoint = state.CreateFunction(
    (state, args) =>
    {
        var x = args[0].Read<int>();
        var y = args[1].Read<int>();
        var point = new Point { X = x, Y = y };
        var userData = state.CreateUserData(Unsafe.SizeOf<Point>());
        userData.Write(point);
        state.PushValue(userData.Reference);
        return 1;
    }
);
state["newPoint"] = newPoint;

var area = state.CreateFunction(
    (state, args) =>
    {
        var point = args[0].Read<Point>();
        var result = point.X * point.Y;
        state.Push(result);
        return 1;
    },
    1
);
state["area"] = area;
var results = state.DoString(
    """
    local p = newPoint(10, 20)
    return area(p)
"""
);
Console.WriteLine(results[0]);

Console.WriteLine("--- async ---");

var fetchAsync = state.CreateFunction(
    async (state, args, ct) =>
    {
        var x = args[0].Read<int>();
        await Task.Delay(50, ct);
        state.Push(x * 10);
        return 1;
    }
);
state["fetch"] = fetchAsync;

var asyncResults = await state.DoStringAsync(
    """
    local a = fetch(3)
    local b = fetch(4)
    return a + b
    """
);
Console.WriteLine($"fetch(3) + fetch(4) = {asyncResults[0]}");

var greet = state.CreateFunction(
    async (state, args, ct) =>
    {
        var name = args[0].Read<string>();
        await Task.Delay(1000, ct);
        state.Push($"hello, {name}!");
        return 1;
    }
);
var greetResults = await greet.InvokeAsync(new LuaValue[] { "world" });
Console.WriteLine(greetResults[0]);

try
{
    state.DoString("return fetch(1)");
}
catch (LuaException ex)
{
    Console.WriteLine($"sync call of async function failed as expected: {ex.Message}");
}

record struct Point
{
    public int X;
    public int Y;
}
