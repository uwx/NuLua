using System.Numerics;

namespace NuLua;

/// <summary>
/// Optional capability implemented by state flavors that support Luau's extended value types —
/// primitives (<see cref="LuaValueType.Primitive"/>) and vectors (<see cref="LuaValueType.Vector"/>).
/// The shared <see cref="LuaStateExtensions.Push"/> dispatches such <see cref="LuaValue"/>s here;
/// flavors that don't implement this (the classic Lua 5.1-5.4/JIT states) reject them.
/// </summary>
public interface ILuauValueState : ILuaState
{
    /// <summary>Pushes a primitive with the given id and raw payload (see <see cref="LuaValue.FromPrimitive"/>).</summary>
    void PushPrimitive(int id, Span<byte> payload);

    /// <summary>Pushes a Luau vector.</summary>
    void PushVector(Vector3 value);
}
