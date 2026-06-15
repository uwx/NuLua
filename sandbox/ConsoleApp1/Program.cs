using NuLua;
using NuLua.Lua55;

using var state = Lua55State.Create();
state.OpenBaseLibrary();

var addFunc = state.CreateFunction(
    (state, args) =>
    {
        var a = args[0].Read<double>();
        var b = args[1].Read<double>();
        state.Push(a + b);
        return 1;
    }
);

state.SetGlobal("add", addFunc);
var results = state.DoString("return add(10, 20)");
Console.WriteLine($"{results[0].Read<double>()}");

state.OpenCoroutineLibrary();
var thread = state
    .DoString(
        """
return coroutine.create(function()
    for i = 1, 5 do
        coroutine.yield(i)
    end
end)
"""
    )[0]
    .Read<Lua55State>();

Console.WriteLine($"thread status: {thread.Status}");

for (int i = 0; i < 5; i++)
{
    thread.Resume(0);
    Console.WriteLine($"yield: {thread.ToNumber(-1)}");
    thread.Pop(1);
}
