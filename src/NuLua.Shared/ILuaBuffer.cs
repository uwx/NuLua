namespace NuLua;

public interface ILuaBuffer : IDisposable
{
    Span<byte> AsSpan();
}
