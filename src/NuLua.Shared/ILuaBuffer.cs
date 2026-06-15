namespace NuLua;

public interface ILuaBuffer : IDisposable
{
    IntPtr AsPointer();
    Span<byte> AsSpan();
}
