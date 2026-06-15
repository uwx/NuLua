namespace NuLua;

public sealed class LuaTable(ILuaState state, int index) : ILuaObject
{
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
                table.Handle.State.PushNil();
                started = true;
            }
            else
            {
                table.Handle.State.Pop(1);
            }

            table.Handle.State.Next(table.Handle.Index);
            if (table.Handle.State.GetType(-2) == LuaType.Nil)
            {
                return false;
            }

            var key = table.Handle.State.ToLuaValue(-2);
            var value = table.Handle.State.ToLuaValue(-1);
            current = new KeyValuePair<LuaValue, LuaValue>(key, value);
            return true;
        }
    }

    public LuaObjectHandle Handle => new(state, index);

    public LuaValue this[int index]
    {
        get
        {
            Handle.State.PushInteger(index);
            Handle.State.GetTable(Handle.Index);
            var value = Handle.State.Pop();
            return value;
        }
        set
        {
            Handle.State.PushInteger(index);
            Handle.State.Push(value);
            Handle.State.SetTable(Handle.Index);
        }
    }

    public LuaValue this[ReadOnlySpan<char> key]
    {
        get
        {
            Handle.State.PushString(key);
            Handle.State.GetTable(Handle.Index);
            var value = Handle.State.Pop();
            return value;
        }
        set
        {
            Handle.State.PushString(key);
            Handle.State.Push(value);
            Handle.State.SetTable(Handle.Index);
        }
    }

    public LuaValue this[ReadOnlySpan<byte> key]
    {
        get
        {
            Handle.State.PushString(key);
            Handle.State.GetTable(Handle.Index);
            var value = Handle.State.Pop();
            return value;
        }
        set
        {
            Handle.State.PushString(key);
            Handle.State.Push(value);
            Handle.State.SetTable(Handle.Index);
        }
    }

    public LuaValue this[LuaValue key]
    {
        get
        {
            Handle.State.Push(key);
            Handle.State.GetTable(Handle.Index);
            var value = Handle.State.Pop();
            return value;
        }
        set
        {
            Handle.State.Push(key);
            Handle.State.Push(value);
            Handle.State.SetTable(Handle.Index);
        }
    }

    public int Length
    {
        get
        {
            Handle.State.PushValue(Handle.Index);
            Handle.State.Len(-1);
            var length = (int)Handle.State.ToNumber(-1);
            Handle.State.SetTop(Handle.State.GetTop() - 1);
            return length;
        }
    }

    public void Next()
    {
        Handle.State.Next(Handle.Index);
    }

    public Enumerator GetEnumerator() => new(this);
}
