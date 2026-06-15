namespace NuLua;

public interface ILuaState : ILuaObject
{
    void OpenLibrary(ReadOnlySpan<char> name);

    void Push(LuaValue value)
    {
        switch (value.Type)
        {
            case LuaType.Nil:
                PushNil();
                break;
            case LuaType.Number:
                PushNumber(value.UnsafeRead<double>());
                break;
            case LuaType.String:
                PushString(value.UnsafeRead<string>());
                break;
            default:
                throw new NotSupportedException($"Unsupported Lua value type: {value.Type}");
        }
    }

    void PushNil();
    void PushInteger(long value);
    void PushNumber(double value);
    void PushString(ReadOnlySpan<char> value);
    void PushString(ReadOnlySpan<byte> utf8Value);
    void PushValue(int index);

    bool ToBoolean(int index);
    double ToNumber(int index);
    string ToString(int index);
    IntPtr ToLightUserData(int index);
    ILuaTable ToTable(int index);
    ILuaFunction ToFunction(int index);
    ILuaUserData ToUserData(int index);

    int GetTop();
    void SetTop(int index);

    void GetGlobal(ReadOnlySpan<char> name);
    void GetGlobal(ReadOnlySpan<byte> utf8Name)
    {
        
    }
    void SetGlobal(ReadOnlySpan<char> name, LuaValue value);
    void SetGlobal(ReadOnlySpan<byte> utf8Name, LuaValue value);
}
