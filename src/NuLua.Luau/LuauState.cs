using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NuLua.Internal;
using NuLua.Interop.Luau;

namespace NuLua.Luau;

public sealed unsafe partial class LuauState
{
    // Luau does not export LUA_OK / LUA_YIELD / LUA_ERR* / LUA_T* through bindgen,
    // so we mirror the values declared in submodules/luau/VM/include/lua.h here.
    const int LUA_OK = 0;
    const int LUA_YIELD = 1;
    const uint LUA_ERRRUN = 2;
    const uint LUA_ERRSYNTAX = 3;
    const uint LUA_ERRMEM = 4;
    const uint LUA_ERRERR = 5;

    const uint LUA_TNIL = 0;
    const uint LUA_TBOOLEAN = 1;
    const uint LUA_TLIGHTUSERDATA = 2;
    const uint LUA_TNUMBER = 3;
    const uint LUA_TINTEGER = 4;
    const uint LUA_TVECTOR = 5;
    const uint LUA_TSTRING = 6;
    const uint LUA_TTABLE = 7;
    const uint LUA_TFUNCTION = 8;
    const uint LUA_TUSERDATA = 9;
    const uint LUA_TTHREAD = 10;
    const uint LUA_TBUFFER = 11;
    const uint LUA_TCLASS = 12;
    const uint LUA_TOBJECT = 13;

    static LuauState GetMainState(lua_State* L)
    {
        var state = ptrToState[(nint)L];
        while (state.from != null)
        {
            state = state.from;
        }
        return state;
    }

    void CheckResult(int code)
    {
        if (code is not LUA_OK and not LUA_YIELD)
        {
            nuint len;
            var message = NativeMethods.lua_tolstring(ptr, -1, &len);
            var messageStr =
                message == null ? string.Empty : new string((sbyte*)message, 0, (int)len);
            throw new LuaException((uint)code, messageStr);
        }
    }

    public void OpenLibraries()
    {
        CheckDisposed();
        NativeMethods.luaL_openlibs(ptr);
    }

    void OpenSingleLibrary(NativeMethods.lua_pushcclosurek_fn__delegate opener)
    {
        NativeMethods.lua_pushcclosurek(ptr, opener, null, 0, null!);
        NativeMethods.lua_call(ptr, 0, 0);
    }

    public void OpenBaseLibrary()
    {
        CheckDisposed();
        OpenSingleLibrary(NativeMethods.luaopen_base);
    }

    public void OpenCoroutineLibrary()
    {
        CheckDisposed();
        OpenSingleLibrary(NativeMethods.luaopen_coroutine);
    }

    public void OpenTableLibrary()
    {
        CheckDisposed();
        OpenSingleLibrary(NativeMethods.luaopen_table);
    }

    public void OpenStringLibrary()
    {
        CheckDisposed();
        OpenSingleLibrary(NativeMethods.luaopen_string);
    }

    public void OpenMathLibrary()
    {
        CheckDisposed();
        OpenSingleLibrary(NativeMethods.luaopen_math);
    }

    public void OpenOsLibrary()
    {
        CheckDisposed();
        OpenSingleLibrary(NativeMethods.luaopen_os);
    }

    public void OpenDebugLibrary()
    {
        CheckDisposed();
        OpenSingleLibrary(NativeMethods.luaopen_debug);
    }

    public void OpenBit32Library()
    {
        CheckDisposed();
        OpenSingleLibrary(NativeMethods.luaopen_bit32);
    }

    public void OpenBufferLibrary()
    {
        CheckDisposed();
        OpenSingleLibrary(NativeMethods.luaopen_buffer);
    }

    public void OpenUtf8Library()
    {
        CheckDisposed();
        OpenSingleLibrary(NativeMethods.luaopen_utf8);
    }

    public void OpenClassLibrary()
    {
        CheckDisposed();
        OpenSingleLibrary(NativeMethods.luaopen_class);
    }

    public void OpenVectorLibrary()
    {
        CheckDisposed();
        OpenSingleLibrary(NativeMethods.luaopen_vector);
    }

    public void OpenIntegerLibrary()
    {
        CheckDisposed();
        OpenSingleLibrary(NativeMethods.luaopen_integer);
    }

    public void Sandbox()
    {
        CheckDisposed();
        NativeMethods.luaL_sandbox(ptr);
    }

    public void SandboxThread()
    {
        CheckDisposed();
        NativeMethods.luaL_sandboxthread(ptr);
    }

    public void LoadString(ReadOnlySpan<byte> utf8Code, ReadOnlySpan<byte> utf8ChunkName)
    {
        CheckDisposed();
        nuint bytecodeSize;
        byte* bytecode;
        fixed (byte* codePtr = utf8Code)
        {
            bytecode = NativeMethods.luau_compile(
                codePtr,
                (nuint)utf8Code.Length,
                null,
                &bytecodeSize
            );
        }
        if (bytecode == null)
        {
            throw new LuaException(LUA_ERRMEM, "luau_compile returned null.");
        }
        try
        {
            var result = NativeMethods.luau_load(
                ptr,
                (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(utf8ChunkName)),
                bytecode,
                bytecodeSize,
                0
            );
            CheckResult(result);
        }
        finally
        {
            NativeMethods.luau_free(bytecode);
        }
    }

    public void LoadString(ReadOnlySpan<char> code, ReadOnlySpan<char> chunkName)
    {
        using var codeBytes = new NullTerminatedString(code);
        using var chunkNameBytes = new NullTerminatedString(chunkName);
        LoadString(codeBytes.AsSpan(), chunkNameBytes.AsNullTerminatedSpan());
    }

    public void LoadBuffer(ReadOnlySpan<byte> buffer, ReadOnlySpan<byte> utf8ChunkName)
    {
        CheckDisposed();
        fixed (byte* bufferPtr = buffer)
        {
            var result = NativeMethods.luau_load(
                ptr,
                (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(utf8ChunkName)),
                bufferPtr,
                (nuint)buffer.Length,
                0
            );
            CheckResult(result);
        }
    }

    public void LoadBuffer(ReadOnlySpan<byte> buffer, ReadOnlySpan<char> chunkName)
    {
        using var chunkNameBytes = new NullTerminatedString(chunkName);
        LoadBuffer(buffer, chunkNameBytes.AsNullTerminatedSpan());
    }

    public bool TryDump(int index, bool strip, Span<byte> buffer, out int bytesWritten)
    {
        // Luau has no lua_dump equivalent
        _ = index;
        _ = strip;
        _ = buffer;
        bytesWritten = 0;
        throw new NotSupportedException("Dumping functions is not supported on Luau.");
    }

    public int GetAbsIndex(int index)
    {
        CheckDisposed();
        return NativeMethods.lua_absindex(ptr, index);
    }

    public void Copy(int fromIndex, int toIndex)
    {
        CheckDisposed();
        // Luau lacks lua_copy; emulate via push-then-replace.
        var absTo = NativeMethods.lua_absindex(ptr, toIndex);
        NativeMethods.lua_pushvalue(ptr, fromIndex);
        NativeMethods.lua_replace(ptr, absTo);
    }

    public void Rotate(int index, int n)
    {
        CheckDisposed();
        // Luau lacks lua_rotate; fall back to insert/remove patterns matching Lua 5.1.
        if (n == 1)
        {
            NativeMethods.lua_insert(ptr, index);
        }
        else if (n == -1)
        {
            NativeMethods.lua_pushvalue(ptr, index);
            NativeMethods.lua_remove(ptr, index < 0 ? index - 1 : index);
        }
        else
        {
            throw new NotSupportedException("lua_rotate with |n| > 1 is not supported on Luau.");
        }
    }

    public void PushInteger(long value)
    {
        CheckDisposed();
        NativeMethods.lua_pushinteger(ptr, (int)value);
    }

    public void PushLightUserData(nint data)
    {
        CheckDisposed();
        NativeMethods.lua_pushlightuserdatatagged(ptr, (void*)data, 0);
    }

    public void PushValue(LuaReference reference)
    {
        CheckDisposed();
        NativeMethods.lua_rawgeti(ptr, reference.TableIndex, reference.Id);
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
        if (strPtr == null)
        {
            return string.Empty;
        }
        return new string((sbyte*)strPtr, 0, (int)len);
    }

    public void GetGlobal(ReadOnlySpan<char> name)
    {
        CheckDisposed();
        using var nameBytes = new NullTerminatedString(name);
        // Luau does not expose lua_getglobal — it's a macro over lua_getfield+LUA_GLOBALSINDEX.
        NativeMethods.lua_getfield(
            ptr,
            NativeMethods.LUA_GLOBALSINDEX,
            (byte*)
                Unsafe.AsPointer(ref MemoryMarshal.GetReference(nameBytes.AsNullTerminatedSpan()))
        );
    }

    public void SetGlobal(ReadOnlySpan<char> name)
    {
        CheckDisposed();
        using var nameBytes = new NullTerminatedString(name);
        NativeMethods.lua_setfield(
            ptr,
            NativeMethods.LUA_GLOBALSINDEX,
            (byte*)
                Unsafe.AsPointer(ref MemoryMarshal.GetReference(nameBytes.AsNullTerminatedSpan()))
        );
    }

    public void GetTable(int index)
    {
        CheckDisposed();
        NativeMethods.lua_gettable(ptr, index);
    }

    public void NewUserData(int size, int userValueCount)
    {
        CheckDisposed();
        if (userValueCount > 1)
        {
            throw new NotSupportedException("Multiple user values are not supported on Luau.");
        }
        NativeMethods.lua_newuserdatatagged(ptr, (nuint)size, 0);
    }

    public bool TryGetUserValue(int index, int userValueIndex, out LuaValueType type)
    {
        CheckDisposed();
        if (userValueIndex != 1)
        {
            type = default;
            return false;
        }
        NativeMethods.lua_getfenv(ptr, index);
        type = CodeToType((uint)NativeMethods.lua_type(ptr, -1));
        return true;
    }

    public bool TrySetUserValue(int index, int userValueIndex, out LuaValueType type)
    {
        CheckDisposed();
        if (userValueIndex != 1)
        {
            type = default;
            return false;
        }
        type = CodeToType((uint)NativeMethods.lua_type(ptr, -1));
        var result = NativeMethods.lua_setfenv(ptr, index);
        if (result == 0)
        {
            throw new InvalidOperationException(
                "lua_setfenv failed; value at the specified index does not accept an environment."
            );
        }
        return true;
    }

    public void NewFunction(LuaFunc<LuauState> func, int upvalueCount)
    {
        static int Fn(lua_State* L)
        {
            var state = GetOrCreate(L, default);
            var main = GetMainState(L);
            var funcIndex = NativeMethods.lua_tointegerx(
                L,
                NativeMethods.LUA_GLOBALSINDEX - 1,
                null
            );
            var func = main.funcs[funcIndex];

            var numArgs = NativeMethods.lua_gettop(L);
            return func(state, new LuaFuncArguments(state, numArgs));
        }

        CheckDisposed();

        var funcIndex = funcs.Count;
        funcs.Add(func);
        NativeMethods.lua_pushinteger(ptr, funcIndex);
        NativeMethods.lua_pushcclosurek(ptr, Fn, null, 1, null!);
    }

    public void NewFunction(AsyncLuaFunc<LuauState> func, int upvalueCount)
    {
        static int AsyncCFn(lua_State* L)
        {
            var state = GetOrCreate(L, default);
            var main = GetMainState(L);

            if (NativeMethods.lua_isyieldable(L) == 0)
            {
                ReadOnlySpan<byte> errMsg =
                    "attempt to call async function from a non-yieldable context\0"u8;
                return NativeMethods.luau_error(
                    L,
                    (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(errMsg))
                );
            }

            var funcIndex = NativeMethods.lua_tointegerx(
                L,
                NativeMethods.LUA_GLOBALSINDEX - 1,
                null
            );
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
            // Luau does not support continuation-aware yields through lua_pushcclosurek
            // (the cont parameter is only invoked for protected-call continuations).
            // The async driver pushes the awaited results back onto the stack and
            // re-resumes the coroutine; those results become the yield expression's
            // value, which is exactly what we want.
            return NativeMethods.lua_yield(L, 0);
        }

        CheckDisposed();

        var funcIndex = asyncFuncs.Count;
        asyncFuncs.Add(func);
        NativeMethods.lua_pushinteger(ptr, funcIndex);
        NativeMethods.lua_pushcclosurek(ptr, AsyncCFn, null, 1, null!);
    }

    public void Arith(LuaArithmeticOperator op)
    {
        CheckDisposed();
        // Luau does not expose lua_arith.
        throw new NotSupportedException("Arith is not supported on Luau.");
    }

    public void Compare(LuaComparisonOperator op)
    {
        CheckDisposed();
        int result = op switch
        {
            LuaComparisonOperator.Equal => NativeMethods.lua_equal(ptr, -2, -1),
            LuaComparisonOperator.Less => NativeMethods.lua_lessthan(ptr, -2, -1),
            // Luau lacks lua_compare; emulate <= via !(b < a).
            LuaComparisonOperator.LessOrEqual => NativeMethods.lua_lessthan(ptr, -1, -2) == 0
                ? 1
                : 0,
            _ => throw new NotSupportedException($"Unsupported Lua comparison operator: {op}"),
        };

        // Pop the operands and push the result for symmetry with newer versions.
        NativeMethods.lua_settop(ptr, NativeMethods.lua_gettop(ptr) - 2);
        NativeMethods.lua_pushboolean(ptr, result);
    }

    public void Len(int index)
    {
        CheckDisposed();
        // Luau uses lua_objlen, which returns the length without pushing it.
        var len = NativeMethods.lua_objlen(ptr, index);
        NativeMethods.lua_pushinteger(ptr, len);
    }

    public void Call(int argCount, int returnCount)
    {
        CheckDisposed();
        var result = NativeMethods.lua_pcall(ptr, argCount, returnCount, 0);
        CheckResult(result);
    }

    public void Next(int index)
    {
        CheckDisposed();
        var result = NativeMethods.lua_next(ptr, index);
        // lua_next returns 0 when iteration ends and !=0 when a key/value pair was pushed.
        // Treat both as success; only protected-call style routines should error here.
        _ = result;
    }

    public LuaValueType RawGet(int index)
    {
        CheckDisposed();
        NativeMethods.lua_rawget(ptr, index);
        var t = (uint)NativeMethods.lua_type(ptr, -1);
        return CodeToType(t);
    }

    public int RawLen(int index)
    {
        CheckDisposed();
        return NativeMethods.lua_objlen(ptr, index);
    }

    public void Resume(int argCount)
    {
        CheckDisposed();
        var result = NativeMethods.lua_resume(ptr, from == null ? null : from.ptr, argCount);
        CheckResult(result);
    }

    internal int RunResumeStep(int argCount)
    {
        var status = NativeMethods.lua_resume(ptr, from == null ? null : from.ptr, argCount);
        if (status != LUA_OK && status != LUA_YIELD)
        {
            nuint len;
            var message = NativeMethods.lua_tolstring(ptr, -1, &len);
            var messageStr =
                message == null ? string.Empty : new string((sbyte*)message, 0, (int)len);
            throw new LuaException((uint)status, messageStr);
        }
        return status;
    }

    public LuaReference Ref(int index)
    {
        CheckDisposed();
        // Luau's `lua_ref` behaves differently than in standard Lua.
        // ref: https://github.com/luau-lang/luau/issues/247
        var refId = NativeMethods.lua_ref(
            ptr,
            index == NativeMethods.LUA_REGISTRYINDEX ? -1 : index
        );
        if (refId == NativeMethods.LUA_NOREF)
        {
            throw new LuaException(LUA_ERRMEM, "Failed to create reference.");
        }
        if (index == NativeMethods.LUA_REGISTRYINDEX)
        {
            this.Pop(1);
        }
        return new LuaReference(refId, NativeMethods.LUA_REGISTRYINDEX);
    }

    public void Unref(LuaReference reference)
    {
        CheckDisposed();
        NativeMethods.lua_unref(ptr, reference.Id);
    }

    static LuaValueType CodeToType(uint code)
    {
        return code switch
        {
            LUA_TNIL => LuaValueType.Nil,
            LUA_TBOOLEAN => LuaValueType.Boolean,
            LUA_TLIGHTUSERDATA => LuaValueType.LightUserData,
            LUA_TNUMBER => LuaValueType.Number,
            LUA_TINTEGER => LuaValueType.Number,
            LUA_TSTRING => LuaValueType.String,
            LUA_TTABLE => LuaValueType.Table,
            LUA_TFUNCTION => LuaValueType.Function,
            LUA_TUSERDATA => LuaValueType.UserData,
            LUA_TTHREAD => LuaValueType.Thread,
            // Vector / Buffer / Class / Object are Luau-specific value types
            // without a direct ILuaState equivalent — surface them as UserData.
            LUA_TVECTOR => LuaValueType.UserData,
            LUA_TBUFFER => LuaValueType.UserData,
            LUA_TCLASS => LuaValueType.UserData,
            LUA_TOBJECT => LuaValueType.UserData,
            _ => throw new NotSupportedException($"Unsupported Lua type code: {code}"),
        };
    }
}
