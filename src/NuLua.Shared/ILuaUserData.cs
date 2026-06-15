namespace NuLua;

public interface ILuaUserData
{
    IntPtr AsPointer();
    bool TryRead<T>(out T result);
}
