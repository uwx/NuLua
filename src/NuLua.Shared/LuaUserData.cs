using System.Runtime.CompilerServices;

namespace NuLua;

public sealed class LuaUserData(ILuaState state, LuaReference reference) : ILuaObject
{
    readonly ILuaState state = state;

    public readonly ref struct UserValues(LuaUserData userData)
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

    public LuaReference Reference => reference;
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

    public bool TryRead<T>(out T result)
        where T : unmanaged
    {
        if (Size != Unsafe.SizeOf<T>())
        {
            state.Pop(1);
            result = default;
            return false;
        }
        result = UnsafeRead<T>();
        return true;
    }

    public T Read<T>()
        where T : unmanaged
    {
        if (!TryRead<T>(out var result))
        {
            throw new InvalidOperationException($"User data size mismatch");
        }
        return result;
    }

    public unsafe T UnsafeRead<T>()
        where T : unmanaged
    {
        state.PushValue(Reference);
        var ptr = state.ToUserDataPointer(-1);
        var result = Unsafe.Read<T>((void*)ptr);
        state.Pop(1);
        return result;
    }

    public bool TryWrite<T>(in T value)
        where T : unmanaged
    {
        if (Size != Unsafe.SizeOf<T>())
        {
            return false;
        }
        UnsafeWrite(value);
        return true;
    }

    public void Write<T>(in T value)
        where T : unmanaged
    {
        if (!TryWrite(value))
        {
            throw new InvalidOperationException($"User data size mismatch");
        }
    }

    public unsafe void UnsafeWrite<T>(in T value)
        where T : unmanaged
    {
        state.PushValue(Reference);
        var ptr = state.ToUserDataPointer(-1);
        Unsafe.Write((void*)ptr, value);
        state.Pop(1);
    }

    public void Dispose()
    {
        state.Unref(Reference);
    }
}
