using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace NuLua;

public sealed class FileSystemModuleLoader : LuaModuleLoader
{
    public static readonly FileSystemModuleLoader Default = new();

    public string? WorkingDirectory { get; init; }
    public string Extension { get; init; } = ".lua";
    public IReadOnlyDictionary<string, string>? Aliases { get; init; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    string GetWorkingDirectoryOrDefault()
    {
        return WorkingDirectory ?? Directory.GetCurrentDirectory();
    }

    protected override bool TryLoadModule(ILuaState state, string fullPath, string requireArgument)
    {
        var targetPath = Path.IsPathRooted(fullPath)
            ? fullPath
            : Path.Combine(GetWorkingDirectoryOrDefault(), fullPath);

        if (!Path.HasExtension(targetPath))
        {
            targetPath += Extension;
        }

        if (!File.Exists(targetPath))
        {
            return false;
        }

        byte[]? rented = null;
        int length;
        try
        {
            using var stream = File.Open(
                targetPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read
            );
            length = (int)stream.Length;
            rented = ArrayPool<byte>.Shared.Rent(length);
            int read = 0;
            while (read < length)
            {
                var n = stream.Read(rented, read, length - read);
                if (n <= 0)
                    break;
                read += n;
            }
            length = read;

            var chunkName = Path.GetFileNameWithoutExtension(targetPath);
            var chunkNameByteCount = Encoding.UTF8.GetByteCount(chunkName);
            var chunkNameBuffer = ArrayPool<byte>.Shared.Rent(chunkNameByteCount);
            try
            {
                Encoding.UTF8.GetBytes(chunkName, 0, chunkName.Length, chunkNameBuffer, 0);
                state.LoadString(
                    new ReadOnlySpan<byte>(rented, 0, length),
                    new ReadOnlySpan<byte>(chunkNameBuffer, 0, chunkNameByteCount)
                );
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(chunkNameBuffer);
            }
        }
        finally
        {
            if (rented != null)
                ArrayPool<byte>.Shared.Return(rented);
        }

        state.Call(0, 1);
        return true;
    }

    protected override string GetCacheKey(string path)
    {
        var targetPath = Path.IsPathRooted(path)
            ? path
            : Path.Combine(GetWorkingDirectoryOrDefault(), path);

        return Path.GetFullPath(targetPath);
    }

    protected override bool TryGetAliasPath(string alias, [NotNullWhen(true)] out string? path)
    {
        if (Aliases != null && Aliases.TryGetValue(alias, out path))
        {
            return true;
        }
        path = null;
        return false;
    }
}
