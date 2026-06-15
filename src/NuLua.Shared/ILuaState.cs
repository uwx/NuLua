namespace NuLua;

public interface ILuaState : IDisposable
{
    void OpenLibrary(string name);
}
