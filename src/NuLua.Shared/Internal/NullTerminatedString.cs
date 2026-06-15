using System.Buffers;
using System.Text;

namespace NuLua.Internal;

public struct NullTerminatedString : IDisposable
{
    byte[] buffer;
    int length;

    public NullTerminatedString(ReadOnlySpan<char> str)
    {
        buffer = ArrayPool<byte>.Shared.Rent((str.Length + 1) * 4);
        length = Encoding.UTF8.GetBytes(str, buffer);
        buffer[length] = 0;
    }

    public readonly Span<byte> AsSpan() => buffer.AsSpan(0, length);

    public readonly Span<byte> AsNullTerminatedSpan() => buffer.AsSpan(0, length + 1);

    public void Dispose()
    {
        if (buffer != null)
        {
            ArrayPool<byte>.Shared.Return(buffer);
            buffer = null!;
            length = 0;
        }
    }
}
