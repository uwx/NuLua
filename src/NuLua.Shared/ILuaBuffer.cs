namespace NuLua;

public interface ILuaBuffer : ILuaObject
{
    Span<byte> AsSpan();
}
