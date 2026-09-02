using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NuLua.Luau;

namespace NuLua;

public struct PrimitiveValue
{
    public int Id;
    public PrimitiveValueValue Primitive;
}

[InlineArray(24)]
public struct PrimitiveValueValue
{
    public byte Value;
}

[StructLayout(LayoutKind.Auto)]
public readonly struct LuaValue : IEquatable<LuaValue>, IDisposable
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

    public static LuaValue Nil => default;

    public static LuaValue FromNumber(double value)
    {
        return new(LuaValueType.Number, new() { NumberValue = value }, null);
    }

    public static LuaValue FromBoolean(bool value)
    {
        return new(LuaValueType.Boolean, new() { BooleanValue = value }, null);
    }

    public static LuaValue FromString(string value)
    {
        return new(LuaValueType.String, default, value);
    }

    public static LuaValue FromLightUserData(IntPtr value)
    {
        return new(LuaValueType.LightUserData, new() { PointerValue = value }, null);
    }

    public static LuaValue FromUserData(LuaUserData value)
    {
        return new(LuaValueType.UserData, default, value);
    }

    public static LuaValue FromVector(Vector3 value)
    {
        return new(LuaValueType.Vector, new() { VectorValue = value }, null);
    }

    public static LuaValue FromPrimitive<T>(T payload) where T : unmanaged, IPrimitive
    {
        return FromPrimitive(T.PrimitiveId, payload);
    }

    public static unsafe LuaValue FromPrimitive<T>(int id, T payload) where T : unmanaged
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

    public static LuaValue FromPrimitive(int id, ReadOnlySpan<byte> payload)
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

    public static LuaValue FromTable(LuaTable value)
    {
        return new(LuaValueType.Table, default, value);
    }

    public static LuaValue FromFunction(LuaFunction value)
    {
        return new(LuaValueType.Function, default, value);
    }

    public static LuaValue FromThread(LuauState value)
    {
        return new(LuaValueType.Thread, default, value);
    }

    public static LuaValue FromBuffer(ILuaObject value)
    {
        return new(LuaValueType.Buffer, default, value);
    }

    public static LuaValue FromClass(ILuaObject value)
    {
        return new(LuaValueType.Class, default, value);
    }

    public static LuaValue FromObject(ILuaObject value)
    {
        return new(LuaValueType.Object, default, value);
    }

    readonly LuaValueType type;
    readonly ValueUnion value;
    readonly object? reference;

    public LuaValueType Type => type;

    LuaValue(LuaValueType type, ValueUnion value, object? reference)
    {
        this.type = type;
        this.value = value;
        this.reference = reference;
    }

    public override string ToString()
    {
        return type switch
        {
            LuaValueType.Nil => "nil",
            LuaValueType.Boolean => value.BooleanValue ? "true" : "false",
            LuaValueType.LightUserData => $"lightuserdata: 0x{value.PointerValue:X}",
            LuaValueType.Number => value.NumberValue.ToString(),
            LuaValueType.Vector => VectorToString(value.VectorValue),
            LuaValueType.Primitive => $"primitive {value.Primitive.Id}",
            LuaValueType.String => ((string)reference!).ToString()!,
            LuaValueType.Table => ((LuaTable)reference!).ToString()!,
            LuaValueType.Function => ((LuaFunction)reference!).ToString()!,
            LuaValueType.UserData => ((LuaUserData)reference!).ToString()!,
            LuaValueType.Thread => ((LuauState)reference!).ToString()!,
            LuaValueType.Buffer => ((ILuaObject)reference!).ToString()!,
            LuaValueType.Class => ((ILuaObject)reference!).ToString()!,
            LuaValueType.Object => ((ILuaObject)reference!).ToString()!,
            _ => "",
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static string VectorToString(Vector3 vector)
    {
        return $"{vector.X}, {vector.Y}, {vector.Z}";
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
        if (typeof(T) == typeof(LuaValue))
        {
            var r = this;
            result = Unsafe.As<LuaValue, T>(ref r)!;
            return true;
        }

        switch (Type)
        {
            case LuaValueType.Nil:
                if (typeof(T) == typeof(object))
                {
                    result = Unsafe.NullRef<T>()!;
                    return true;
                }
                break;
            case LuaValueType.Boolean:
                if (typeof(T) == typeof(bool))
                {
                    var r = value.BooleanValue;
                    result = Unsafe.As<bool, T>(ref r)!;
                    return true;
                }
                if (typeof(T) == typeof(object))
                {
                    var r = (object)value.BooleanValue;
                    result = Unsafe.As<object, T>(ref r)!;
                    return true;
                }
                break;
            case LuaValueType.UserData:
                if (typeof(T) == typeof(LuaUserData))
                {
                    var r = (LuaUserData)reference!;
                    result = Unsafe.As<LuaUserData, T>(ref r)!;
                    return true;
                }
                if (typeof(ILuaObject).IsAssignableFrom(typeof(T)))
                {
                    var r = (ILuaObject)reference!;
                    result = Unsafe.As<ILuaObject, T>(ref r)!;
                    return true;
                }
                if (reference is LuaUserData ud)
                {
                    if (ud.TryRead(out result))
                        return true;
                }
                break;
            case LuaValueType.LightUserData:
                if (typeof(T) == typeof(IntPtr))
                {
                    var r = value.PointerValue;
                    result = Unsafe.As<IntPtr, T>(ref r)!;
                    return true;
                }
                if (typeof(T) == typeof(object))
                {
                    var r = (object)value.PointerValue;
                    result = Unsafe.As<object, T>(ref r)!;
                    return true;
                }
                break;
            case LuaValueType.Number:
                if (typeof(T) == typeof(double))
                {
                    var r = value.NumberValue;
                    result = Unsafe.As<double, T>(ref r)!;
                    return true;
                }
                if (typeof(T) == typeof(float))
                {
                    var r = (float)value.NumberValue;
                    result = Unsafe.As<float, T>(ref r)!;
                    return true;
                }
                if (typeof(T) == typeof(int) && MathEx.IsInteger(value.NumberValue))
                {
                    var r = (int)value.NumberValue;
                    result = Unsafe.As<int, T>(ref r)!;
                    return true;
                }
                if (typeof(T) == typeof(long) && MathEx.IsInteger(value.NumberValue))
                {
                    var r = (long)value.NumberValue;
                    result = Unsafe.As<long, T>(ref r)!;
                    return true;
                }
                if (
                    typeof(T) == typeof(uint)
                    && MathEx.IsInteger(value.NumberValue)
                    && value.NumberValue >= 0
                )
                {
                    var r = (uint)value.NumberValue;
                    result = Unsafe.As<uint, T>(ref r)!;
                    return true;
                }
                if (
                    typeof(T) == typeof(ulong)
                    && MathEx.IsInteger(value.NumberValue)
                    && value.NumberValue >= 0
                )
                {
                    var r = (ulong)value.NumberValue;
                    result = Unsafe.As<ulong, T>(ref r)!;
                    return true;
                }
                if (typeof(T) == typeof(object))
                {
                    var r = (object)value.NumberValue;
                    result = Unsafe.As<object, T>(ref r)!;
                    return true;
                }
                break;
            case LuaValueType.Vector:
                if (typeof(T) == typeof(Vector3))
                {
                    var r = value.VectorValue;
                    result = Unsafe.As<Vector3, T>(ref r)!;
                    return true;
                }
                if (typeof(T) == typeof(object))
                {
                    var r = (object)value.VectorValue;
                    result = Unsafe.As<object, T>(ref r)!;
                    return true;
                }
                break;
            case LuaValueType.Primitive:
                if (typeof(T) == typeof(PrimitiveValue))
                {
                    var r = value.Primitive;
                    result = Unsafe.As<PrimitiveValue, T>(ref r)!;
                    return true;
                }
                if (typeof(T) == typeof(byte[]))
                {
                    var r = ((ReadOnlySpan<byte>)value.Primitive.Primitive).ToArray();
                    result = Unsafe.As<byte[], T>(ref r)!;
                    return true;
                }
                if (typeof(T) == typeof(ReadOnlyMemory<byte>))
                {
                    ReadOnlyMemory<byte> r = ((ReadOnlySpan<byte>)value.Primitive.Primitive).ToArray();
                    result = Unsafe.As<ReadOnlyMemory<byte>, T>(ref r)!;
                    return true;
                }
                if (typeof(T) == typeof(object))
                {
                    var r = (object)((ReadOnlySpan<byte>)value.Primitive.Primitive).ToArray();
                    result = Unsafe.As<object, T>(ref r)!;
                    return true;
                }
                break;
            case LuaValueType.String:
                if (typeof(T) == typeof(string))
                {
                    var r = (string)reference!;
                    result = Unsafe.As<string, T>(ref r)!;
                    return true;
                }
                if (typeof(T) == typeof(object))
                {
                    var r = (object)(string)reference!;
                    result = Unsafe.As<object, T>(ref r)!;
                    return true;
                }
                break;
            case LuaValueType.Table:
                if (typeof(T) == typeof(LuaTable))
                {
                    var r = (LuaTable)reference!;
                    result = Unsafe.As<LuaTable, T>(ref r)!;
                    return true;
                }
                if (typeof(ILuaObject).IsAssignableFrom(typeof(T)))
                {
                    var r = (ILuaObject)reference!;
                    result = Unsafe.As<ILuaObject, T>(ref r)!;
                    return true;
                }
                if (typeof(T) == typeof(object))
                {
                    var r = (object)(LuaTable)reference!;
                    result = Unsafe.As<object, T>(ref r)!;
                    return true;
                }
                break;
            case LuaValueType.Function:
                if (typeof(T) == typeof(LuaFunction))
                {
                    var r = (LuaFunction)reference!;
                    result = Unsafe.As<LuaFunction, T>(ref r)!;
                    return true;
                }
                if (typeof(ILuaObject).IsAssignableFrom(typeof(T)))
                {
                    var r = (ILuaObject)reference!;
                    result = Unsafe.As<ILuaObject, T>(ref r)!;
                    return true;
                }
                if (typeof(T) == typeof(object))
                {
                    var r = (object)(LuaFunction)reference!;
                    result = Unsafe.As<object, T>(ref r)!;
                    return true;
                }
                break;
            case LuaValueType.Thread:
                if (typeof(T) == typeof(LuauState))
                {
                    var r = (LuauState)reference!;
                    result = Unsafe.As<LuauState, T>(ref r)!;
                    return true;
                }
                if (typeof(ILuaObject).IsAssignableFrom(typeof(T)))
                {
                    var r = (ILuaObject)reference!;
                    result = Unsafe.As<ILuaObject, T>(ref r)!;
                    return true;
                }
                if (typeof(T) == typeof(object))
                {
                    var r = (object)(LuauState)reference!;
                    result = Unsafe.As<object, T>(ref r)!;
                    return true;
                }
                break;
            case LuaValueType.Buffer:
            case LuaValueType.Class:
            case LuaValueType.Object:
                if (typeof(ILuaObject).IsAssignableFrom(typeof(T)))
                {
                    var r = (ILuaObject)reference!;
                    result = Unsafe.As<ILuaObject, T>(ref r)!;
                    return true;
                }
                if (typeof(T) == typeof(object))
                {
                    var r = reference!;
                    result = Unsafe.As<object, T>(ref r)!;
                    return true;
                }
                break;
        }

        Unsafe.SkipInit(out result);
        return false;
    }

    public bool Equals(LuaValue other)
    {
        if (type != other.type)
            return false;

        return type switch
        {
            LuaValueType.Nil => true,
            LuaValueType.Boolean => value.BooleanValue == other.value.BooleanValue,
            LuaValueType.LightUserData => value.PointerValue == other.value.PointerValue,
            LuaValueType.UserData => reference == other.reference,
            LuaValueType.Number => value.NumberValue == other.value.NumberValue,
            LuaValueType.Vector => value.VectorValue == other.value.VectorValue,
            LuaValueType.Primitive =>
                value.Primitive.Id == other.value.Primitive.Id
                && ((ReadOnlySpan<byte>)value.Primitive.Primitive).SequenceEqual(other.value.Primitive.Primitive),
            LuaValueType.String => ((string)reference!).Equals((string)other.reference!),
            _ => reference == other.reference,
        };
    }

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is LuaValue other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(type, value, reference);
    }

    public static bool operator ==(LuaValue left, LuaValue right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(LuaValue left, LuaValue right)
    {
        return !(left == right);
    }

    public static implicit operator LuaValue(double value) => FromNumber(value);

    public static implicit operator LuaValue(bool value) => FromBoolean(value);

    public static implicit operator LuaValue(string value) => FromString(value);

    public static implicit operator LuaValue(Vector3 value) => FromVector(value);

    public static implicit operator LuaValue(LuaFunction value) => FromFunction(value);

    public static implicit operator LuaValue(LuaTable value) => FromTable(value);

    public static implicit operator LuaValue(LuaUserData value) => FromUserData(value);

    public void Dispose()
    {
        if (reference is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
