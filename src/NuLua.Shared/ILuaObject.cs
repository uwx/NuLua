namespace NuLua;

public interface ILuaObject
{
    LuaObjectHandle Handle { get; }
}

public record struct LuaObjectHandle(ILuaState State, int Index);
