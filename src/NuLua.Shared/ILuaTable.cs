namespace NuLua;

public interface ILuaTable : IDisposable
{
    LuaValue GetValue(LuaValue key);
    void SetValue(LuaValue key, LuaValue value);
}
