namespace NuLua;

public interface ILuaTable : IDisposable
{
    IntPtr AsPointer();
    LuaValue GetValue(LuaValue key);
    void SetValue(LuaValue key, LuaValue value);
}
