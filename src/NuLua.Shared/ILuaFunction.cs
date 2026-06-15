namespace NuLua;

public interface ILuaFunction : IDisposable
{
    int Invoke(ILuaState state, int argCount);
}
