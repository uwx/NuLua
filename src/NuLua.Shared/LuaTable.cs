namespace NuLua;

public sealed class LuaTable(ILuaState state, LuaReference reference) : ILuaObject
{
    readonly ILuaState state = state;

    public struct Enumerator(LuaTable table)
    {
        readonly LuaTable table = table;
        bool started = false;
        KeyValuePair<LuaValue, LuaValue> current;
        public KeyValuePair<LuaValue, LuaValue> Current => current;

        public bool MoveNext()
        {
            if (!started)
            {
                table.state.PushNil();
                started = true;
            }
            else
            {
                table.state.Pop(1);
            }

            table.state.PushValue(table.Reference);
            table.state.Next(-2);
            if (table.state.GetType(-2) == LuaValueType.Nil)
            {
                return false;
            }

            var key = table.state.ToLuaValue(-2);
            var value = table.state.ToLuaValue(-1);
            current = new KeyValuePair<LuaValue, LuaValue>(key, value);
            return true;
        }
    }

    public LuaReference Reference => reference;

    public LuaValue this[int index]
    {
        get
        {
            state.PushInteger(index);
            state.GetTable(Reference.Id);
            var value = state.Pop();
            return value;
        }
        set
        {
            state.PushInteger(index);
            state.Push(value);
            state.SetTable(Reference.Id);
        }
    }

    public LuaValue this[ReadOnlySpan<char> key]
    {
        get
        {
            state.PushString(key);
            state.GetTable(Reference.Id);
            var value = state.Pop();
            return value;
        }
        set
        {
            state.PushString(key);
            state.Push(value);
            state.SetTable(Reference.Id);
        }
    }

    public LuaValue this[ReadOnlySpan<byte> key]
    {
        get
        {
            state.PushString(key);
            state.GetTable(Reference.Id);
            var value = state.Pop();
            return value;
        }
        set
        {
            state.PushString(key);
            state.Push(value);
            state.SetTable(Reference.Id);
        }
    }

    public LuaValue this[LuaValue key]
    {
        get
        {
            state.Push(key);
            state.GetTable(Reference.Id);
            var value = state.Pop();
            return value;
        }
        set
        {
            state.Push(key);
            state.Push(value);
            state.SetTable(Reference.Id);
        }
    }

    public int Length
    {
        get
        {
            state.PushValue(Reference.Id);
            state.Len(-1);
            var length = (int)state.ToNumber(-1);
            state.SetTop(state.GetTop() - 1);
            return length;
        }
    }

    public Enumerator GetEnumerator() => new(this);

    public void Dispose()
    {
        state.Unref(Reference);
    }
}
