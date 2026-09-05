using NuLua.Luau;

namespace NuLua;

public sealed class LuaTableRef(LuauState state, LuaReference reference) : LuaObjectRef(state, reference)
{
    readonly LuauState state = state;

    public struct Enumerator(LuaTableRef table)
    {
        readonly LuaTableRef table = table;
        bool started = false;
        bool finished = false;
        int tableIndex = 0;
        KeyValuePair<LuaRefValue, LuaRefValue> current;
        public KeyValuePair<LuaRefValue, LuaRefValue> Current => current;

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
                table.state.Pop(1);
            }

            table.state.Next(tableIndex);
            if (table.state.GetTop() == tableIndex)
            {
                table.state.Pop(1);
                finished = true;
                return false;
            }
            
            var key = table.state.ToLuaValue(-2);
            var value = table.state.ToLuaValue(-1);
            
            current.Key.Return();
            current.Value.Return();
            current = new KeyValuePair<LuaRefValue, LuaRefValue>(key, value);
            return true;
        }

        public void Dispose()
        {
            current.Key.Return();
            current.Value.Return();
        }
    }

    public LuaRefValue this[int index]
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

    public LuaRefValue this[ReadOnlySpan<char> key]
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

    public LuaRefValue this[ReadOnlySpan<byte> key]
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

    public LuaRefValue this[LuaRefValue key]
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

    public bool TryGetValue(LuaRefValue key, out LuaRefValue value)
    {
        value = this[key];
        return value.Type != LuaValueType.Nil;
    }
}
