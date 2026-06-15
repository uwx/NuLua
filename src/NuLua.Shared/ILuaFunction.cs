namespace NuLua;

public sealed class LuaFunction(ILuaState state, int index) : ILuaObject
{
    public LuaObjectHandle Handle => new(state, index);

    public int Invoke(ILuaState state, ReadOnlySpan<LuaValue> args)
    {
        if (Handle.State != state)
        {
            throw new ArgumentException(
                "Cannot invoke a Lua function from a different Lua state.",
                nameof(state)
            );
        }

        state.PushValue(Handle.Index);
        foreach (var arg in args)
        {
            state.Push(arg);
        }
        state.Call(args.Length, 0);
        return 0;
    }
}
