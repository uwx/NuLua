using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NuLua.Luau;

namespace NuLua;

[StructLayout(LayoutKind.Auto)]
public readonly ref struct LuaStackValue : IEquatable<LuaStackValue>
{
}
