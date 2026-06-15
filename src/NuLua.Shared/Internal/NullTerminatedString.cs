using System.Buffers;
using System.Text;

namespace NuLua.Internal;

public struct NullTerminatedString : IDisposable
{
    byte[] buffer;

    public NullTerminatedString(ReadOnlySpan<char> str)
    {
        buffer = ArrayPool<byte>.Shared.Rent((str.Length + 1) * 4);
        var bytesWritten = Encoding.UTF8.GetBytes(str, buffer);
        buffer[bytesWritten] = 0;
    }

    public readonly Span<byte> AsSpan() => buffer;

    public void Dispose()
    {
        if (buffer != null)
        {
            ArrayPool<byte>.Shared.Return(buffer);
            buffer = null!;
        }
    }
}
