using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace NuLua;

[StructLayout(LayoutKind.Auto)]
public readonly struct LuaValue : IEquatable<LuaValue>
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
    }

    public static readonly LuaValue Nil = default;

    public static LuaValue FromNumber(double value)
    {
        return new(LuaType.Number, new() { NumberValue = value }, null);
    }

    public static LuaValue FromBoolean(bool value)
    {
        return new(LuaType.Boolean, new() { BooleanValue = value }, null);
    }

    public static LuaValue FromString(string value)
    {
        return new(LuaType.String, default, value);
    }

    public static LuaValue FromLightUserData(IntPtr value)
    {
        return new(LuaType.LightUserData, new() { PointerValue = value }, null);
    }

    public static LuaValue FromUserData(ILuaUserData value)
    {
        return new(LuaType.UserData, default, value);
    }

    public static LuaValue FromVector(Vector3 value)
    {
        return new(LuaType.Vector, new() { VectorValue = value }, null);
    }

    public static LuaValue FromTable(LuaTable value)
    {
        return new(LuaType.Table, default, value);
    }

    public static LuaValue FromFunction(LuaFunction value)
    {
        return new(LuaType.Function, default, value);
    }

    public static LuaValue FromThread(ILuaState value)
    {
        return new(LuaType.Thread, default, value);
    }

    public static LuaValue FromBuffer(ILuaBuffer value)
    {
        return new(LuaType.Buffer, default, value);
    }

    readonly LuaType type;
    readonly ValueUnion value;
    readonly object? reference;

    public LuaType Type => type;

    LuaValue(LuaType type, ValueUnion value, object? reference)
    {
        this.type = type;
        this.value = value;
        this.reference = reference;
    }

    public override string ToString()
    {
        return type switch
        {
            LuaType.Nil => "nil",
            LuaType.Boolean => value.BooleanValue ? "true" : "false",
            LuaType.LightUserData => $"lightuserdata: 0x{value.PointerValue:X}",
            LuaType.Number => value.NumberValue.ToString(),
            LuaType.Vector => VectorToString(value.VectorValue),
            LuaType.String => ((string)reference!).ToString(),
            LuaType.Table => ((LuaTable)reference!).ToString(),
            LuaType.Function => ((LuaFunction)reference!).ToString()!,
            LuaType.UserData => ((ILuaUserData)reference!).ToString(),
            LuaType.Thread => ((ILuaState)reference!).ToString()!,
            LuaType.Buffer => ((ILuaBuffer)reference!).ToString()!,
            _ => "",
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static string VectorToString(Vector3 vector)
    {
        return $"{vector.X}, {vector.Y}, {vector.Z}";
    }

    public bool IsNil => Type == LuaType.Nil;

    public T Read<T>()
    {
        if (TryRead<T>(out var result))
            return result;
        throw new InvalidOperationException($"Cannot convert {Type} to {typeof(T).Name}");
    }

    public T UnsafeRead<T>()
    {
        if (TryRead<T>(out var result))
            return result;
        Unsafe.SkipInit(out result);
        return result!;
    }

    public bool TryRead<T>(out T result)
    {
        if (typeof(T) == typeof(LuaValue))
        {
            var r = this;
            result = Unsafe.As<LuaValue, T>(ref r);
            return true;
        }

        switch (Type)
        {
            case LuaType.Nil:
                if (typeof(T) == typeof(object))
                {
                    result = Unsafe.NullRef<T>();
                    return true;
                }
                break;
            case LuaType.Boolean:
                if (typeof(T) == typeof(bool))
                {
                    var r = value.BooleanValue;
                    result = Unsafe.As<bool, T>(ref r);
                    return true;
                }
                if (typeof(T) == typeof(object))
                {
                    var r = (object)value.BooleanValue;
                    result = Unsafe.As<object, T>(ref r);
                    return true;
                }
                break;
            case LuaType.UserData:
                if (typeof(T) == typeof(ILuaUserData))
                {
                    var r = (ILuaUserData)reference!;
                    result = Unsafe.As<ILuaUserData, T>(ref r);
                    return true;
                }
                if (typeof(T) == typeof(object))
                {
                    var r = (object)(ILuaUserData)reference!;
                    result = Unsafe.As<object, T>(ref r);
                    return true;
                }
                if (
                    reference is ILuaUserData userData
                    && userData.TryRead<T>(out var userDataResult)
                )
                {
                    result = userDataResult;
                    return true;
                }
                break;
            case LuaType.LightUserData:
                if (typeof(T) == typeof(IntPtr))
                {
                    var r = value.PointerValue;
                    result = Unsafe.As<IntPtr, T>(ref r);
                    return true;
                }
                if (typeof(T) == typeof(object))
                {
                    var r = (object)value.PointerValue;
                    result = Unsafe.As<object, T>(ref r);
                    return true;
                }
                break;
            case LuaType.Number:
                if (typeof(T) == typeof(double))
                {
                    var r = value.NumberValue;
                    result = Unsafe.As<double, T>(ref r);
                    return true;
                }
                if (typeof(T) == typeof(float))
                {
                    var r = (float)value.NumberValue;
                    result = Unsafe.As<float, T>(ref r);
                    return true;
                }
                if (typeof(T) == typeof(int) && MathEx.IsInteger(value.NumberValue))
                {
                    var r = (int)value.NumberValue;
                    result = Unsafe.As<int, T>(ref r);
                    return true;
                }
                if (typeof(T) == typeof(long) && MathEx.IsInteger(value.NumberValue))
                {
                    var r = (long)value.NumberValue;
                    result = Unsafe.As<long, T>(ref r);
                    return true;
                }
                if (
                    typeof(T) == typeof(uint)
                    && MathEx.IsInteger(value.NumberValue)
                    && value.NumberValue >= 0
                )
                {
                    var r = (uint)value.NumberValue;
                    result = Unsafe.As<uint, T>(ref r);
                    return true;
                }
                if (
                    typeof(T) == typeof(ulong)
                    && MathEx.IsInteger(value.NumberValue)
                    && value.NumberValue >= 0
                )
                {
                    var r = (ulong)value.NumberValue;
                    result = Unsafe.As<ulong, T>(ref r);
                    return true;
                }
                if (typeof(T) == typeof(object))
                {
                    var r = (object)value.NumberValue;
                    result = Unsafe.As<object, T>(ref r);
                    return true;
                }
                break;
            case LuaType.Vector:
                if (typeof(T) == typeof(Vector3))
                {
                    var r = value.VectorValue;
                    result = Unsafe.As<Vector3, T>(ref r);
                    return true;
                }
                if (typeof(T) == typeof(object))
                {
                    var r = (object)value.VectorValue;
                    result = Unsafe.As<object, T>(ref r);
                    return true;
                }
                break;
            case LuaType.String:
                if (typeof(T) == typeof(string))
                {
                    var r = (string)reference!;
                    result = Unsafe.As<string, T>(ref r);
                    return true;
                }
                if (typeof(T) == typeof(object))
                {
                    var r = (object)(string)reference!;
                    result = Unsafe.As<object, T>(ref r);
                    return true;
                }
                break;
            case LuaType.Table:
                if (typeof(T) == typeof(LuaTable))
                {
                    var r = (LuaTable)reference!;
                    result = Unsafe.As<LuaTable, T>(ref r);
                    return true;
                }
                if (typeof(ILuaObject).IsAssignableFrom(typeof(T)))
                {
                    var r = (ILuaObject)reference!;
                    result = Unsafe.As<ILuaObject, T>(ref r);
                    return true;
                }
                if (typeof(T) == typeof(object))
                {
                    var r = (object)(LuaTable)reference!;
                    result = Unsafe.As<object, T>(ref r);
                    return true;
                }
                break;
            case LuaType.Function:
                if (typeof(T) == typeof(LuaFunction))
                {
                    var r = (LuaFunction)reference!;
                    result = Unsafe.As<LuaFunction, T>(ref r);
                    return true;
                }
                if (typeof(ILuaObject).IsAssignableFrom(typeof(T)))
                {
                    var r = (ILuaObject)reference!;
                    result = Unsafe.As<ILuaObject, T>(ref r);
                    return true;
                }
                if (typeof(T) == typeof(object))
                {
                    var r = (object)(LuaFunction)reference!;
                    result = Unsafe.As<object, T>(ref r);
                    return true;
                }
                break;
            case LuaType.Thread:
                if (typeof(T) == typeof(ILuaState))
                {
                    var r = (ILuaState)reference!;
                    result = Unsafe.As<ILuaState, T>(ref r);
                    return true;
                }
                if (typeof(ILuaObject).IsAssignableFrom(typeof(T)))
                {
                    var r = (ILuaObject)reference!;
                    result = Unsafe.As<ILuaObject, T>(ref r);
                    return true;
                }
                if (typeof(T) == typeof(object))
                {
                    var r = (object)(ILuaState)reference!;
                    result = Unsafe.As<object, T>(ref r);
                    return true;
                }
                break;
            case LuaType.Buffer:
                if (typeof(T) == typeof(ILuaBuffer))
                {
                    var r = (ILuaBuffer)reference!;
                    result = Unsafe.As<ILuaBuffer, T>(ref r);
                    return true;
                }
                if (typeof(ILuaObject).IsAssignableFrom(typeof(T)))
                {
                    var r = (ILuaObject)reference!;
                    result = Unsafe.As<ILuaObject, T>(ref r);
                    return true;
                }
                if (typeof(T) == typeof(object))
                {
                    var r = (object)(ILuaBuffer)reference!;
                    result = Unsafe.As<object, T>(ref r);
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
            LuaType.Nil => true,
            LuaType.Boolean => value.BooleanValue == other.value.BooleanValue,
            LuaType.LightUserData or LuaType.UserData => value.PointerValue
                == other.value.PointerValue,
            LuaType.Number => value.NumberValue == other.value.NumberValue,
            LuaType.Vector => value.VectorValue == other.value.VectorValue,
            LuaType.String => ((string)reference!).Equals((string)other.reference!),
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
}
