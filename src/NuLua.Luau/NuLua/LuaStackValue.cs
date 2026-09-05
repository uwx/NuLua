using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NuLua.Luau;

namespace NuLua;

[StructLayout(LayoutKind.Auto)]
public readonly ref struct LuaArgumentValue : IEquatable<LuaArgumentValue>
{
    [StructLayout(LayoutKind.Explicit)]
    struct ValueUnion
    {
        [FieldOffset(0)]
        public bool BooleanValue;

        [FieldOffset(0)]
        public double NumberValue;

        [FieldOffset(0)]
        public IntPtr PointerValue;

        [FieldOffset(0)]
        public Vector3 VectorValue;

        [FieldOffset(0)]
        public PrimitiveValue Primitive;

        [FieldOffset(0)]
        public int StackPointer;
    }

    /// <summary>
    /// Maximum byte length of a primitive payload, mirroring Luau's LUA_PRIMITIVE_SIZE.
    /// </summary>
    public const int PrimitivePayloadSize = 24;

    /// <summary>
    /// Number of distinct primitive identifiers (0..PrimitiveIdLimit-1),
    /// mirroring Luau's LUA_PRIMITIVE_LIMIT.
    /// </summary>
    public const int PrimitiveIdLimit = 16;

    public static LuaArgumentValue Nil => default;

    public static LuaArgumentValue FromNumber(double value)
    {
        return new(LuaValueType.Number, new() { NumberValue = value }, null);
    }

    public static LuaArgumentValue FromBoolean(bool value)
    {
        return new(LuaValueType.Boolean, new() { BooleanValue = value }, null);
    }

    public static LuaArgumentValue FromLightUserData(IntPtr value)
    {
        return new(LuaValueType.LightUserData, new() { PointerValue = value }, null);
    }

    public static LuaArgumentValue FromVector(Vector3 value)
    {
        return new(LuaValueType.Vector, new() { VectorValue = value }, null);
    }

    public static LuaArgumentValue FromPrimitive<T>(T payload) where T : unmanaged, IPrimitive
    {
        return FromPrimitive(T.PrimitiveId, payload);
    }

    public static LuaArgumentValue FromStackPointer(LuaValueType type, LuauState state, int stackPointer)
    {
        return new(type, new() { StackPointer = stackPointer }, state);
    }

    public static unsafe LuaArgumentValue FromPrimitive<T>(int id, T payload) where T : unmanaged
    {
        if (id < 0 || id >= PrimitiveIdLimit)
        {
            throw new ArgumentOutOfRangeException(
                nameof(id),
                $"Primitive id must be in the range 0..{PrimitiveIdLimit - 1}."
            );
        }
        if (sizeof(T) > PrimitivePayloadSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(payload),
                $"Primitive payload must be at most {PrimitivePayloadSize} bytes."
            );
        }
        PrimitiveValueValue value = default;
        Span<byte> valueSpan = value;
        MemoryMarshal.Write(valueSpan, payload);
        return new(LuaValueType.Primitive, new() { Primitive = new PrimitiveValue() { Id = id, Primitive = value } }, null);
    }

    public static LuaArgumentValue FromPrimitive(int id, ReadOnlySpan<byte> payload)
    {
        if (id < 0 || id >= PrimitiveIdLimit)
        {
            throw new ArgumentOutOfRangeException(
                nameof(id),
                $"Primitive id must be in the range 0..{PrimitiveIdLimit - 1}."
            );
        }
        if (payload.Length > PrimitivePayloadSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(payload),
                $"Primitive payload must be at most {PrimitivePayloadSize} bytes."
            );
        }
        PrimitiveValueValue value = default;
        Span<byte> valueSpan = value;
        payload.CopyTo(valueSpan);
        return new(LuaValueType.Primitive, new() { Primitive = new PrimitiveValue() { Id = id, Primitive = value } }, null);
    }
    
    readonly LuaValueType type;
    readonly ValueUnion value;
    readonly LuauState? state;

    public LuaValueType Type => type;

    LuaArgumentValue(LuaValueType type, ValueUnion value, LuauState? state)
    {
        this.type = type;
        this.value = value;
    }
    
    public override string ToString()
    {
        return type switch
        {
            LuaValueType.Nil => "nil",
            LuaValueType.Boolean => value.BooleanValue ? "true" : "false",
            LuaValueType.LightUserData => $"lightuserdata: 0x{value.PointerValue:X}",
            LuaValueType.Number => value.NumberValue.ToString(CultureInfo.InvariantCulture),
            LuaValueType.Vector => LuaRefValue.VectorToString(value.VectorValue),
            LuaValueType.Primitive => $"primitive {value.Primitive.Id}",
            LuaValueType.String => state!.ToString(value.StackPointer),
            LuaValueType.Table => state!.ToString(value.StackPointer),
            LuaValueType.Function => state!.ToString(value.StackPointer),
            LuaValueType.UserData => state!.ToString(value.StackPointer),
            LuaValueType.Thread => state!.ToString(value.StackPointer),
            LuaValueType.Buffer => state!.ToString(value.StackPointer),
            LuaValueType.Class => state!.ToString(value.StackPointer),
            LuaValueType.Object => state!.ToString(value.StackPointer),
            _ => "",
        };
    }

    public bool IsNil => Type == LuaValueType.Nil;

    public bool TryGetPrimitiveId(out int primitiveId)
    {
        if (Type == LuaValueType.Primitive)
        {
            primitiveId = value.Primitive.Id;
            return true;
        }

        primitiveId = 0;
        return false;
    }

    public T Read<T>()
    {
        if (TryRead<T>(out var result))
            return result;
        throw new InvalidOperationException($"Cannot convert {Type} to {typeof(T).Name}");
    }

    [return: NotNullIfNotNull(nameof(@default))]
    public T? ReadOrDefault<T>(T? @default = default)
    {
        if (!TryRead<T>(out var result))
            return @default;

        return result;
    }

    public T UnsafeRead<T>()
    {
        if (TryRead<T>(out var result))
            return result;
        Unsafe.SkipInit(out result);
        return result!;
    }

    public bool TryReadPrimitive<T>(out T result) where T : unmanaged, IPrimitive
    {
        return TryReadPrimitive(T.PrimitiveId, out result);
    }

    public unsafe bool TryReadPrimitive<T>(int expectedId, out T result) where T : unmanaged
    {
        if (Type != LuaValueType.Primitive)
        {
            result = default;
            return false;
        }

        if (sizeof(T) > PrimitivePayloadSize)
        {
            result = default;
            return false;
        }
        
        var r = value.Primitive;
        if (r.Id != expectedId)
        {
            result = default;
            return false;
        }
        
        result = Unsafe.As<PrimitiveValueValue, T>(ref r.Primitive);
        return true;
    }

    public unsafe bool TryReadPrimitive<T>(out T result, out int id) where T : unmanaged
    {
        if (Type != LuaValueType.Primitive)
        {
            result = default;
            id = 0;
            return false;
        }

        if (sizeof(T) > PrimitivePayloadSize)
        {
            result = default;
            id = 0;
            return false;
        }
        
        var r = value.Primitive;
        id = r.Id;
        result = Unsafe.As<PrimitiveValueValue, T>(ref r.Primitive);
        return true;
    }

    public T ReadPrimitive<T>() where T : unmanaged, IPrimitive
    {
        return ReadPrimitive<T>(T.PrimitiveId);
    }

    public T ReadPrimitive<T>(int expectedId) where T : unmanaged
    {
        if (TryReadPrimitive<T>(expectedId, out var result))
            return result;
        throw new InvalidOperationException($"Cannot convert {Type} to {typeof(T).Name}");
    }

    public T ReadPrimitive<T>(out int id) where T : unmanaged
    {
        if (TryReadPrimitive<T>(out var result, out id))
            return result;
        throw new InvalidOperationException($"Cannot convert {Type} to {typeof(T).Name}");
    }

    public bool TryRead<T>([NotNullWhen(true)] out T? result)
    {
        if (typeof(T) == typeof(LuaArgumentValue))
        {
            var r = this;
            result = Unsafe.As<LuaArgumentValue, T>(ref r)!;
            return true;
        }

        switch (Type)
        {
            case LuaValueType.Nil:
                if (typeof(T) == typeof(object))
                {
                    result = default!;
                    return true;
                }
                break;
            case LuaValueType.Boolean:
                if (typeof(T) == typeof(bool) || typeof(T) == typeof(object))
                {
                    result = (T)(object)value.BooleanValue;
                    return true;
                }
                break;
            case LuaValueType.UserData:
                if (typeof(T) == typeof(LuaUserDataRef) || typeof(ILuaObjectRef).IsAssignableFrom(typeof(T)))
                {
                    state!.PushValue(value.StackPointer);
                    var reference = state.Ref();
                    result = (T)(object)new LuaUserDataRef(state, reference);
                    return true;
                }

                result = default;

                state!.PushValue(value.StackPointer);
                try
                {
                    unsafe
                    {
                        if (state.RawLen(-1) < Unsafe.SizeOf<ManagedUserData>())
                        {
                            return false;
                        }

                        var data = state.ToUserDataPointer(-1);
                        ref var header = ref Unsafe.AsRef<ManagedUserData>((void*)data);
                        if (header.Handle == 0)
                        {
                            return false;
                        }

                        var handle = GCHandle.FromIntPtr(header.Handle);
                        if (!handle.IsAllocated)
                        {
                            return false;
                        }

                        if (handle.Target is T typed)
                        {
                            result = typed;
                            return true;
                        }

                        return false;
                    }
                }
                finally
                {
                    state.Pop(1);
                }
                break;
            case LuaValueType.LightUserData:
                if (typeof(T) == typeof(IntPtr) || typeof(T) == typeof(object))
                {
                    result = (T)(object)value.PointerValue;
                    return true;
                }
                break;
            case LuaValueType.Number:
                if (typeof(T) == typeof(double))
                {
                    result = (T)(object)value.NumberValue;
                    return true;
                }
                if (typeof(T) == typeof(float))
                {
                    result = (T)(object)(float)value.NumberValue;
                    return true;
                }
                if (typeof(T) == typeof(int) && MathEx.IsInteger(value.NumberValue))
                {
                    result = (T)(object)(int)value.NumberValue;
                    return true;
                }
                if (typeof(T) == typeof(long) && MathEx.IsInteger(value.NumberValue))
                {
                    result = (T)(object)(long)value.NumberValue;
                    return true;
                }
                if (
                    typeof(T) == typeof(uint)
                    && MathEx.IsInteger(value.NumberValue)
                    && value.NumberValue >= 0
                )
                {
                    result = (T)(object)(uint)value.NumberValue;
                    return true;
                }
                if (
                    typeof(T) == typeof(ulong)
                    && MathEx.IsInteger(value.NumberValue)
                    && value.NumberValue >= 0
                )
                {
                    result = (T)(object)(ulong)value.NumberValue;
                    return true;
                }
                if (typeof(T) == typeof(object))
                {
                    result = (T)(object)value.NumberValue;
                    return true;
                }
                break;
            case LuaValueType.Vector:
                if (typeof(T) == typeof(Vector3) || typeof(T) == typeof(object))
                {
                    result = (T)(object)value.VectorValue;
                    return true;
                }
                break;
            case LuaValueType.Primitive:
                if (typeof(T) == typeof(PrimitiveValue))
                {
                    result = (T)(object)value.Primitive;
                    return true;
                }
                if (typeof(T) == typeof(byte[]))
                {
                    result = (T)(object)((ReadOnlySpan<byte>)value.Primitive.Primitive).ToArray();
                    return true;
                }
                if (typeof(T) == typeof(ReadOnlyMemory<byte>))
                {
                    ReadOnlyMemory<byte> r = ((ReadOnlySpan<byte>)value.Primitive.Primitive).ToArray();
                    result = (T)(object)r;
                    return true;
                }
                if (typeof(T) == typeof(object))
                {
                    result = (T)(object)((ReadOnlySpan<byte>)value.Primitive.Primitive).ToArray();
                    return true;
                }
                break;
            case LuaValueType.String:
                if (typeof(T) == typeof(string) || typeof(T) == typeof(object))
                {
                    result = (T)(object)state!.ToString(value.StackPointer);
                    return true;
                }
                break;
            case LuaValueType.Table:
                if (typeof(T) == typeof(LuaTableRef) || typeof(ILuaObjectRef).IsAssignableFrom(typeof(T)) || typeof(T) == typeof(object))
                {
                    state!.PushValue(value.StackPointer);
                    var reference = state.Ref();
                    result = (T)(object)new LuaTableRef(state, reference);
                    return true;
                }
                break;
            case LuaValueType.Function:
                if (typeof(T) == typeof(LuaFunctionRef) || typeof(ILuaObjectRef).IsAssignableFrom(typeof(T)) || typeof(T) == typeof(object))
                {
                    state!.PushValue(value.StackPointer);
                    var reference = state.Ref();
                    result = (T)(object)new LuaFunctionRef(state, reference);
                    return true;
                }
                break;
            case LuaValueType.Thread:
                if (typeof(T) == typeof(LuauState) || typeof(ILuaObjectRef).IsAssignableFrom(typeof(T)) || typeof(T) == typeof(object))
                {
                    // ToThread() already keeps the coroutine alive via its own
                    // registry reference, so no extra Ref() is taken here (and none
                    // would ever be released).
                    var thread = state!.ToThread(value.StackPointer);
                    result = (T)(object)LuaRefValue.FromThread(thread);
                    return true;
                }
                break;
            case LuaValueType.Buffer:
                if (typeof(T) == typeof(LuauBufferRef) || typeof(ILuaObjectRef).IsAssignableFrom(typeof(T)) || typeof(T) == typeof(object))
                {
                    state!.PushValue(value.StackPointer);
                    var reference = state.Ref();
                    result = (T)(object)new LuauBufferRef(state, reference);
                    return true;
                }
                break;
            case LuaValueType.Class:
                if (typeof(T) == typeof(LuauClassRef) || typeof(ILuaObjectRef).IsAssignableFrom(typeof(T)) || typeof(T) == typeof(object))
                {
                    state!.PushValue(value.StackPointer);
                    var reference = state.Ref();
                    result = (T)(object)new LuauClassRef(state, reference);
                    return true;
                }
                break;
            case LuaValueType.Object:
                if (typeof(T) == typeof(LuauObjectRef) || typeof(ILuaObjectRef).IsAssignableFrom(typeof(T)) || typeof(T) == typeof(object))
                {
                    state!.PushValue(value.StackPointer);
                    var reference = state.Ref();
                    result = (T)(object)new LuauObjectRef(state, reference);
                    return true;
                }
                break;
        }

        Unsafe.SkipInit(out result);
        return false;
    }

    public bool Equals(LuaArgumentValue other)
    {
        if (type != other.type)
            return false;

        return type switch
        {
            LuaValueType.Nil => true,
            LuaValueType.Boolean => value.BooleanValue == other.value.BooleanValue,
            LuaValueType.LightUserData => value.PointerValue == other.value.PointerValue,
            LuaValueType.UserData => state!.RawEqual(value.StackPointer, other.value.StackPointer),
            LuaValueType.Number => value.NumberValue == other.value.NumberValue,
            LuaValueType.Vector => value.VectorValue == other.value.VectorValue,
            LuaValueType.Primitive =>
                value.Primitive.Id == other.value.Primitive.Id
                && ((ReadOnlySpan<byte>)value.Primitive.Primitive).SequenceEqual(other.value.Primitive.Primitive),
            LuaValueType.String => state!.RawEqual(value.StackPointer, other.value.StackPointer),
            LuaValueType.Table => state!.RawEqual(value.StackPointer, other.value.StackPointer),
            LuaValueType.Function => state!.RawEqual(value.StackPointer, other.value.StackPointer),
            LuaValueType.Thread => state!.RawEqual(value.StackPointer, other.value.StackPointer),
            LuaValueType.Buffer => state!.RawEqual(value.StackPointer, other.value.StackPointer),
            LuaValueType.Class => state!.RawEqual(value.StackPointer, other.value.StackPointer),
            LuaValueType.Object => state!.RawEqual(value.StackPointer, other.value.StackPointer),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(type, value);
    }

    public static bool operator ==(LuaArgumentValue left, LuaArgumentValue right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(LuaArgumentValue left, LuaArgumentValue right)
    {
        return !(left == right);
    }

    public static implicit operator LuaArgumentValue(double value) => FromNumber(value);

    public static implicit operator LuaArgumentValue(bool value) => FromBoolean(value);

    public static implicit operator LuaArgumentValue(string value) => FromString(value);

    public static implicit operator LuaArgumentValue(Vector3 value) => FromVector(value);

}
