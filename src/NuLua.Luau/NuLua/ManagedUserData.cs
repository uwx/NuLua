using System.Runtime.InteropServices;

namespace NuLua;

/// <summary>
/// Payload header stored at the start of a managed-object userdata (created via
/// <c>CreateUserData</c> in the Luau backend).
///
/// Layout: <see cref="Handle"/> (pointer-size)
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct ManagedUserData
{
    /// <summary>GCHandle (from <see cref="GCHandle.ToIntPtr"/>) to the managed object, or 0.</summary>
    public nint Handle;

    /// <summary>Allocates a strong GCHandle over <paramref name="value"/> and builds the header.</summary>
    public static ManagedUserData Create(object value)
    {
        var handle = GCHandle.Alloc(value);
        return new ManagedUserData
        {
            Handle = GCHandle.ToIntPtr(handle),
        };
    }
}
