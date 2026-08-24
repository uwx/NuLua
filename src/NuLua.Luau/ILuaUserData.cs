using System.Collections;
using System.Numerics;
using NuLua.Luau;

namespace NuLua;

/// <summary>
/// Metamethods that an <see cref="ILuaUserData"/> type opts into. The per-type metatable
/// builder only installs the metamethods listed here.
/// </summary>
[Flags]
public enum LuaUserDataMetamethods
{
    /// <summary>No metamethods (opaque userdata).</summary>
    None = 0,

    /// <summary><c>__index</c> — dispatched to <see cref="ILuaUserData.TryGetIndex"/>.</summary>
    Index = 1 << 0,

    /// <summary><c>__newindex</c> — dispatched to <see cref="ILuaUserData.TrySetIndex"/>.</summary>
    NewIndex = 1 << 1,

    /// <summary><c>__len</c> — dispatched to <see cref="ILuaUserData.Length"/>.</summary>
    Length = 1 << 2,

    /// <summary><c>__tostring</c> — dispatched to <see cref="ILuaUserData.ToLuaString"/>.</summary>
    ToString = 1 << 3,

    /// <summary>
    /// <c>__iter</c> (generic <c>for .. in</c> loop) — driven by <see cref="ILuaUserData.GetIterator"/>.
    /// Luau has no <c>__pairs</c> metamethod and its base <c>pairs()</c>/<c>ipairs()</c> reject userdata,
    /// so <c>__iter</c> is the only userdata iteration path.
    /// </summary>
    Iter = 1 << 4,
    
    // Relational operators
    Eq = 1 << 5,
    Lt = 1 << 6,
    Le = 1 << 7,
    
    // Arithmetic operators
    Unm = 1 << 8,
    Add = 1 << 9,
    Sub = 1 << 10,
    Mul = 1 << 11,
    Div = 1 << 12,
    Idiv = 1 << 13,
    Mod = 1 << 14,
    Pow = 1 << 15,
}

/// <summary>
/// A managed object that can be exposed to Luau as a userdata carrying a per-type metatable.
///
/// This is the NuLua replacement for the managed interpreter's <c>ILuaUserData</c>. Instead of
/// exposing a <c>LuaTable</c> metatable (which is state-bound in NuLua), each type declares the
/// metamethods it supports (<see cref="SupportedMetamethods"/>) and implements the corresponding
/// dispatch methods. The runtime builds one metatable per type per state (cached) and installs
/// C closures that recover the managed object from the userdata's GCHandle and call back into
/// these methods.
///
/// Dispatch methods receive the <see cref="LuauState"/> and operate on <see cref="LuaValue"/>
/// directly, so it is up to each implementor how values are marshalled to/from Lua (including
/// wrapping nested <see cref="ILuaUserData"/> objects as userdata).
/// </summary>
public interface ILuaUserData<T> where T : ILuaUserData<T>
{
    /// <summary>
    /// Backs the <c>__index</c> metamethod. Returns <see langword="true"/> and sets
    /// <paramref name="value"/> when <paramref name="key"/> is handled; otherwise returns
    /// <see langword="false"/> and Lua yields <c>nil</c>. Use <paramref name="state"/> to marshal
    /// the result (e.g. wrap a nested object as userdata).
    /// </summary>
    bool TryGetIndex(LuauState state, LuaValue key, out LuaValue value)
    {
        value = default;
        return false;
    }

    /// <summary>
    /// Backs the <c>__newindex</c> metamethod. Returns <see langword="true"/> when
    /// <paramref name="key"/> was set; <see langword="false"/> for read-only or unknown keys.
    /// <paramref name="state"/> is provided for any marshalling the value requires.
    /// </summary>
    bool TrySetIndex(LuauState state, LuaValue key, LuaValue value)
    {
        return false;
    }
    
    // implement these usually

    static virtual bool operator ==(T? left, T? right) => false;
    static virtual bool operator !=(T? left, T? right) => false;
    static virtual bool operator <(T left, T right) => false;
    static virtual bool operator <=(T left, T right) => false;
    static virtual bool operator >(T left, T right) => false;
    static virtual bool operator >=(T left, T right) => false;
    static virtual T operator -(T value) => default!;
    static virtual T operator +(T left, T right) => default!;
    static virtual T operator -(T left, T right) => default!;
    static virtual T operator *(T left, T right) => default!;
    static virtual T operator /(T left, T right) => default!;
    static virtual T operator %(T left, T right) => default!;
    static virtual T FloorDivide(T self, T other) => default!;
    static virtual T Power(T self, T other) => default!;
    
    // implement if you want access to the raw values
    
    static virtual bool Equals(LuauState state, T self, T other) => self == other;

    static virtual bool LessThan(LuauState state, T self, T other) => self < other;

    static virtual bool LessThanOrEqual(LuauState state, T self, T other) => self <= other;

    static virtual LuaValue UnaryMinus(LuauState state, T self) => state.CreateUserData(-self);

    static virtual LuaValue Add(LuauState state, T self, T other) => state.CreateUserData(self + other);

    static virtual LuaValue Subtract(LuauState state, T self, T other) => state.CreateUserData(self - other);

    static virtual LuaValue Multiply(LuauState state, T self, T other) => state.CreateUserData(self * other);

    static virtual LuaValue Divide(LuauState state, T self, T other) => state.CreateUserData(self / other);

    static virtual LuaValue FloorDivide(LuauState state, T self, T other) => state.CreateUserData(T.FloorDivide(self, other));

    static virtual LuaValue Modulus(LuauState state, T self, T other) => state.CreateUserData(self % other);

    static virtual LuaValue Power(LuauState state, T self, T other) => state.CreateUserData(T.Power(self, other));

    /// <summary>
    /// Backs the <c>__len</c> metamethod. Only consulted when
    /// <see cref="LuaUserDataMetamethods.Length"/> is set; <see langword="null"/> means "no length".
    /// </summary>
    long? Length => null;

    /// <summary>
    /// Backs the <c>__tostring</c> metamethod. Only consulted when
    /// <see cref="LuaUserDataMetamethods.ToString"/> is set; <see langword="null"/> falls back to
    /// <see cref="object.ToString"/>.
    /// </summary>
    string? ToLuaString() => null;

    /// <summary>
    /// Backs the <c>__iter</c> metamethod (generic <c>for .. in</c>). Only consulted when
    /// <see cref="LuaUserDataMetamethods.Iter"/> is set. Returns an enumerator of (key, value)
    /// pairs already marshalled to <see cref="LuaValue"/>, or <see langword="null"/> to signal
    /// "no iteration". Use <paramref name="state"/> for any marshalling needed.
    /// </summary>
    IEnumerator<KeyValuePair<LuaValue, LuaValue>>? GetIterator(LuauState state) => null;

    /// <summary>
    /// Declares which metamethods should be installed into the per-type metatable for this type.
    /// Used once per (state, type) to build the cached metatable.
    /// </summary>
    LuaUserDataMetamethods SupportedMetamethods { get; }
}
