using System.Buffers;
using System.Text;

namespace NuLua;

public static class LuaStateExtensions
{
    public static void Push(this ILuaState state, LuaValue value)
    {
        switch (value.Type)
        {
            case LuaValueType.Nil:
                state.PushNil();
                break;
            case LuaValueType.Boolean:
                state.PushBoolean(value.UnsafeRead<bool>());
                break;
            case LuaValueType.Number:
                state.PushNumber(value.UnsafeRead<double>());
                break;
            case LuaValueType.String:
                state.PushString(value.UnsafeRead<string>());
                break;
            case LuaValueType.Table:
            case LuaValueType.Function:
            case LuaValueType.UserData:
            case LuaValueType.Thread:
            {
                var obj = value.UnsafeRead<ILuaObject>();
                state.PushValue(obj.Reference);
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

    public static int GetUpvalueIndex(this ILuaState state, int index)
    {
        return state.RegistryIndex - index;
    }

    public static LuaReference ToReference(this ILuaState state, int index)
    {
        state.PushValue(index);
        var reference = state.Ref();
        state.Pop(1);
        return reference;
    }

    public static LuaTable ToTable(this ILuaState state, int index)
    {
        if (state.GetType(index) != LuaValueType.Table)
        {
            throw new InvalidOperationException("Value at the specified index is not a table.");
        }
        return new LuaTable(state, state.ToReference(index));
    }

    public static LuaUserData ToUserData(this ILuaState state, int index)
    {
        if (state.GetType(index) != LuaValueType.UserData)
        {
            throw new InvalidOperationException("Value at the specified index is not user data.");
        }
        return new LuaUserData(state, state.ToReference(index));
    }

    public static LuaValue ToLuaValue(this ILuaState state, int index)
    {
        var type = state.GetType(index);
        switch (type)
        {
            case LuaValueType.Nil:
                return LuaValue.Nil;
            case LuaValueType.Boolean:
                return state.ToBoolean(index);
            case LuaValueType.Number:
                return state.ToNumber(index);
            case LuaValueType.String:
                return state.ToString(index);
            case LuaValueType.Table:
            {
                state.PushValue(index);
                var reference = state.Ref();
                state.Pop(1);
                return new LuaTable(state, reference);
            }
            case LuaValueType.Function:
            {
                state.PushValue(index);
                var reference = state.Ref();
                state.Pop(1);
                return new LuaFunction(state, reference);
            }
            case LuaValueType.Thread:
            {
                state.PushValue(index);
                var reference = state.Ref();
                state.Pop(1);
                state.PushValue(reference);
                return LuaValue.FromThread(state.ToThread(-1));
            }
            case LuaValueType.UserData:
            {
                state.PushValue(index);
                var reference = state.Ref();
                state.Pop(1);
                return new LuaUserData(state, reference);
            }
            default:
                throw new NotSupportedException($"Unsupported Lua value type: {type}");
        }
    }

    public static LuaReference Ref(this ILuaState state)
    {
        return state.Ref(state.RegistryIndex);
    }

    public static LuaValue Add(this ILuaState state, LuaValue a, LuaValue b)
    {
        state.Push(a);
        state.Push(b);
        state.Arith(LuaArithmeticOperator.Add);
        return state.Pop();
    }

    public static LuaValue Sub(this ILuaState state, LuaValue a, LuaValue b)
    {
        state.Push(a);
        state.Push(b);
        state.Arith(LuaArithmeticOperator.Sub);
        return state.Pop();
    }

    public static LuaValue Mul(this ILuaState state, LuaValue a, LuaValue b)
    {
        state.Push(a);
        state.Push(b);
        state.Arith(LuaArithmeticOperator.Mul);
        return state.Pop();
    }

    public static LuaValue Div(this ILuaState state, LuaValue a, LuaValue b)
    {
        state.Push(a);
        state.Push(b);
        state.Arith(LuaArithmeticOperator.Div);
        return state.Pop();
    }

    public static LuaValue Mod(this ILuaState state, LuaValue a, LuaValue b)
    {
        state.Push(a);
        state.Push(b);
        state.Arith(LuaArithmeticOperator.Mod);
        return state.Pop();
    }

    public static LuaValue Pow(this ILuaState state, LuaValue a, LuaValue b)
    {
        state.Push(a);
        state.Push(b);
        state.Arith(LuaArithmeticOperator.Pow);
        return state.Pop();
    }

    public static LuaValue Unm(this ILuaState state, LuaValue a)
    {
        state.Push(a);
        state.Arith(LuaArithmeticOperator.Unm);
        return state.Pop();
    }

    public static LuaValue BNot(this ILuaState state, LuaValue a)
    {
        state.Push(a);
        state.Arith(LuaArithmeticOperator.BNot);
        return state.Pop();
    }

    public static LuaValue BAnd(this ILuaState state, LuaValue a, LuaValue b)
    {
        state.Push(a);
        state.Push(b);
        state.Arith(LuaArithmeticOperator.BAnd);
        return state.Pop();
    }

    public static LuaValue BOr(this ILuaState state, LuaValue a, LuaValue b)
    {
        state.Push(a);
        state.Push(b);
        state.Arith(LuaArithmeticOperator.BOr);
        return state.Pop();
    }

    public static LuaValue BXor(this ILuaState state, LuaValue a, LuaValue b)
    {
        state.Push(a);
        state.Push(b);
        state.Arith(LuaArithmeticOperator.BXor);
        return state.Pop();
    }

    public static LuaValue Shl(this ILuaState state, LuaValue a, LuaValue b)
    {
        state.Push(a);
        state.Push(b);
        state.Arith(LuaArithmeticOperator.Shl);
        return state.Pop();
    }

    public static LuaValue Shr(this ILuaState state, LuaValue a, LuaValue b)
    {
        state.Push(a);
        state.Push(b);
        state.Arith(LuaArithmeticOperator.Shr);
        return state.Pop();
    }

    public static LuaValue Len(this ILuaState state, LuaValue a)
    {
        state.Push(a);
        state.Len(-1);
        return state.Pop();
    }

    public static LuaValue Concat(this ILuaState state, params ReadOnlySpan<LuaValue> values)
    {
        foreach (var value in values)
        {
            state.Push(value);
        }
        state.Concat(values.Length);
        return state.Pop();
    }

    public static bool Equals(this ILuaState state, LuaValue a, LuaValue b)
    {
        state.Push(a);
        state.Push(b);
        state.Compare(LuaComparisonOperator.Equal);
        return state.ToBoolean(-1);
    }

    public static bool LessThan(this ILuaState state, LuaValue a, LuaValue b)
    {
        state.Push(a);
        state.Push(b);
        state.Compare(LuaComparisonOperator.Less);
        return state.ToBoolean(-1);
    }

    public static bool LessThanOrEqual(this ILuaState state, LuaValue a, LuaValue b)
    {
        state.Push(a);
        state.Push(b);
        state.Compare(LuaComparisonOperator.LessOrEqual);
        return state.ToBoolean(-1);
    }

    public static LuaTable CreateTable(
        this ILuaState state,
        int initialArraySize = 0,
        int initialRecordsSize = 0
    )
    {
        state.NewTable(initialArraySize, initialRecordsSize);
        return new LuaTable(state, state.Ref());
    }

    public static LuaUserData CreateUserData(this ILuaState state, int size, int userValueCount = 1)
    {
        state.NewUserData(size, userValueCount);
        return new LuaUserData(state, state.Ref());
    }

    public static LuaFunction CreateFunction<TState>(
        this ILuaState<TState> state,
        LuaFunc<TState> function,
        int upvalueCount = 0
    )
        where TState : ILuaState<TState>
    {
        state.NewFunction(function, upvalueCount);
        return new LuaFunction(state, state.Ref());
    }

    public static ILuaState CreateThread(this ILuaState state)
    {
        state.NewThread();
        return state.ToThread(-1);
    }

    public static TState CreateThread<TState>(this ILuaState<TState> state)
        where TState : ILuaState<TState>
    {
        state.NewThread();
        return state.ToThread(-1);
    }

    public static LuaValue[] DoString(
        this ILuaState state,
        ReadOnlySpan<char> code,
        ReadOnlySpan<LuaValue> args = default
    )
    {
        var baseTop = state.GetTop();

        state.LoadString(code);
        for (int i = 0; i < args.Length; i++)
        {
            state.Push(args[i]);
        }
        state.Call(args.Length, -1);

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

    public static byte[] Dump(this ILuaState state, int index)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(1024);
        try
        {
            while (true)
            {
                if (state.TryDump(index, buffer, out var bytesWritten))
                {
                    return buffer.AsSpan(0, bytesWritten).ToArray();
                }
                var newBuffer = ArrayPool<byte>.Shared.Rent(buffer.Length * 2);
                Array.Copy(buffer, newBuffer, buffer.Length);
                ArrayPool<byte>.Shared.Return(buffer);
                buffer = newBuffer;
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
