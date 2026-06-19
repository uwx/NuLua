namespace NuLua.Luau;

public sealed class LuauObject(LuauState state, LuaReference reference) : ILuaObject
{
    readonly LuauState state = state;

    public LuaReference Reference => reference;

    public void Dispose() => state.Unref(reference);
}
