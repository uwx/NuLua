using System.Buffers;
using NuLua.Interop.Luau;

namespace NuLua.Luau;

public static unsafe class LuauCompiler
{
    public static void Compile(
        IBufferWriter<byte> writer,
        ReadOnlySpan<byte> source,
        LuauCompileOptions? options = null
    )
    {
        byte* code;
        nuint size;

        fixed (byte* ptr = source)
        {
            var nativeOptions = (options ?? LuauCompileOptions.Default).options;
            code = NativeMethods.luau_compile(
                ptr,
                (nuint)(source.Length * sizeof(byte)),
                &nativeOptions,
                &size
            );
        }

        try
        {
            var destination = writer.GetSpan((int)size);
            new ReadOnlySpan<byte>(code, (int)size).CopyTo(destination);
            writer.Advance((int)size);
        }
        finally
        {
            NativeMethods.luau_free(code);
        }
    }

    public static byte[] Compile(ReadOnlySpan<byte> source, LuauCompileOptions? options = null)
    {
        byte* code;
        nuint size;

        fixed (byte* ptr = source)
        {
            var nativeOptions = (options ?? LuauCompileOptions.Default).options;
            code = NativeMethods.luau_compile(
                ptr,
                (nuint)(source.Length * sizeof(byte)),
                &nativeOptions,
                &size
            );
        }

        try
        {
            if (size > 0X7FFFFFC7) // Array.MaxLength
            {
                throw new LuaException(4, "Bytecode size is too large");
            }

            var result = new byte[(int)size];
            new ReadOnlySpan<byte>(code, (int)size).CopyTo(result);

            return result;
        }
        finally
        {
            NativeMethods.luau_free(code);
        }
    }
}
