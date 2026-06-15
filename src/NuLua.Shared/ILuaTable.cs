namespace NuLua;

public interface ILuaTable : ILuaObject
{
    LuaValue GetValue(LuaValue key);
    void SetValue(LuaValue key, LuaValue value);
}
