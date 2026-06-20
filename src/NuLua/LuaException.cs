namespace NuLua;

public class LuaException : Exception
{
    public uint Code { get; }

    public LuaException(uint code, string message)
        : base(message)
    {
        Code = code;
    }

    public LuaException(uint code, string message, Exception innerException)
        : base(message, innerException)
    {
        Code = code;
    }
}
