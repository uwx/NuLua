namespace NuLua;

public sealed class LuaFunction(ILuaState state, LuaReference reference) : ILuaObject
{
    public LuaReference Reference => reference;

    public int Invoke(ReadOnlySpan<LuaValue> args)
    {
        state.PushValue(Reference);
        foreach (var arg in args)
        {
            state.Push(arg);
        }
        state.Call(args.Length, 0);
        return 0;
    }

    public void Dispose()
    {
        state.Unref(Reference);
    }
}
