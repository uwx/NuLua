namespace NuLua;

public sealed class LuaFunction(ILuaState state, LuaReference reference)
    : LuaFunctionBase(state, reference)
{
    public int Invoke(ReadOnlySpan<LuaValue> args)
    {
        State.PushValue(Reference);
        foreach (var arg in args)
        {
            State.Push(arg);
        }
        State.Call(args.Length, 0);
        return 0;
    }
}
