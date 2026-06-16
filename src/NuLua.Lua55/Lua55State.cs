using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NuLua.Internal;
using NuLua.Interop.Lua55;

namespace NuLua.Lua55;

public sealed unsafe partial class Lua55State : ILuaState<Lua55State>
{
    static readonly ConcurrentDictionary<nint, Lua55State> ptrToState = new();

    readonly List<LuaFunc<Lua55State>> funcs = new(8);
    readonly List<AsyncLuaFunc<Lua55State>> asyncFuncs = new(8);
    readonly LuaReference reference;
    readonly Lua55State? from;
    lua_State* ptr;

    ValueTask<int> pendingAsyncTask;
    bool hasPendingTask;
    CancellationToken asyncCancellationToken;

    Lua55State? ILuaState<Lua55State>.From => from;
    ILuaState? ILuaState.From => from;

    Lua55State(lua_State* ptr, Lua55State? from, LuaReference reference)
    {
        this.ptr = ptr;
        this.from = from;
        this.reference = reference;
    }

    public static Lua55State Create()
    {
        var ptr = NativeMethods.luaL_newstate();
        if (ptr == null)
        {
            throw new LuaException(NativeMethods.LUA_ERRMEM, "Failed to create Lua state.");
        }

        var state = new Lua55State(ptr, null, default);
        ptrToState[(nint)ptr] = state;
        return state;
    }

    static Lua55State GetOrCreate(lua_State* ptr, LuaReference reference)
    {
        if (ptrToState.TryGetValue((nint)ptr, out var state))
        {
            return state;
        }
        else
        {
            state = new Lua55State(ptr, null, reference);
            ptrToState[(nint)ptr] = state;
            return state;
        }
    }

    static Lua55State GetMainState(lua_State* L)
    {
        _ = NativeMethods.lua_rawgeti(
            L,
            NativeMethods.LUA_REGISTRYINDEX,
            NativeMethods.LUA_RIDX_MAINTHREAD
        );
        var mainPtr = NativeMethods.lua_tothread(L, -1);
        NativeMethods.lua_settop(L, NativeMethods.lua_gettop(L) - 1);
        return ptrToState[(nint)mainPtr];
    }

    public lua_State* AsPointer() => ptr;

    nint ILuaState.AsPointer() => (nint)ptr;

    public LuaReference Reference => reference;
    public bool IsYieldable
    {
        get
        {
            CheckDisposed();
            return NativeMethods.lua_isyieldable(ptr) != 0;
        }
    }

    public int RegistryIndex => NativeMethods.LUA_REGISTRYINDEX;

    public LuaThreadStatus Status
    {
        get
        {
            CheckDisposed();
            var status = NativeMethods.lua_status(ptr);
            return status switch
            {
                (int)NativeMethods.LUA_OK => LuaThreadStatus.Suspended,
                _ => LuaThreadStatus.Dead,
            };
        }
    }

    public LuaValue this[ReadOnlySpan<char> name]
    {
        get
        {
            GetGlobal(name);
            return this.Pop();
        }
        set
        {
            this.Push(value);
            SetGlobal(name);
        }
    }

    public void Dispose()
    {
        if (ptr != null)
        {
            if (from == null)
            {
                NativeMethods.lua_close(ptr);
            }
            else
            {
                from.Unref(reference);
            }
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
        if (code is not (int)NativeMethods.LUA_OK and not (int)NativeMethods.LUA_YIELD)
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
        byte* modname = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference("table"u8));
        NativeMethods.luaL_requiref(ptr, modname, NativeMethods.luaopen_table, 1);
        NativeMethods.lua_settop(ptr, -2);
    }

    public void OpenStringLibrary()
    {
        CheckDisposed();
        byte* modname = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference("string"u8));
        NativeMethods.luaL_requiref(ptr, modname, NativeMethods.luaopen_string, 1);
        NativeMethods.lua_settop(ptr, -2);
    }

    public void OpenMathLibrary()
    {
        CheckDisposed();
        byte* modname = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference("math"u8));
        NativeMethods.luaL_requiref(ptr, modname, NativeMethods.luaopen_math, 1);
        NativeMethods.lua_settop(ptr, -2);
    }

    public void OpenCoroutineLibrary()
    {
        CheckDisposed();
        byte* modname = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference("coroutine"u8));
        NativeMethods.luaL_requiref(ptr, modname, NativeMethods.luaopen_coroutine, 1);
        NativeMethods.lua_settop(ptr, -2);
    }

    public void OpenIoLibrary()
    {
        CheckDisposed();
        byte* modname = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference("io"u8));
        NativeMethods.luaL_requiref(ptr, modname, NativeMethods.luaopen_io, 1);
        NativeMethods.lua_settop(ptr, -2);
    }

    public void OpenOsLibrary()
    {
        CheckDisposed();
        byte* modname = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference("os"u8));
        NativeMethods.luaL_requiref(ptr, modname, NativeMethods.luaopen_os, 1);
        NativeMethods.lua_settop(ptr, -2);
    }

    public void OpenPackageLibrary()
    {
        CheckDisposed();
        byte* modname = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference("package"u8));
        NativeMethods.luaL_requiref(ptr, modname, NativeMethods.luaopen_package, 1);
        NativeMethods.lua_settop(ptr, -2);
    }

    public void OpenDebugLibrary()
    {
        CheckDisposed();
        byte* modname = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference("debug"u8));
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

    public bool TryDump(int index, Span<byte> buffer, out int bytesWritten)
    {
        static int Writer(lua_State* L, void* p, nuint sz, void* ud)
        {
            var bufferPtr = (byte*)ud;
            if (sz > int.MaxValue || (nuint)bufferPtr + sz < (nuint)bufferPtr)
            {
                // Size is too large or overflowed
                return 1;
            }
            Buffer.MemoryCopy(p, bufferPtr, sz, sz);
            return 0;
        }

        CheckDisposed();
        fixed (byte* bufferPtr = buffer)
        {
            var result = NativeMethods.lua_dump(ptr, Writer, bufferPtr, 0);
            if (result == 0)
            {
                bytesWritten = 0;
                return false;
            }
            else
            {
                bytesWritten = (int)(nuint)bufferPtr;
                return true;
            }
        }
    }

    public LuaValueType GetType(int index)
    {
        CheckDisposed();
        var t = (uint)NativeMethods.lua_type(ptr, index);
        return CodeToType(t);
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

    public bool CheckStack(int n)
    {
        CheckDisposed();
        return NativeMethods.lua_checkstack(ptr, n) != 0;
    }

    public void Copy(int fromIndex, int toIndex)
    {
        CheckDisposed();
        NativeMethods.lua_copy(ptr, fromIndex, toIndex);
    }

    public void Rotate(int index, int n)
    {
        CheckDisposed();
        NativeMethods.lua_rotate(ptr, index, n);
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

    public bool PushThread()
    {
        CheckDisposed();
        return NativeMethods.lua_pushthread(ptr) != 0;
    }

    public void PushValue(int index)
    {
        CheckDisposed();
        NativeMethods.lua_pushvalue(ptr, index);
    }

    public void PushValue(LuaReference reference)
    {
        CheckDisposed();
        _ = NativeMethods.lua_rawgeti(ptr, reference.TableIndex, reference.Id);
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

    public nint ToUserDataPointer(int index)
    {
        CheckDisposed();
        var userData = NativeMethods.lua_touserdata(ptr, index);
        if (userData == null)
        {
            throw new InvalidOperationException("Value at the specified index is not user data.");
        }
        return (nint)userData;
    }

    public Lua55State ToThread(int index)
    {
        CheckDisposed();
        var threadPtr = NativeMethods.lua_tothread(ptr, index);
        if (threadPtr == null)
        {
            throw new InvalidOperationException("Value at the specified index is not a thread.");
        }

        if (ptrToState.TryGetValue((nint)threadPtr, out var threadState))
        {
            return threadState;
        }
        else
        {
            NativeMethods.lua_pushvalue(ptr, index);
            var reference = this.Ref();
            threadState = new Lua55State(threadPtr, this, reference);
            ptrToState[(nint)threadPtr] = threadState;
            return threadState;
        }
    }

    ILuaState ILuaState.ToThread(int index) => ToThread(index);

    public void* ToPointer(int index)
    {
        if (GetType(index) != LuaValueType.LightUserData)
        {
            throw new InvalidOperationException(
                "Value at the specified index is not light user data."
            );
        }
        return NativeMethods.lua_topointer(ptr, index);
    }

    public LuaFunction ToFunction(int index)
    {
        if (GetType(index) != LuaValueType.Function)
        {
            throw new InvalidOperationException("Value at the specified index is not a function.");
        }
        return new LuaFunction(this, this.Ref());
    }

    public void XMove(Lua55State target, int count)
    {
        CheckDisposed();
        NativeMethods.lua_xmove(ptr, target.ptr, count);
    }

    void ILuaState.XMove(ILuaState target, int count)
    {
        XMove((Lua55State)target, count);
    }

    public void NewTable(int initialArraySize = 0, int initialRecordsSize = 0)
    {
        CheckDisposed();
        NativeMethods.lua_createtable(ptr, initialArraySize, initialRecordsSize);
    }

    public void GetGlobal(ReadOnlySpan<char> name)
    {
        CheckDisposed();
        using var nameBytes = new NullTerminatedString(name);
        _ = NativeMethods.lua_getglobal(
            ptr,
            (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(nameBytes.AsSpan()))
        );
    }

    public void SetGlobal(ReadOnlySpan<char> name)
    {
        CheckDisposed();
        using var nameBytes = new NullTerminatedString(name);
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

    public void NewUserData(int size, int userValueCount)
    {
        CheckDisposed();
        NativeMethods.lua_newuserdatauv(ptr, (nuint)size, userValueCount);
    }

    public bool TryGetUserValue(int index, int userValueIndex, out LuaValueType type)
    {
        CheckDisposed();
        var result = NativeMethods.lua_getiuservalue(ptr, index, userValueIndex);
        if (result == NativeMethods.LUA_TNONE)
        {
            type = default;
            return false;
        }
        else
        {
            type = CodeToType((uint)result);
            this.Pop(1);
            return true;
        }
    }

    public bool TrySetUserValue(int index, int userValueIndex, out LuaValueType type)
    {
        CheckDisposed();
        var result = NativeMethods.lua_setiuservalue(ptr, index, userValueIndex);
        if (result == NativeMethods.LUA_TNONE)
        {
            type = default;
            return false;
        }
        else
        {
            type = CodeToType((uint)result);
            return true;
        }
    }

    public void NewFunction(LuaFunc<Lua55State> func, int upvalueCount)
    {
        static int Fn(lua_State* L)
        {
            var state = GetOrCreate(L, default);
            var main = GetMainState(L);
            var funcIndex = NativeMethods.lua_tointegerx(
                L,
                NativeMethods.LUA_REGISTRYINDEX - 1,
                null
            );
            var func = main.funcs[(int)funcIndex];

            var numArgs = NativeMethods.lua_gettop(L);
            return func(state, new LuaFuncArguments(state, numArgs));
        }

        CheckDisposed();

        var funcIndex = funcs.Count;
        funcs.Add(func);
        NativeMethods.lua_pushinteger(ptr, funcIndex);
        NativeMethods.lua_pushcclosure(ptr, Fn, 1);
    }

    public void NewFunction(AsyncLuaFunc<Lua55State> func, int upvalueCount)
    {
        static int AsyncCFn(lua_State* L)
        {
            var state = GetOrCreate(L, default);
            var main = GetMainState(L);

            if (NativeMethods.lua_isyieldable(L) == 0)
            {
                ReadOnlySpan<byte> errMsg =
                    "attempt to call async function from a non-yieldable context"u8;
                return NativeMethods.luaL_error(
                    L,
                    (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(errMsg))
                );
            }

            var funcIndex = (int)
                NativeMethods.lua_tointegerx(L, NativeMethods.LUA_REGISTRYINDEX - 1, null);
            var func = main.asyncFuncs[funcIndex];

            var numArgs = NativeMethods.lua_gettop(L);
            var snapshot = numArgs == 0 ? [] : new LuaValue[numArgs];
            for (int i = 0; i < numArgs; i++)
            {
                snapshot[i] = state.ToLuaValue(i + 1);
            }
            var args = new LuaFuncArguments(snapshot, numArgs);
            var ct = state.asyncCancellationToken;

            var task = func(state, args, ct);

            if (task.IsCompletedSuccessfully)
            {
                return task.Result;
            }

            state.pendingAsyncTask = task;
            state.hasPendingTask = true;
            NativeMethods.lua_settop(L, 0);
            return NativeMethods.lua_yieldk(L, 0, 0, AsyncContinuation);
        }

        static int AsyncContinuation(lua_State* L, int status, nint ctx)
        {
            return NativeMethods.lua_gettop(L);
        }

        CheckDisposed();

        var funcIndex = asyncFuncs.Count;
        asyncFuncs.Add(func);
        NativeMethods.lua_pushinteger(ptr, funcIndex);
        NativeMethods.lua_pushcclosure(ptr, AsyncCFn, 1);
    }

    public void NewThread()
    {
        CheckDisposed();
        var threadPtr = NativeMethods.lua_newthread(ptr);
        if (threadPtr == null)
        {
            throw new LuaException(NativeMethods.LUA_ERRMEM, "Failed to create new thread.");
        }
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
            case LuaArithmeticOperator.Unm:
                NativeMethods.lua_arith(ptr, (int)NativeMethods.LUA_OPUNM);
                break;
            case LuaArithmeticOperator.BNot:
                NativeMethods.lua_arith(ptr, (int)NativeMethods.LUA_OPBNOT);
                break;
            case LuaArithmeticOperator.BAnd:
                NativeMethods.lua_arith(ptr, (int)NativeMethods.LUA_OPBAND);
                break;
            case LuaArithmeticOperator.BOr:
                NativeMethods.lua_arith(ptr, (int)NativeMethods.LUA_OPBOR);
                break;
            case LuaArithmeticOperator.BXor:
                NativeMethods.lua_arith(ptr, (int)NativeMethods.LUA_OPBXOR);
                break;
            case LuaArithmeticOperator.Shl:
                NativeMethods.lua_arith(ptr, (int)NativeMethods.LUA_OPSHL);
                break;
            case LuaArithmeticOperator.Shr:
                NativeMethods.lua_arith(ptr, (int)NativeMethods.LUA_OPSHR);
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
        var result = NativeMethods.lua_pcallk(ptr, argCount, returnCount, 0, 0, null);
        CheckResult(result);
    }

    public void Next(int index)
    {
        CheckDisposed();
        var result = NativeMethods.lua_next(ptr, index);
        CheckResult(result);
    }

    public bool RawEqual(int index1, int index2)
    {
        CheckDisposed();
        return NativeMethods.lua_rawequal(ptr, index1, index2) != 0;
    }

    public LuaValueType RawGet(int index)
    {
        CheckDisposed();
        var t = (uint)NativeMethods.lua_rawget(ptr, index);
        return CodeToType(t);
    }

    public int RawLen(int index)
    {
        CheckDisposed();
        return (int)NativeMethods.lua_rawlen(ptr, index);
    }

    public void RawSet(int index)
    {
        CheckDisposed();
        NativeMethods.lua_rawset(ptr, index);
    }

    public bool TryGetMetatable(int index, [NotNullWhen(true)] out LuaTable? metatable)
    {
        CheckDisposed();
        if (NativeMethods.lua_getmetatable(ptr, index) == 0)
        {
            metatable = default;
            return false;
        }
        else
        {
            metatable = new LuaTable(this, this.Ref());
            return true;
        }
    }

    public void SetMetatable(int index, LuaTable? metatable)
    {
        CheckDisposed();
        if (metatable == null)
        {
            NativeMethods.lua_pushnil(ptr);
            _ = NativeMethods.lua_setmetatable(ptr, index);
        }
        else
        {
            PushValue(metatable.Reference);
            _ = NativeMethods.lua_setmetatable(ptr, index);
        }
    }

    public void Resume(int argCount)
    {
        CheckDisposed();
        int nres;
        var result = NativeMethods.lua_resume(ptr, from == null ? null : from.ptr, argCount, &nres);
        CheckResult(result);
    }

    ValueTask ILuaState.CompleteAsync(int initialArgCount, CancellationToken cancellationToken)
    {
        CheckDisposed();
        return Lua55AsyncDriver.RunAsync(this, initialArgCount, cancellationToken);
    }

    internal void SetAsyncCancellationToken(CancellationToken cancellationToken)
    {
        asyncCancellationToken = cancellationToken;
    }

    internal bool TryTakePendingAsyncTask(out ValueTask<int> task)
    {
        if (hasPendingTask)
        {
            task = pendingAsyncTask;
            hasPendingTask = false;
            pendingAsyncTask = default;
            return true;
        }
        task = default;
        return false;
    }

    internal int RunResumeStep(int argCount)
    {
        int nres;
        var status = NativeMethods.lua_resume(ptr, from == null ? null : from.ptr, argCount, &nres);
        if (status != (int)NativeMethods.LUA_OK && status != (int)NativeMethods.LUA_YIELD)
        {
            nuint len;
            var message = NativeMethods.luaL_tolstring(ptr, -1, &len);
            var messageStr = new string((sbyte*)message, 0, (int)len);
            throw new LuaException((uint)status, messageStr);
        }
        return status;
    }

    public LuaReference Ref(int index)
    {
        CheckDisposed();
        var reference = NativeMethods.luaL_ref(ptr, index);
        if (reference == NativeMethods.LUA_REFNIL)
        {
            throw new LuaException(NativeMethods.LUA_ERRMEM, "Failed to create reference.");
        }
        return new LuaReference(reference, index);
    }

    public void Unref(LuaReference reference)
    {
        CheckDisposed();
        NativeMethods.luaL_unref(ptr, reference.TableIndex, reference.Id);
    }

    static LuaValueType CodeToType(uint code)
    {
        return code switch
        {
            NativeMethods.LUA_TBOOLEAN => LuaValueType.Boolean,
            NativeMethods.LUA_TNUMBER => LuaValueType.Number,
            NativeMethods.LUA_TSTRING => LuaValueType.String,
            NativeMethods.LUA_TTABLE => LuaValueType.Table,
            NativeMethods.LUA_TFUNCTION => LuaValueType.Function,
            NativeMethods.LUA_TUSERDATA => LuaValueType.UserData,
            NativeMethods.LUA_TTHREAD => LuaValueType.Thread,
            NativeMethods.LUA_TLIGHTUSERDATA => LuaValueType.LightUserData,
            NativeMethods.LUA_TNIL => LuaValueType.Nil,
            _ => throw new NotSupportedException($"Unsupported Lua type code: {code}"),
        };
    }
}
