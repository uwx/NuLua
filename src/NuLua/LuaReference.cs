using System.Runtime.InteropServices;

namespace NuLua;

[StructLayout(LayoutKind.Auto)]
public record struct LuaReference(int Id, int TableIndex);
