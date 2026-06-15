using System.Buffers;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using NuLua.Internal;
using NuLua.Interop.Lua55;

namespace NuLua.Lua55;

public sealed unsafe class Lua55State : ILuaState<Lua55State>
{
    static readonly ConcurrentDictionary<nint, Lua55State> ptrToState = new();

    readonly List<LuaFunc<Lua55State>> funcs = new(8);
    lua_State* ptr;

    Lua55State(lua_State* ptr)
    {
        this.ptr = ptr;
    }

    public static Lua55State Create()
    {
        var ptr = NativeMethods.luaL_newstate();
        if (ptr == null)
        {
            throw new LuaException(NativeMethods.LUA_ERRMEM, "Failed to create Lua state.");
        }

        var state = new Lua55State(ptr);
        ptrToState[(nint)ptr] = state;
        return state;
    }

    static Lua55State GetOrCreate(lua_State* ptr)
    {
        if (ptrToState.TryGetValue((nint)ptr, out var state))
        {
            return state;
        }
        else
        {
            return Create();
        }
    }

    public lua_State* AsPointer() => ptr;

    LuaObjectHandle ILuaObject.Handle => new(this, -1);

    nint ILuaState.AsPointer() => (nint)ptr;

    public void Dispose()
    {
        if (ptr != null)
        {
            NativeMethods.lua_close(ptr);
            ptrToState.TryRemove((nint)ptr, out _);
            ptr = null;
        }
    }

    void CheckDisposed()
    {
        if (ptr == null)
        {
            throw new ObjectDisposedException(nameof(Lua55State));
        }
    }

    void CheckResult(int code)
    {
        if (code != 0)
        {
            nuint len;
            var message = NativeMethods.luaL_tolstring(ptr, -1, &len);
            var messageStr = new string((sbyte*)message, 0, (int)len);
            throw new LuaException((uint)code, messageStr);
        }
    }

    public void OpenLibraries()
    {
        CheckDisposed();
        NativeMethods.luaL_openselectedlibs(ptr, ~0, 0);
    }

    public void OpenBaseLibrary()
    {
        CheckDisposed();
        byte* modname = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference("_G"u8));
        NativeMethods.luaL_requiref(ptr, modname, NativeMethods.luaopen_base, 1);
        NativeMethods.lua_settop(ptr, -2);
    }

    public void OpenTableLibrary()
    {
        CheckDisposed();
        byte* modname = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference("_G"u8));
        NativeMethods.luaL_requiref(ptr, modname, NativeMethods.luaopen_table, 1);
        NativeMethods.lua_settop(ptr, -2);
    }

    public void OpenStringLibrary()
    {
        CheckDisposed();
        byte* modname = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference("_G"u8));
        NativeMethods.luaL_requiref(ptr, modname, NativeMethods.luaopen_string, 1);
        NativeMethods.lua_settop(ptr, -2);
    }

    public void OpenMathLibrary()
    {
        CheckDisposed();
        byte* modname = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference("_G"u8));
        NativeMethods.luaL_requiref(ptr, modname, NativeMethods.luaopen_math, 1);
        NativeMethods.lua_settop(ptr, -2);
    }

    public void OpenCoroutineLibrary()
    {
        CheckDisposed();
        byte* modname = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference("_G"u8));
        NativeMethods.luaL_requiref(ptr, modname, NativeMethods.luaopen_coroutine, 1);
        NativeMethods.lua_settop(ptr, -2);
    }

    public void OpenIoLibrary()
    {
        CheckDisposed();
        byte* modname = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference("_G"u8));
        NativeMethods.luaL_requiref(ptr, modname, NativeMethods.luaopen_io, 1);
        NativeMethods.lua_settop(ptr, -2);
    }

    public void OpenOsLibrary()
    {
        CheckDisposed();
        byte* modname = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference("_G"u8));
        NativeMethods.luaL_requiref(ptr, modname, NativeMethods.luaopen_os, 1);
        NativeMethods.lua_settop(ptr, -2);
    }

    public void OpenPackageLibrary()
    {
        CheckDisposed();
        byte* modname = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference("_G"u8));
        NativeMethods.luaL_requiref(ptr, modname, NativeMethods.luaopen_package, 1);
        NativeMethods.lua_settop(ptr, -2);
    }

    public void OpenDebugLibrary()
    {
        CheckDisposed();
        byte* modname = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference("_G"u8));
        NativeMethods.luaL_requiref(ptr, modname, NativeMethods.luaopen_debug, 1);
        NativeMethods.lua_settop(ptr, -2);
    }

    public void LoadString(ReadOnlySpan<byte> utf8Code, ReadOnlySpan<byte> utf8ChunkName)
    {
        CheckDisposed();
        fixed (byte* codePtr = utf8Code)
        {
            var result = NativeMethods.luaL_loadbufferx(
                ptr,
                codePtr,
                (nuint)utf8Code.Length,
                (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(utf8ChunkName)),
                (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference("t"u8))
            );
            CheckResult(result);
        }
    }

    public void LoadString(ReadOnlySpan<char> code, ReadOnlySpan<char> chunkName)
    {
        using var codeBytes = new NullTerminatedString(code);
        using var chunkNameBytes = new NullTerminatedString(chunkName);
        LoadString(codeBytes.AsSpan(), chunkNameBytes.AsSpan());
    }

    public void LoadBuffer(ReadOnlySpan<byte> buffer, ReadOnlySpan<byte> utf8ChunkName)
    {
        CheckDisposed();
        fixed (byte* bufferPtr = buffer)
        {
            var result = NativeMethods.luaL_loadbufferx(
                ptr,
                bufferPtr,
                (nuint)buffer.Length,
                (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(utf8ChunkName)),
                (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference("b"u8))
            );
            CheckResult(result);
        }
    }

    public void LoadBuffer(ReadOnlySpan<byte> buffer, ReadOnlySpan<char> chunkName)
    {
        using var chunkNameBytes = new NullTerminatedString(chunkName);
        LoadBuffer(buffer, chunkNameBytes.AsSpan());
    }

    public LuaType GetType(int index)
    {
        CheckDisposed();
        var t = (uint)NativeMethods.lua_type(ptr, index);
        return t switch
        {
            NativeMethods.LUA_TBOOLEAN => LuaType.Boolean,
            NativeMethods.LUA_TNUMBER => LuaType.Number,
            NativeMethods.LUA_TSTRING => LuaType.String,
            NativeMethods.LUA_TTABLE => LuaType.Table,
            NativeMethods.LUA_TFUNCTION => LuaType.Function,
            NativeMethods.LUA_TUSERDATA => LuaType.UserData,
            NativeMethods.LUA_TTHREAD => LuaType.Thread,
            NativeMethods.LUA_TLIGHTUSERDATA => LuaType.LightUserData,
            NativeMethods.LUA_TNIL => LuaType.Nil,
            _ => throw new NotSupportedException($"Unsupported Lua type code: {t}"),
        };
    }

    public int GetTop()
    {
        CheckDisposed();
        return NativeMethods.lua_gettop(ptr);
    }

    public void SetTop(int index)
    {
        CheckDisposed();
        NativeMethods.lua_settop(ptr, index);
    }

    public int GetAbsIndex(int index)
    {
        CheckDisposed();
        return NativeMethods.lua_absindex(ptr, index);
    }

    public void Copy(int fromIndex, int toIndex)
    {
        CheckDisposed();
        NativeMethods.lua_copy(ptr, fromIndex, toIndex);
    }

    public void PushNil()
    {
        CheckDisposed();
        NativeMethods.lua_pushnil(ptr);
    }

    public void PushBoolean(bool value)
    {
        CheckDisposed();
        NativeMethods.lua_pushboolean(ptr, value ? 1 : 0);
    }

    public void PushInteger(long value)
    {
        CheckDisposed();
        NativeMethods.lua_pushinteger(ptr, value);
    }

    public void PushNumber(double value)
    {
        CheckDisposed();
        NativeMethods.lua_pushnumber(ptr, value);
    }

    public void PushString(ReadOnlySpan<byte> utf8Str)
    {
        CheckDisposed();
        fixed (byte* strPtr = utf8Str)
        {
            NativeMethods.lua_pushlstring(ptr, strPtr, (nuint)utf8Str.Length);
        }
    }

    public void PushLightUserData(nint data)
    {
        CheckDisposed();
        NativeMethods.lua_pushlightuserdata(ptr, (void*)data);
    }

    public void PushValue(int index)
    {
        CheckDisposed();
        NativeMethods.lua_pushvalue(ptr, index);
    }

    public bool ToBoolean(int index)
    {
        CheckDisposed();
        return NativeMethods.lua_toboolean(ptr, index) != 0;
    }

    public double ToNumber(int index)
    {
        CheckDisposed();
        return NativeMethods.lua_tonumberx(ptr, index, null);
    }

    public string ToString(int index)
    {
        CheckDisposed();
        nuint len;
        var strPtr = NativeMethods.lua_tolstring(ptr, index, &len);
        return new string((sbyte*)strPtr, 0, (int)len);
    }

    public nint ToLightUserData(int index)
    {
        CheckDisposed();
        return (nint)NativeMethods.lua_touserdata(ptr, index);
    }

    public void XMove(Lua55State target, int count)
    {
        CheckDisposed();
        NativeMethods.lua_xmove(ptr, target.ptr, count);
    }

    public LuaTable CreateTable(int initialArraySize = 0, int initialRecordsSize = 0)
    {
        CheckDisposed();
        NativeMethods.lua_createtable(ptr, initialArraySize, initialRecordsSize);
        return new LuaTable(this, GetTop());
    }

    public LuaValue GetGlobal(ReadOnlySpan<char> name)
    {
        CheckDisposed();
        using var nameBytes = new NullTerminatedString(name);
        var result = NativeMethods.lua_getglobal(
            ptr,
            (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(nameBytes.AsSpan()))
        );
        CheckResult(result);
        var value = this.ToLuaValue(-1);
        return value;
    }

    public void SetGlobal(ReadOnlySpan<char> name, LuaValue value)
    {
        CheckDisposed();
        using var nameBytes = new NullTerminatedString(name);
        this.Push(value);
        NativeMethods.lua_setglobal(
            ptr,
            (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(nameBytes.AsSpan()))
        );
    }

    public void GetTable(int index)
    {
        CheckDisposed();
        var result = NativeMethods.lua_gettable(ptr, index);
        CheckResult(result);
    }

    public void SetTable(int index)
    {
        CheckDisposed();
        NativeMethods.lua_settable(ptr, index);
    }

    public LuaFunction CreateFunction(LuaFunc<Lua55State> func)
    {
        static int Fn(lua_State* L)
        {
            var state = GetOrCreate(L);
            var funcIndex = NativeMethods.lua_tointegerx(
                L,
                NativeMethods.LUA_REGISTRYINDEX - 1,
                null
            );
            var func = state.funcs[(int)funcIndex];

            var numArgs = NativeMethods.lua_gettop(L);
            var buffer = ArrayPool<LuaValue>.Shared.Rent(numArgs);
            try
            {
                for (int i = 0; i < numArgs; i++)
                {
                    buffer[i] = state.ToLuaValue(i + 1);
                }
                return func(state, buffer.AsSpan(0, numArgs));
            }
            finally
            {
                ArrayPool<LuaValue>.Shared.Return(buffer);
            }
        }

        CheckDisposed();

        var funcIndex = funcs.Count;
        funcs.Add(func);
        NativeMethods.lua_pushinteger(ptr, funcIndex);
        NativeMethods.lua_pushcclosure(ptr, Fn, 1);
        return new LuaFunction(this, NativeMethods.lua_gettop(ptr));
    }

    public void Arith(LuaArithmeticOperator op)
    {
        CheckDisposed();
        switch (op)
        {
            case LuaArithmeticOperator.Add:
                NativeMethods.lua_arith(ptr, (int)NativeMethods.LUA_OPADD);
                break;
            case LuaArithmeticOperator.Sub:
                NativeMethods.lua_arith(ptr, (int)NativeMethods.LUA_OPSUB);
                break;
            case LuaArithmeticOperator.Mul:
                NativeMethods.lua_arith(ptr, (int)NativeMethods.LUA_OPMUL);
                break;
            case LuaArithmeticOperator.Div:
                NativeMethods.lua_arith(ptr, (int)NativeMethods.LUA_OPDIV);
                break;
            case LuaArithmeticOperator.Mod:
                NativeMethods.lua_arith(ptr, (int)NativeMethods.LUA_OPMOD);
                break;
            case LuaArithmeticOperator.Pow:
                NativeMethods.lua_arith(ptr, (int)NativeMethods.LUA_OPPOW);
                break;
            default:
                throw new NotSupportedException($"Unsupported Lua arithmetic operator: {op}");
        }
    }

    public void Compare(LuaComparisonOperator op)
    {
        CheckDisposed();
        var result = NativeMethods.lua_compare(
            ptr,
            -2,
            -1,
            op switch
            {
                LuaComparisonOperator.Equal => (int)NativeMethods.LUA_OPEQ,
                LuaComparisonOperator.Less => (int)NativeMethods.LUA_OPLT,
                LuaComparisonOperator.LessOrEqual => (int)NativeMethods.LUA_OPLE,
                _ => throw new NotSupportedException($"Unsupported Lua comparison operator: {op}"),
            }
        );

        CheckResult(result);
    }

    public void Concat(int count)
    {
        CheckDisposed();
        NativeMethods.lua_concat(ptr, count);
    }

    public void Len(int index)
    {
        CheckDisposed();
        NativeMethods.lua_len(ptr, index);
    }

    public void Call(int argCount, int returnCount)
    {
        CheckDisposed();
        NativeMethods.lua_callk(ptr, argCount, returnCount, 0, null);
    }
}
