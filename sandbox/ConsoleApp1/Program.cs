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

record struct Point
{
    public int X;
    public int Y;
}
