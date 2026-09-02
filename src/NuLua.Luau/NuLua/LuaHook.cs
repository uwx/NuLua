using NuLua.Luau;

namespace NuLua;

public delegate void LuaHook(LuauState state, LuaHookEvent hookEvent, int currentLine);