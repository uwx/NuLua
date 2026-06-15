namespace NuLua;

public interface ILuaUserData
{
    bool TryRead<T>(out T result);
}
