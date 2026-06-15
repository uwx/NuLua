namespace NuLua;

public interface ILuaUserData : ILuaObject
{
    bool TryRead<T>(out T result);
}
