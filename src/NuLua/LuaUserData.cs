using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace NuLua;

public sealed class LuaUserData(ILuaState state, LuaReference reference) : LuaObject(state, reference)
{
    readonly ILuaState state = state;

    public readonly struct UserValues(LuaUserData userData)
    {
        public LuaValue this[int index]
        {
            get
            {
                userData.state.PushValue(userData.Reference);
                if (!userData.state.TryGetUserValue(-1, index, out _))
                {
                    userData.state.Pop(1);
                    ThrowIndexOutOfRange(index);
                }
                var value = userData.state.Pop();
                return value;
            }
            set
            {
                userData.state.PushValue(userData.Reference);
                userData.state.Push(value);
                if (!userData.state.TrySetUserValue(-2, index, out _))
                {
                    userData.state.Pop(2);
                    ThrowIndexOutOfRange(index);
                }
                userData.state.Pop(1);
            }
        }

        static void ThrowIndexOutOfRange(int index)
        {
            throw new InvalidOperationException($"User value index {index} is out of range.");
        }
    }

    public UserValues UserValue => new(this);
    public int Size
    {
        get
        {
            state.PushValue(Reference);
            var len = state.RawLen(-1);
            state.Pop(1);
            return len;
        }
    }

    public unsafe bool TryRead<T>([NotNullWhen(true)] out T? result)
    {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            result = default;

            state.PushValue(Reference);
            try
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
            finally
            {
                state.Pop(1);
            }
        }

        if (Size != Unsafe.SizeOf<T>())
        {
            state.Pop(1);
            result = default;
            return false;
        }
        result = UnsafeRead<T>()!;
        return true;
    }

    public T Read<T>()
    {
        if (!TryRead<T>(out var result))
        {
            throw new InvalidOperationException($"Cannot convert user data to {typeof(T).Name}");
        }
        return result;
    }

    public unsafe T UnsafeRead<T>()
    {
        state.PushValue(Reference);
        var ptr = state.ToUserDataPointer(-1);
        var result = Unsafe.Read<T>((void*)ptr);
        state.Pop(1);
        return result;
    }

    public bool TryWrite<T>(in T value)
    {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            return false;
        }

        if (Size != Unsafe.SizeOf<T>())
        {
            return false;
        }
        UnsafeWrite(value);
        return true;
    }

    public void Write<T>(in T value)
    {
        if (!TryWrite(value))
        {
            throw new InvalidOperationException($"Cannot convert {typeof(T).Name} to user data");
        }
    }

    public unsafe void UnsafeWrite<T>(in T value)
    {
        state.PushValue(Reference);
        var ptr = state.ToUserDataPointer(-1);
        Unsafe.Write((void*)ptr, value);
        state.Pop(1);
    }
}
