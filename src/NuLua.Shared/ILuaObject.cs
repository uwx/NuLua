namespace NuLua;

public interface ILuaObject : IDisposable
{
    LuaReference Reference { get; }
}

