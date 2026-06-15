namespace NuLua;

public interface ILuaFunction : IDisposable
{
    IntPtr AsPointer();
    int Invoke(ILuaState state, int argCount);
}
