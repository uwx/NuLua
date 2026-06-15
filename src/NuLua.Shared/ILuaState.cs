namespace NuLua;

public interface ILuaState : IDisposable
{
    void OpenLibrary(string name);
    void Push(LuaValue value);
    LuaValue Pop();
    LuaValue GetGlobal(string name);
    void SetGlobal(string name, LuaValue value);
}
