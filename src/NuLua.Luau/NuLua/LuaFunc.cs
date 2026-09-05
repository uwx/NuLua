using NuLua.Luau;

namespace NuLua;

public delegate int LuaFunc(LuauState state, LuaFuncArguments args);

public readonly struct LuaFuncArguments
{
    readonly LuauState state;
    readonly int len;

    public LuaFuncArguments(LuauState state, int len)
    {
        this.state = state;
        this.len = len;
    }

    public LuaArgumentValue this[int index]
    {
        get
        {
            if (index < 0 || index >= len)
                ThrowIndexOutOfRange();
            return state.ToArgumentValue(index + 1);
        }
    }

    public int Length => len;

    static void ThrowIndexOutOfRange()
    {
        throw new IndexOutOfRangeException();
    }
}
