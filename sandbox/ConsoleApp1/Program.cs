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
state.DoString("print(add(10, 20))");