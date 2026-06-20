namespace NuLua;

public abstract class LuaFunctionBase(ILuaState state, LuaReference reference) : ILuaObject
{
    public LuaReference Reference => reference;

    protected ILuaState State => state;

    public void Dispose()
    {
        State.Unref(Reference);
    }
}
