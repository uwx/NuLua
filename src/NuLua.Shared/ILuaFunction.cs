namespace NuLua;

public interface ILuaFunction : ILuaObject
{
    int Invoke(ILuaState state, int argCount);
}
