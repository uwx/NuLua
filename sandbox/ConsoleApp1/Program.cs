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

record struct Point
{
    public int X;
    public int Y;
}
