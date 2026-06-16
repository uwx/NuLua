namespace NuLua;

public delegate int LuaFunc<TState>(TState state, LuaFuncArguments args)
    where TState : ILuaState;

public readonly struct LuaFuncArguments
{
    readonly ILuaState? state;
    readonly LuaValue[]? values;
    readonly int len;

    public LuaFuncArguments(ILuaState state, int len)
    {
        this.state = state;
        this.values = null;
        this.len = len;
    }

    public LuaFuncArguments(LuaValue[] values, int len)
    {
        this.state = null;
        this.values = values;
        this.len = len;
    }

    public LuaValue this[int index]
    {
        get
        {
            if (index < 0 || index >= len)
                ThrowIndexOutOfRange();
            if (values != null)
                return values[index];
            return state!.ToLuaValue(index + 1);
        }
    }

    public int Length => len;

    static void ThrowIndexOutOfRange()
    {
        throw new IndexOutOfRangeException();
    }
}
