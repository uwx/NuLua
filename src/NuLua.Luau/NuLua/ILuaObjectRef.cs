namespace NuLua;

public interface ILuaObjectRef : IDisposable
{
    LuaReference Reference { get; }
}
