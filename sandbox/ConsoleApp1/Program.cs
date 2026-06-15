using NuLua;
using NuLua.Lua55;

using var state = Lua55State.Create();
state.OpenBaseLibrary();
state.DoString("print('Hello, world!')");
state.DoString("print(1 + 2 * 3)");
