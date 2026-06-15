namespace NuLua;

public sealed class LuaTable(ILuaState state, int index) : ILuaObject
{
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
}
