namespace NuLua;

public interface ILuaObject : IDisposable
{
    LuaObjectHandle Handle { get; }
}

public record struct LuaObjectHandle(ILuaState State, int Index);
