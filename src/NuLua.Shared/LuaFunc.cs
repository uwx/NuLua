namespace NuLua;

public delegate int LuaFunc<TState>(TState state, LuaFuncArguments args)
    where TState : ILuaState;

public readonly struct LuaFuncArguments(ILuaState state, int len)
{
    public LuaValue this[int index]
    {
        get
        {
            if (index < 0 || index >= len)
                ThrowIndexOutOfRange();
            return state.ToLuaValue(index + 1);
        }
    }

    public int Length => len;

    void ThrowIndexOutOfRange()
    {
        throw new IndexOutOfRangeException();
    }
}
