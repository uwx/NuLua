namespace NuLua;

public sealed class LuaTable(ILuaState state, LuaReference reference) : LuaObject(state, reference)
{
    readonly ILuaState state = state;

    public struct Enumerator(LuaTable table)
    {
        readonly LuaTable table = table;
        bool started = false;
        bool finished = false;
        int tableIndex = 0;
        KeyValuePair<LuaValue, LuaValue> current;
        public KeyValuePair<LuaValue, LuaValue> Current => current;

        public bool MoveNext()
        {
            if (finished)
            {
                return false;
            }

            if (!started)
            {
                table.state.PushValue(table.Reference);
                tableIndex = table.state.GetAbsIndex(-1);
                table.state.PushNil();
                started = true;
            }
            else
            {
                // Each pair yielded by this enumerator holds registry references
                // (see ToLuaValue); release the previous pair now that it has been
                // consumed and is about to be overwritten.
                ReleaseCurrent();
                table.state.Pop(1);
            }

            table.state.Next(tableIndex);
            if (table.state.GetTop() == tableIndex)
            {
                table.state.Pop(1);
                finished = true;
                // The last yielded pair is no longer reachable once the enumeration
                // completes; release its registry references too.
                ReleaseCurrent();
                return false;
            }

            var key = table.state.ToLuaValue(-2);
            var value = table.state.ToLuaValue(-1);
            current = new KeyValuePair<LuaValue, LuaValue>(key, value);
            return true;
        }

        void ReleaseCurrent()
        {
            current.Key.Dispose();
            current.Value.Dispose();
            current = default;
        }
    }

    public LuaValue this[int index]
    {
        get
        {
            state.PushValue(Reference);
            state.PushInteger(index);
            state.GetTable(-2);
            var value = state.Pop();
            state.Pop(1);
            return value;
        }
        set
        {
            state.PushValue(Reference);
            state.PushInteger(index);
            state.Push(value);
            state.SetTable(-3);
            state.Pop(1);
        }
    }

    public LuaValue this[ReadOnlySpan<char> key]
    {
        get
        {
            state.PushValue(Reference);
            state.PushString(key);
            state.GetTable(-2);
            var value = state.Pop();
            state.Pop(1);
            return value;
        }
        set
        {
            state.PushValue(Reference);
            state.PushString(key);
            state.Push(value);
            state.SetTable(-3);
            state.Pop(1);
        }
    }

    public LuaValue this[ReadOnlySpan<byte> key]
    {
        get
        {
            state.PushValue(Reference);
            state.PushString(key);
            state.GetTable(-2);
            var value = state.Pop();
            state.Pop(1);
            return value;
        }
        set
        {
            state.PushValue(Reference);
            state.PushString(key);
            state.Push(value);
            state.SetTable(-3);
            state.Pop(1);
        }
    }

    public LuaValue this[LuaValue key]
    {
        get
        {
            state.PushValue(Reference);
            state.Push(key);
            state.GetTable(-2);
            var value = state.Pop();
            state.Pop(1);
            return value;
        }
        set
        {
            state.PushValue(Reference);
            state.Push(key);
            state.Push(value);
            state.SetTable(-3);
            state.Pop(1);
        }
    }

    public int Length
    {
        get
        {
            state.PushValue(Reference);
            state.Len(-1);
            var length = (int)state.ToNumber(-1);
            state.Pop(2);
            return length;
        }
    }

    public Enumerator GetEnumerator() => new(this);

    public bool TryGetValue(LuaValue key, out LuaValue value)
    {
        value = this[key];
        return value.Type != LuaValueType.Nil;
    }
}
