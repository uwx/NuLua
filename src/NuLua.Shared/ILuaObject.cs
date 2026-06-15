namespace NuLua;

public interface ILuaObject : IDisposable
{
    IntPtr AsPointer();
}