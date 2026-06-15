namespace NuLua;

public static class LuaStateExtensions
{
    public static void SetGlobal(this ILuaState state, ReadOnlySpan<char> name, LuaValue value)
    {
        state.Push(value);
        state.SetGlobal(name);
    }

    public static void Push(this ILuaState state, LuaValue value)
    {
        switch (value.Type)
        {
            case LuaType.Nil:
                state.PushNil();
                break;
            case LuaType.Boolean:
                state.PushBoolean(value.UnsafeRead<bool>());
                break;
            case LuaType.Number:
                state.PushNumber(value.UnsafeRead<double>());
                break;
            case LuaType.String:
                state.PushString(value.UnsafeRead<string>());
                break;
            case LuaType.Table:
            case LuaType.Function:
            case LuaType.UserData:
            {
                var obj = value.UnsafeRead<ILuaObject>();
                if (obj.Handle.State != state)
                {
                    throw new ArgumentException(
                        "Cannot push a Lua object from a different Lua state.",
                        nameof(value)
                    );
                }
                state.PushValue(obj.Handle.Index);
                break;
            }
            default:
                throw new NotSupportedException($"Unsupported Lua value type: {value.Type}");
        }
    }

    public static LuaValue Pop(this ILuaState state)
    {
        var value = state.ToLuaValue(-1);
        state.SetTop(state.GetTop() - 1);
        return value;
    }

    public static LuaValue ToLuaValue(this ILuaState state, int index)
    {
        var type = state.GetType(index);
        return type switch
        {
            LuaType.Nil => LuaValue.Nil,
            LuaType.Boolean => (LuaValue)state.ToBoolean(index),
            LuaType.Number => (LuaValue)state.ToNumber(index),
            LuaType.String => (LuaValue)state.ToString(index),
            LuaType.Table => LuaValue.FromTable(new LuaTable(state, index)),
            LuaType.Function => LuaValue.FromFunction(new LuaFunction(state, index)),
            _ => throw new NotSupportedException($"Unsupported Lua value type: {type}"),
        };
    }

    public static void DoString(this ILuaState state, ReadOnlySpan<char> code)
    {
        state.LoadString(code);
        state.Call(0, 0);
    }
}
