using System.Buffers;
using System.Text;

namespace NuLua;

public static class LuaStateExtensions
{
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
            case LuaType.Thread:
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

    public static void PushString(this ILuaState state, ReadOnlySpan<char> str)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(str.Length * 3);
        try
        {
            var len = Encoding.UTF8.GetBytes(str, buffer);
            state.PushString(new ReadOnlySpan<byte>(buffer, 0, len));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public static LuaValue Pop(this ILuaState state)
    {
        var value = state.ToLuaValue(-1);
        state.SetTop(state.GetTop() - 1);
        return value;
    }

    public static void Pop(this ILuaState state, int count)
    {
        state.SetTop(state.GetTop() - count);
    }

    public static void Insert(this ILuaState state, int index)
    {
        state.Rotate(index, 1);
    }

    public static void Remove(this ILuaState state, int index)
    {
        state.Rotate(index, -1);
        state.SetTop(state.GetTop() - 1);
    }

    public static void Replace(this ILuaState state, int index)
    {
        state.Copy(-1, index);
        state.SetTop(state.GetTop() - 1);
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
            LuaType.Thread => LuaValue.FromThread(state.ToThread(index)),
            _ => throw new NotSupportedException($"Unsupported Lua value type: {type}"),
        };
    }

    public static LuaValue[] DoString(this ILuaState state, ReadOnlySpan<char> code)
    {
        var baseTop = state.GetTop();

        state.LoadString(code);
        state.Call(0, -1);

        var currentTop = state.GetTop();
        var returnCount = currentTop - baseTop;

        var results = new LuaValue[returnCount];
        for (int i = 0; i < returnCount; i++)
        {
            results[i] = state.ToLuaValue(baseTop + 1 + i);
        }
        state.SetTop(baseTop);

        return results;
    }
}
