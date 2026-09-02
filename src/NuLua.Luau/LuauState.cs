using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks.Sources;
using NuLua.Internal;
using NuLua.Interop.Luau;

namespace NuLua.Luau;

public sealed partial class LuauState
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
    const uint LUA_TPRIMITIVE = 6;
    const uint LUA_TSTRING = 7;
    const uint LUA_TTABLE = 8;
    const uint LUA_TFUNCTION = 9;
    const uint LUA_TUSERDATA = 10;
    const uint LUA_TTHREAD = 11;
    const uint LUA_TBUFFER = 12;
    const uint LUA_TCLASS = 13;
    const uint LUA_TOBJECT = 14;

    // Tracks primitive ids whose metatable has been set in this state. Luau only allows
    // setting a primitive metatable once per id (lua_setprimitivemetatable api_checks that
    // the metatable is not already assigned), so we guard it on the managed side too.
    readonly HashSet<int> primitiveMetatablesSet = new();

    // Registry references awaiting unref, enqueued by LuaObject.Dispose() from any thread
    // (finalizers run on the GC thread). Drained on the owning thread at safe points
    // (Call/LoadString/LoadBuffer) so the native state is never touched from a foreign thread.
    readonly ConcurrentQueue<LuaReference> _pendingUnrefs = new();

    public static unsafe LuauState CreateSandbox()
    {
        var state = Create();
        NativeMethods.luaL_sandbox(state.ptr);
        return state;
    }

    public unsafe void NewSandboxThread()
    {
        NewThread();
        var thread = ToThread(-1);
        NativeMethods.luaL_sandboxthread(thread.ptr);
    }

    public unsafe LuauState CreateSandboxThread()
    {
        NewThread();
        var thread = ToThread(-1);
        NativeMethods.luaL_sandboxthread(thread.ptr);
        return thread;
    }

    static unsafe LuauState GetMainState(lua_State* L)
    {
        var state = ptrToState[(nint)L];
        while (state.from != null)
        {
            state = state.from;
        }
        return state;
    }

    unsafe void CheckResult(int code)
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

    /// <summary>
    /// Raises a Lua runtime error from a C closure. This longjmps to the nearest protected call
    /// (e.g. <c>lua_pcall</c>) and does not return normally; the throw below is defensive.
    /// </summary>
    public unsafe void RaiseError(string message)
    {
        CheckDisposed();
        var msg = Encoding.UTF8.GetBytes(message + "\0");
        fixed (byte* p = msg)
        {
            NativeMethods.luau_error(ptr, p);
        }
        throw new InvalidOperationException("luau_error returned unexpectedly.");
    }

    public unsafe void OpenLibraries()
    {
        CheckDisposed();
        NativeMethods.luaL_openlibs(ptr);
    }

    unsafe void OpenSingleLibrary(delegate* unmanaged[Cdecl]<lua_State*, int> opener)
    {
        NativeMethods.lua_pushcclosurek(ptr, opener, null, 0, null!);
        NativeMethods.lua_call(ptr, 0, 0);
    }

    public unsafe void OpenBaseLibrary()
    {
        CheckDisposed();
        OpenSingleLibrary(&NativeMethods.luaopen_base);
    }

    public unsafe void OpenCoroutineLibrary()
    {
        CheckDisposed();
        OpenSingleLibrary(&NativeMethods.luaopen_coroutine);
    }

    public unsafe void OpenTableLibrary()
    {
        CheckDisposed();
        OpenSingleLibrary(&NativeMethods.luaopen_table);
    }

    public unsafe void OpenStringLibrary()
    {
        CheckDisposed();
        OpenSingleLibrary(&NativeMethods.luaopen_string);
    }

    public unsafe void OpenMathLibrary()
    {
        CheckDisposed();
        OpenSingleLibrary(&NativeMethods.luaopen_math);
    }

    public unsafe void OpenOsLibrary()
    {
        CheckDisposed();
        OpenSingleLibrary(&NativeMethods.luaopen_os);
    }

    public unsafe void OpenDebugLibrary()
    {
        CheckDisposed();
        OpenSingleLibrary(&NativeMethods.luaopen_debug);
    }

    public unsafe void OpenBit32Library()
    {
        CheckDisposed();
        OpenSingleLibrary(&NativeMethods.luaopen_bit32);
    }

    public unsafe void OpenBufferLibrary()
    {
        CheckDisposed();
        OpenSingleLibrary(&NativeMethods.luaopen_buffer);
    }

    public unsafe void OpenUtf8Library()
    {
        CheckDisposed();
        OpenSingleLibrary(&NativeMethods.luaopen_utf8);
    }

    public unsafe void OpenClassLibrary()
    {
        CheckDisposed();
        OpenSingleLibrary(&NativeMethods.luaopen_class);
    }

    public unsafe void OpenVectorLibrary()
    {
        CheckDisposed();
        OpenSingleLibrary(&NativeMethods.luaopen_vector);
    }

    public unsafe void OpenIntegerLibrary()
    {
        CheckDisposed();
        OpenSingleLibrary(&NativeMethods.luaopen_integer);
    }

    public void LoadString(ReadOnlySpan<byte> utf8Code, ReadOnlySpan<byte> utf8ChunkName)
    {
        using var chunkName = new CString(utf8ChunkName);
        LoadStringCore(utf8Code, chunkName);
    }

    public void LoadString(ReadOnlySpan<char> code, ReadOnlySpan<char> chunkName)
    {
        var codeBuffer = ArrayPool<byte>.Shared.Rent(code.Length * 3);
        try
        {
            var codeBytes = Encoding.UTF8.GetBytes(code, codeBuffer);
            using var chunkNameBytes = new CString(chunkName);
            LoadStringCore(codeBuffer.AsSpan(0, codeBytes), chunkNameBytes);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(codeBuffer);
        }
    }

    unsafe void LoadStringCore(ReadOnlySpan<byte> utf8Code, CString chunkName)
    {
        CheckDisposed();
        ProcessPendingUnrefs();
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
            var result = NativeMethods.luau_load(ptr, chunkName.Pointer, bytecode, bytecodeSize, 0);
            CheckResult(result);
        }
        finally
        {
            NativeMethods.luau_free(bytecode);
        }
    }

    public void LoadBuffer(ReadOnlySpan<byte> buffer, ReadOnlySpan<byte> utf8ChunkName)
    {
        using var chunkName = new CString(utf8ChunkName);
        LoadBufferCore(buffer, chunkName);
    }

    public void LoadBuffer(ReadOnlySpan<byte> buffer, ReadOnlySpan<char> chunkName)
    {
        using var chunkNameBytes = new CString(chunkName);
        LoadBuffer(buffer, chunkNameBytes.AsSpan());
    }

    unsafe void LoadBufferCore(ReadOnlySpan<byte> buffer, CString chunkName)
    {
        CheckDisposed();
        ProcessPendingUnrefs();
        fixed (byte* bufferPtr = buffer)
        {
            var result = NativeMethods.luau_load(
                ptr,
                chunkName.Pointer,
                bufferPtr,
                (nuint)buffer.Length,
                0
            );
            CheckResult(result);
        }
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

    public unsafe int GetAbsIndex(int index)
    {
        CheckDisposed();
        return NativeMethods.lua_absindex(ptr, index);
    }

    public unsafe void Copy(int fromIndex, int toIndex)
    {
        CheckDisposed();
        // Luau lacks lua_copy; emulate via push-then-replace.
        var absTo = NativeMethods.lua_absindex(ptr, toIndex);
        NativeMethods.lua_pushvalue(ptr, fromIndex);
        NativeMethods.lua_replace(ptr, absTo);
    }

    public unsafe void Rotate(int index, int n)
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

    public unsafe void PushInteger(long value)
    {
        CheckDisposed();
        NativeMethods.lua_pushinteger(ptr, (int)value);
    }

    public unsafe void PushLightUserData(nint data)
    {
        CheckDisposed();
        NativeMethods.lua_pushlightuserdatatagged(ptr, (void*)data, 0);
    }

    public unsafe void PushVector(Vector3 value)
    {
        CheckDisposed();
        NativeMethods.lua_pushvector(ptr, value.X, value.Y, value.Z);
    }

    public Vector3 ToVector(int index)
    {
        var span = ToVectorSpan(index);
        return new Vector3(span[0], span[1], span[2]);
    }

    public unsafe Span<float> ToVectorSpan(int index)
    {
        CheckDisposed();
        var p = NativeMethods.lua_tovector(ptr, index);
        if (p == null)
        {
            throw new InvalidOperationException("Value at the specified index is not a vector.");
        }
        return new Span<float>(p, 3);
    }

    public void PushPrimitive<T>(T data) where T : unmanaged, IPrimitive
    {
        PushPrimitive(T.PrimitiveId, data);
    }

    public unsafe void PushPrimitive<T>(int id, T data) where T : unmanaged
    {
        if (sizeof(T) > NativeMethods.LUA_PRIMITIVE_SIZE)
        {
            throw new InvalidOperationException("Maximum primitive length is 24 bytes.");
        }

        CheckDisposed();
        NativeMethods.lua_pushprimitive(ptr, id, &data, (nuint)sizeof(T));
    }

    public unsafe void PushPrimitive(int id, Span<byte> data)
    {
        if (data.Length > NativeMethods.LUA_PRIMITIVE_SIZE)
        {
            throw new InvalidOperationException("Maximum primitive length is 24 bytes.");
        }

        CheckDisposed();
        fixed (byte* dataPtr = data)
            NativeMethods.lua_pushprimitive(ptr, id, dataPtr, (nuint)data.Length);
    }

    public unsafe Span<byte> ToPrimitive(int index, out int id)
    {
        CheckDisposed();
        if (GetType(index) != LuaValueType.Primitive)
        {
            id = default;
            throw new InvalidOperationException("Value at the specified index is not a primitive.");
        }
        fixed (int* idPtr = &id)
        {
            var p = NativeMethods.lua_toprimitive(ptr, index, idPtr);
            if (p == null)
            {
                throw new InvalidOperationException("Value at the specified index is not a primitive.");
            }
            return new Span<byte>(p, (int)NativeMethods.LUA_PRIMITIVE_SIZE);
        }
    }

    public unsafe bool GetPrimitiveMetatable(int id, [NotNullWhen(true)] out LuaTable? metatable)
    {
        CheckDisposed();
        if (id < 0 || (uint)id >= NativeMethods.LUA_PRIMITIVE_LIMIT)
        {
            metatable = default;
            throw new ArgumentOutOfRangeException(nameof(id));
        }
        NativeMethods.lua_getprimitivemetatable(ptr, id);
        if (GetType(-1) != LuaValueType.Table)
        {
            Pop(1);
            metatable = default;
            return false;
        }
        metatable = new LuaTable(this, Ref());
        return true;
    }

    public void SetPrimitiveMetatable<T>(LuaTable metatable) where T : unmanaged, IPrimitive
    {
        SetPrimitiveMetatable(T.PrimitiveId, metatable);
    }

    public unsafe void SetPrimitiveMetatable(int id, LuaTable metatable)
    {
        CheckDisposed();
        if (id < 0 || (uint)id >= NativeMethods.LUA_PRIMITIVE_LIMIT)
        {
            throw new ArgumentOutOfRangeException(nameof(id));
        }
        if (!primitiveMetatablesSet.Add(id))
        {
            throw new InvalidOperationException(
                $"A metatable for primitive id {id} has already been set; primitive metatables can only be set once."
            );
        }
        PushValue(metatable.Reference);
        NativeMethods.lua_setprimitivemetatable(ptr, id);
    }

    public unsafe LuauBuffer NewBuffer(int size)
    {
        CheckDisposed();
        _ = NativeMethods.lua_newbuffer(ptr, (nuint)size);
        return new LuauBuffer(this, Ref());
    }

    public unsafe LuauBuffer ToBuffer(int index)
    {
        CheckDisposed();
        if (GetType(index) != LuaValueType.Buffer)
        {
            throw new InvalidOperationException("Value at the specified index is not a buffer.");
        }
        NativeMethods.lua_pushvalue(ptr, index);
        return new LuauBuffer(this, Ref());
    }

    public unsafe Span<byte> ToBufferSpan(int index)
    {
        CheckDisposed();
        nuint len;
        var p = NativeMethods.lua_tobuffer(ptr, index, &len);
        if (p == null)
        {
            throw new InvalidOperationException("Value at the specified index is not a buffer.");
        }
        return new Span<byte>((byte*)p, (int)len);
    }

    public unsafe LuauClass ToClass(int index)
    {
        CheckDisposed();
        if (GetType(index) != LuaValueType.Class)
        {
            throw new InvalidOperationException("Value at the specified index is not a class.");
        }
        NativeMethods.lua_pushvalue(ptr, index);
        return new LuauClass(this, Ref());
    }

    public unsafe LuauObject ToObject(int index)
    {
        CheckDisposed();
        if (GetType(index) != LuaValueType.Object)
        {
            throw new InvalidOperationException("Value at the specified index is not an object.");
        }
        NativeMethods.lua_pushvalue(ptr, index);
        return new LuauObject(this, Ref());
    }

    public LuaValue ToLuaValue(int index)
    {
        CheckDisposed();
        var type = GetType(index);
        switch (type)
        {
            case LuaValueType.Nil:
                return LuaValue.Nil;
            case LuaValueType.Boolean:
                return ToBoolean(index);
            case LuaValueType.Number:
                return ToNumber(index);
            case LuaValueType.String:
                return ToString(index);
            case LuaValueType.Vector:
                return LuaValue.FromVector(ToVector(index));
            case LuaValueType.Primitive:
            {
                var payload = ToPrimitive(index, out var primitiveId);
                return LuaValue.FromPrimitive(primitiveId, payload);
            }
            case LuaValueType.Buffer:
                return LuaValue.FromBuffer(ToBuffer(index));
            case LuaValueType.Class:
                return LuaValue.FromClass(ToClass(index));
            case LuaValueType.Object:
                return LuaValue.FromObject(ToObject(index));
            case LuaValueType.Table:
            {
                PushValue(index);
                var reference = Ref();
                return new LuaTable(this, reference);
            }
            case LuaValueType.Function:
            {
                PushValue(index);
                var reference = Ref();
                return new LuaFunction(this, reference);
            }
            case LuaValueType.Thread:
            {
                // ToThread() already keeps the coroutine alive via its own
                // registry reference, so no extra Ref() is taken here (and none
                // would ever be released).
                var thread = ToThread(index);
                return LuaValue.FromThread(thread);
            }
            case LuaValueType.UserData:
            {
                PushValue(index);
                var reference = Ref();
                return new LuaUserData(this, reference);
            }
            default:
                throw new NotSupportedException($"Unsupported Lua value type: {type}");
        }
    }

    public void Push(LuaValue value)
    {
        CheckDisposed();
        switch (value.Type)
        {
            case LuaValueType.Vector:
                PushVector(value.UnsafeRead<Vector3>());
                return;
            case LuaValueType.Primitive:
                var primitiveValue = value.UnsafeRead<PrimitiveValue>();
                PushPrimitive(primitiveValue.Id, primitiveValue.Primitive);
                return;
            case LuaValueType.Buffer:
            case LuaValueType.Class:
            case LuaValueType.Object:
                PushValue(value.UnsafeRead<ILuaObject>().Reference);
                return;
            case LuaValueType.Nil:
                PushNil();
                break;
            case LuaValueType.Boolean:
                PushBoolean(value.UnsafeRead<bool>());
                break;
            case LuaValueType.Number:
                PushNumber(value.UnsafeRead<double>());
                break;
            case LuaValueType.String:
                PushString(value.UnsafeRead<string>());
                break;
            case LuaValueType.Table:
            case LuaValueType.Function:
            case LuaValueType.UserData:
            case LuaValueType.Thread:
            {
                var obj = value.UnsafeRead<ILuaObject>();
                PushValue(obj.Reference);
                break;
            }
            default:
                throw new NotSupportedException($"Unsupported Lua value type: {value.Type}");
        }
    }

    public unsafe void PushValue(LuaReference reference)
    {
        CheckDisposed();
        NativeMethods.lua_rawgeti(ptr, reference.TableIndex, reference.Id);
    }

    public unsafe double ToNumber(int index)
    {
        CheckDisposed();
        return NativeMethods.lua_tonumberx(ptr, index, null);
    }

    public unsafe long ToInteger(int index)
    {
        CheckDisposed();
        return NativeMethods.lua_tointegerx(ptr, index, null);
    }

    public unsafe string ToString(int index)
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

    public unsafe void GetGlobal(ReadOnlySpan<char> name)
    {
        CheckDisposed();
        using var nameBytes = new CString(name);
        CheckResult(NativeMethods.nulua_pgetglobal(ptr, nameBytes.Pointer));
    }

    public unsafe void SetGlobal(ReadOnlySpan<char> name)
    {
        CheckDisposed();
        using var nameBytes = new CString(name);
        CheckResult(NativeMethods.nulua_psetglobal(ptr, nameBytes.Pointer));
    }

    public unsafe void GetTable(int index)
    {
        CheckDisposed();
        CheckResult(NativeMethods.nulua_pgettable(ptr, index));
    }

    public unsafe void GetField(int index, ReadOnlySpan<char> name)
    {
        CheckDisposed();
        using var nameBytes = new CString(name);
        CheckResult(NativeMethods.nulua_pgetfield(ptr, index, nameBytes.Pointer));
    }

    public unsafe void SetField(int index, ReadOnlySpan<char> name)
    {
        CheckDisposed();
        using var nameBytes = new CString(name);
        CheckResult(NativeMethods.nulua_psetfield(ptr, index, nameBytes.Pointer));
    }

    public unsafe void GetI(int index, long n)
    {
        CheckDisposed();
        var absIndex = NativeMethods.lua_absindex(ptr, index);
        NativeMethods.lua_pushinteger(ptr, (int)n);
        CheckResult(NativeMethods.nulua_pgettable(ptr, absIndex));
    }

    public unsafe void SetI(int index, long n)
    {
        CheckDisposed();
        var absIndex = NativeMethods.lua_absindex(ptr, index);
        NativeMethods.lua_pushinteger(ptr, (int)n);
        NativeMethods.lua_insert(ptr, -2);
        CheckResult(NativeMethods.nulua_psettable(ptr, absIndex));
    }

    public unsafe void NewUserData(int size, int userValueCount)
    {
        CheckDisposed();
        if (userValueCount > 1)
        {
            throw new NotSupportedException("Multiple user values are not supported on Luau.");
        }
        NativeMethods.lua_newuserdatatagged(ptr, (nuint)size, 0);
    }

    public unsafe bool TryGetUserValue(int index, int userValueIndex, out LuaValueType type)
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

    public unsafe bool TrySetUserValue(int index, int userValueIndex, out LuaValueType type)
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

    public unsafe void NewFunction(LuaFunc func, int upvalueCount)
    {
        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
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

            int result;
            Exception? error = null;
            try
            {
                result = func(state, new LuaFuncArguments(state, numArgs));
            }
            catch (Exception ex)
            {
                // Defer the raise until after the catch completes: luau_error longjmps via an
                // SEH exception, and throwing one from inside a catch of an
                // [UnmanagedCallersOnly] method re-enters the catch on .NET 10
                // (dotnet/runtime#123579, fixed in 10.0 servicing / #131467) -> infinite loop
                // -> stack overflow.
                error = ex;
                result = 0;
            }

            if (error is not null)
            {
                state.RaiseError(error.Message + "\n" + error.StackTrace);
            }

            return result;
        }

        CheckDisposed();

        var top = NativeMethods.lua_gettop(ptr);
        if ((uint)upvalueCount > (uint)top || upvalueCount >= byte.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(upvalueCount));
        }

        var funcIndex = funcs.Count;
        funcs.Add(func);
        NativeMethods.lua_pushinteger(ptr, funcIndex);
        NativeMethods.lua_insert(ptr, -upvalueCount - 1);
        NativeMethods.lua_pushcclosurek(ptr, &Fn, null, upvalueCount + 1, null!);
    }

    public unsafe void NewFunction(AsyncLuaFunc func, int upvalueCount)
    {
        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
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
            var args = new LuaFuncArguments(state, numArgs);
            var ct = state.asyncCancellationToken;

            ValueTask<int> task;
            Exception? error = null;
            try
            {
                task = func(state, args, ct);
            }
            catch (Exception ex)
            {
                // Defer the raise until after the catch completes; see the synchronous
                // NewFunction bridge (dotnet/runtime#123579 workaround).
                error = ex;
                task = default;
            }

            if (error is not null)
            {
                state.RaiseError(error.Message + "\n" + error.StackTrace);
                return 0;
            }

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

        var top = NativeMethods.lua_gettop(ptr);
        if ((uint)upvalueCount > (uint)top || upvalueCount >= byte.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(upvalueCount));
        }

        var funcIndex = asyncFuncs.Count;
        asyncFuncs.Add(func);
        NativeMethods.lua_pushinteger(ptr, funcIndex);
        NativeMethods.lua_insert(ptr, -upvalueCount - 1);
        NativeMethods.lua_pushcclosurek(ptr, &AsyncCFn, null, upvalueCount + 1, null!);
    }

    static readonly byte[]?[] arithBytecodeCache = new byte[(int)LuaArithmeticOperator.Shr + 1][];

    public unsafe void Arith(LuaArithmeticOperator op)
    {
        CheckDisposed();
        if (TryArithBit32(op))
        {
            return;
        }

        // Luau does not expose lua_arith; emulate by calling a per-operator
        // Lua snippet so that metamethods participate. The Luau-compiled
        // bytecode is cached after the first call.
        ReadOnlySpan<byte> source;
        int argCount;
        switch (op)
        {
            case LuaArithmeticOperator.Add:
                source = "local a,b=...;return a+b"u8;
                argCount = 2;
                break;
            case LuaArithmeticOperator.Sub:
                source = "local a,b=...;return a-b"u8;
                argCount = 2;
                break;
            case LuaArithmeticOperator.Mul:
                source = "local a,b=...;return a*b"u8;
                argCount = 2;
                break;
            case LuaArithmeticOperator.Div:
                source = "local a,b=...;return a/b"u8;
                argCount = 2;
                break;
            case LuaArithmeticOperator.IDiv:
                source = "local a,b=...;return a//b"u8;
                argCount = 2;
                break;
            case LuaArithmeticOperator.Mod:
                source = "local a,b=...;return a%b"u8;
                argCount = 2;
                break;
            case LuaArithmeticOperator.Pow:
                source = "local a,b=...;return a^b"u8;
                argCount = 2;
                break;
            case LuaArithmeticOperator.Unm:
                source = "local a=...;return -a"u8;
                argCount = 1;
                break;
            default:
                throw new NotSupportedException($"Arith operator '{op}' is not supported on Luau.");
        }

        var bytecode = arithBytecodeCache[(int)op];
        if (bytecode == null)
        {
            nuint bytecodeSize;
            byte* bytecodePtr;
            fixed (byte* codePtr = source)
            {
                bytecodePtr = NativeMethods.luau_compile(
                    codePtr,
                    (nuint)source.Length,
                    null,
                    &bytecodeSize
                );
            }
            if (bytecodePtr == null)
            {
                throw new LuaException(LUA_ERRMEM, "luau_compile returned null.");
            }
            try
            {
                bytecode = new ReadOnlySpan<byte>(bytecodePtr, (int)bytecodeSize).ToArray();
            }
            finally
            {
                NativeMethods.luau_free(bytecodePtr);
            }
            arithBytecodeCache[(int)op] = bytecode;
        }

        using var chunkName = new CString("arith"u8);
        fixed (byte* bcPtr = bytecode)
        {
            var loadResult = NativeMethods.luau_load(
                ptr,
                chunkName.Pointer,
                bcPtr,
                (nuint)bytecode.Length,
                0
            );
            CheckResult(loadResult);
        }

        // The chunk is on top; insert it below the operands so the call
        // pattern becomes [..., func, a, (b)].
        NativeMethods.lua_insert(ptr, -1 - argCount);

        var callResult = NativeMethods.lua_pcall(ptr, argCount, 1, 0);
        CheckResult(callResult);
    }

    unsafe bool TryArithBit32(LuaArithmeticOperator op)
    {
        static int NormalizeShiftCount(long value) => (int)(value & 31);

        switch (op)
        {
            case LuaArithmeticOperator.BNot:
            {
                var a = (uint)ToInteger(-1);
                NativeMethods.lua_settop(ptr, NativeMethods.lua_gettop(ptr) - 1);
                PushInteger((long)(uint)~a);
                return true;
            }
            case LuaArithmeticOperator.BAnd:
            {
                var b = (uint)ToInteger(-1);
                var a = (uint)ToInteger(-2);
                NativeMethods.lua_settop(ptr, NativeMethods.lua_gettop(ptr) - 2);
                PushInteger((long)(a & b));
                return true;
            }
            case LuaArithmeticOperator.BOr:
            {
                var b = (uint)ToInteger(-1);
                var a = (uint)ToInteger(-2);
                NativeMethods.lua_settop(ptr, NativeMethods.lua_gettop(ptr) - 2);
                PushInteger((long)(a | b));
                return true;
            }
            case LuaArithmeticOperator.BXor:
            {
                var b = (uint)ToInteger(-1);
                var a = (uint)ToInteger(-2);
                NativeMethods.lua_settop(ptr, NativeMethods.lua_gettop(ptr) - 2);
                PushInteger((long)(a ^ b));
                return true;
            }
            case LuaArithmeticOperator.Shl:
            {
                var b = NormalizeShiftCount(ToInteger(-1));
                var a = (uint)ToInteger(-2);
                NativeMethods.lua_settop(ptr, NativeMethods.lua_gettop(ptr) - 2);
                PushInteger((long)(a << b));
                return true;
            }
            case LuaArithmeticOperator.Shr:
            {
                var b = NormalizeShiftCount(ToInteger(-1));
                var a = (uint)ToInteger(-2);
                NativeMethods.lua_settop(ptr, NativeMethods.lua_gettop(ptr) - 2);
                PushInteger((long)(a >> b));
                return true;
            }
            default:
                return false;
        }
    }

    public unsafe void Compare(LuaComparisonOperator op)
    {
        CheckDisposed();
        var nativeOp = op switch
        {
            LuaComparisonOperator.Equal => 0,
            LuaComparisonOperator.Less => 1,
            LuaComparisonOperator.LessOrEqual => 2,
            _ => throw new NotSupportedException($"Unsupported Lua comparison operator: {op}"),
        };
        CheckResult(NativeMethods.nulua_pcompare(ptr, nativeOp));
    }

    public unsafe void Len(int index)
    {
        CheckDisposed();
        CheckResult(NativeMethods.nulua_plen(ptr, index));
    }

    public unsafe void Call(int argCount, int returnCount)
    {
        CheckDisposed();
        ProcessPendingUnrefs();
        var result = NativeMethods.lua_pcall(ptr, argCount, returnCount, 0);
        CheckResult(result);
    }

    public unsafe void Next(int index)
    {
        CheckDisposed();
        int hasNext;
        CheckResult(NativeMethods.nulua_pnext(ptr, index, &hasNext));
    }

    public unsafe LuaValueType RawGet(int index)
    {
        CheckDisposed();
        NativeMethods.lua_rawget(ptr, index);
        var t = (uint)NativeMethods.lua_type(ptr, -1);
        return CodeToType(t);
    }

    public unsafe int RawLen(int index)
    {
        CheckDisposed();
        return NativeMethods.lua_objlen(ptr, index);
    }

    public unsafe void Resume(int argCount)
    {
        CheckDisposed();
        var result = NativeMethods.lua_resume(ptr, from == null ? null : from.ptr, argCount);
        CheckResult(result);
    }

    internal unsafe int RunResumeStep(int argCount)
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

    public unsafe LuaReference Ref(int index)
    {
        CheckDisposed();
        // Reclaim references deferred by finalizers (LuaObject.Dispose → EnqueueUnref) so the
        // registry stays bounded even inside a single long Call that creates many userdata refs.
        // This runs on the owning thread (Ref is only reached via marshalling on that thread).
        ProcessPendingUnrefs();
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
            Pop(1);
        }
        return new LuaReference(refId, NativeMethods.LUA_REGISTRYINDEX);
    }

    public unsafe void Unref(LuaReference reference)
    {
        CheckDisposed();
        NativeMethods.lua_unref(ptr, reference.Id);
    }

    /// <summary>
    /// Thread-safe: queues <paramref name="reference"/> for unref on the owning thread. Safe to
    /// call from a finalizer (GC thread). No-op once the state is disposed.
    /// </summary>
    public void EnqueueUnref(LuaReference reference)
    {
        if (IsDisposed)
        {
            return;
        }
        _pendingUnrefs.Enqueue(reference);
    }

    /// <summary>
    /// Releases any deferred references. MUST be called on the owning thread (the one running the
    /// VM); done automatically at the start of Call/LoadString/LoadBuffer. Call explicitly after a
    /// batch of work to reclaim the registry before the next script execution.
    /// </summary>
    public unsafe void ProcessPendingUnrefs()
    {
        if (IsDisposed)
        {
            return;
        }
        while (_pendingUnrefs.TryDequeue(out var reference))
        {
            NativeMethods.lua_unref(ptr, reference.Id);
        }
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
            LUA_TPRIMITIVE => LuaValueType.Primitive,
            LUA_TSTRING => LuaValueType.String,
            LUA_TTABLE => LuaValueType.Table,
            LUA_TFUNCTION => LuaValueType.Function,
            LUA_TUSERDATA => LuaValueType.UserData,
            LUA_TTHREAD => LuaValueType.Thread,
            LUA_TVECTOR => LuaValueType.Vector,
            LUA_TBUFFER => LuaValueType.Buffer,
            LUA_TCLASS => LuaValueType.Class,
            LUA_TOBJECT => LuaValueType.Object,
            _ => throw new NotSupportedException($"Unsupported Lua type code: {code}"),
        };
    }

    public int Return()
    {
        return 0;
    }

    public int Return(params ReadOnlySpan<LuaValue> values)
    {
        foreach (var value in values)
        {
            Push(value);
        }

        return values.Length;
    }

    #region LuaModuleExtensions

    
    const string FallbackCacheKey = "_NULUA_MODULES";

    public void UseModuleLoader(LuaModuleLoader loader)
    {
        if (TryRegisterSearcher(loader))
        {
            return;
        }

        UseRequireReplacement(loader);
    }

    bool TryRegisterSearcher(LuaModuleLoader loader)
    {
        var baseTop = GetTop();
        GetGlobal("package");
        if (GetType(-1) != LuaValueType.Table)
        {
            SetTop(baseTop);
            return false;
        }

        GetField(-1, "searchers");
        if (GetType(-1) != LuaValueType.Table)
        {
            SetTop(GetTop() - 1);
            GetField(-1, "loaders");
            if (GetType(-1) != LuaValueType.Table)
            {
                SetTop(baseTop);
                return false;
            }
        }

        PushValue(-1);
        var searchersRef = Ref();
        SetTop(baseTop);

        var searchers = new LuaTable(this, searchersRef);

        try
        {
            using var searcher = CreateFunction(
                (lua, args) =>
                {
                    var name = args[0].Read<string>();
                    if (!loader.TryLoad(lua, name))
                    {
                        lua.PushString($"\n\tno module '{name}' from custom loader");
                        return 1;
                    }

                    var moduleValue = lua.ToLuaValue(-1);
                    lua.NewFunction(
                        (inner, _) =>
                        {
                            inner.Push(moduleValue);
                            // The module is now on the stack (and will be cached in
                            // package.loaded by require), so the registry reference
                            // taken by ToLuaValue is no longer needed.
                            moduleValue.Dispose();
                            return 1;
                        },
                        0
                    );
                    return 1;
                }
            );

            searchers[searchers.Length + 1] = LuaValue.FromFunction(searcher);
        }
        finally
        {
            searchers.Dispose();
        }

        return true;
    }

    void UseRequireReplacement(LuaModuleLoader loader)
    {
        var requireFn = CreateFunction(
            (lua, args) =>
            {
                var name = args[0].Read<string>();

                var cacheValue = lua[FallbackCacheKey];
                LuaTable cacheTable;
                if (cacheValue.IsNil)
                {
                    cacheTable = lua.CreateTable();
                    lua[FallbackCacheKey] = cacheTable;
                }
                else
                {
                    cacheTable = cacheValue.Read<LuaTable>();
                }

                try
                {
                    var cacheKey = loader.ResolveCacheKey(name);
                    var cached = cacheTable[cacheKey];
                    if (!cached.IsNil)
                    {
                        lua.Push(cached);
                        // The module is on the stack (and cached in the table), so
                        // release the temporary registry reference from the getter.
                        cached.Dispose();
                        return 1;
                    }

                    if (!loader.TryLoad(lua, name))
                    {
                        throw new LuaException(2, $"module '{name}' not found");
                    }

                    var moduleValue = lua.ToLuaValue(-1);
                    cacheTable[cacheKey] = moduleValue;
                    lua.Push(moduleValue);
                    // The module is now both cached in `cacheTable` and on the
                    // stack, so the temporary registry reference can be released.
                    moduleValue.Dispose();
                    return 1;
                }
                finally
                {
                    // `cacheValue` is a per-call temporary wrapper around the
                    // persistent `_NULUA_MODULES` table (which stays alive as a
                    // global); release its registry reference. No-op when the
                    // cache table was freshly created (cacheValue is nil).
                    cacheValue.Dispose();
                }
            }
        );

        this["require"] = requireFn;
    }

    #endregion

    #region LuaStateExtensions

    public void PushString(ReadOnlySpan<char> str)
    {
        using var strBytes = new CString(str);
        PushString(strBytes.AsSpan());
    }

    public LuaValue Pop()
    {
        var top = GetTop();

        if (top == 0)
        {
            throw new InvalidOperationException("Stack is empty");
        }

        var value = ToLuaValue(-1);
        SetTop(top - 1);
        return value;
    }

    public void Pop(int count)
    {
        var top = GetTop();
        if ((uint)count > (uint)top)
        {
            throw new ArgumentOutOfRangeException(
                nameof(count),
                count,
                $"Pop count must be between 0 and the current stack size ({top})."
            );
        }

        SetTop(top - count);
    }

    public void Insert(int index)
    {
        Rotate(index, 1);
    }

    public void Remove(int index)
    {
        Rotate(index, -1);
        SetTop(GetTop() - 1);
    }

    public void Replace(int index)
    {
        Copy(-1, index);
        SetTop(GetTop() - 1);
    }

    public int GetUpvalueIndex(int index)
    {
        if (index <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return UpvalueIndexBase - index;
    }

    public LuaReference ToReference(int index)
    {
        PushValue(index);
        var reference = Ref();
        Pop(1);
        return reference;
    }

    public LuaTable ToTable(int index)
    {
        if (GetType(index) != LuaValueType.Table)
        {
            throw new InvalidOperationException("Value at the specified index is not a table.");
        }
        return new LuaTable(this, ToReference(index));
    }

    public LuaUserData ToUserData(int index)
    {
        if (GetType(index) != LuaValueType.UserData)
        {
            throw new InvalidOperationException("Value at the specified index is not user data.");
        }
        return new LuaUserData(this, ToReference(index));
    }

    public LuaReference Ref()
    {
        return Ref(RegistryIndex);
    }

    public LuaValue Add(LuaValue a, LuaValue b)
    {
        Push(a);
        Push(b);
        Arith(LuaArithmeticOperator.Add);
        return Pop();
    }

    public LuaValue Sub(LuaValue a, LuaValue b)
    {
        Push(a);
        Push(b);
        Arith(LuaArithmeticOperator.Sub);
        return Pop();
    }

    public LuaValue Mul(LuaValue a, LuaValue b)
    {
        Push(a);
        Push(b);
        Arith(LuaArithmeticOperator.Mul);
        return Pop();
    }

    public LuaValue Div(LuaValue a, LuaValue b)
    {
        Push(a);
        Push(b);
        Arith(LuaArithmeticOperator.Div);
        return Pop();
    }

    public LuaValue IDiv(LuaValue a, LuaValue b)
    {
        Push(a);
        Push(b);
        Arith(LuaArithmeticOperator.IDiv);
        return Pop();
    }

    public LuaValue Mod(LuaValue a, LuaValue b)
    {
        Push(a);
        Push(b);
        Arith(LuaArithmeticOperator.Mod);
        return Pop();
    }

    public LuaValue Pow(LuaValue a, LuaValue b)
    {
        Push(a);
        Push(b);
        Arith(LuaArithmeticOperator.Pow);
        return Pop();
    }

    public LuaValue Unm(LuaValue a)
    {
        Push(a);
        Arith(LuaArithmeticOperator.Unm);
        return Pop();
    }

    public LuaValue BNot(LuaValue a)
    {
        Push(a);
        Arith(LuaArithmeticOperator.BNot);
        return Pop();
    }

    public LuaValue BAnd(LuaValue a, LuaValue b)
    {
        Push(a);
        Push(b);
        Arith(LuaArithmeticOperator.BAnd);
        return Pop();
    }

    public LuaValue BOr(LuaValue a, LuaValue b)
    {
        Push(a);
        Push(b);
        Arith(LuaArithmeticOperator.BOr);
        return Pop();
    }

    public LuaValue BXor(LuaValue a, LuaValue b)
    {
        Push(a);
        Push(b);
        Arith(LuaArithmeticOperator.BXor);
        return Pop();
    }

    public LuaValue Shl(LuaValue a, LuaValue b)
    {
        Push(a);
        Push(b);
        Arith(LuaArithmeticOperator.Shl);
        return Pop();
    }

    public LuaValue Shr(LuaValue a, LuaValue b)
    {
        Push(a);
        Push(b);
        Arith(LuaArithmeticOperator.Shr);
        return Pop();
    }

    public LuaValue Len(LuaValue a)
    {
        Push(a);
        Len(-1);
        return Pop();
    }

    public LuaValue Concat(params ReadOnlySpan<LuaValue> values)
    {
        foreach (var value in values)
        {
            Push(value);
        }
        Concat(values.Length);
        return Pop();
    }

    public bool Equals(LuaValue a, LuaValue b)
    {
        Push(a);
        Push(b);
        Compare(LuaComparisonOperator.Equal);
        return ToBoolean(-1);
    }

    public bool LessThan(LuaValue a, LuaValue b)
    {
        Push(a);
        Push(b);
        Compare(LuaComparisonOperator.Less);
        return ToBoolean(-1);
    }

    public bool LessThanOrEqual(LuaValue a, LuaValue b)
    {
        Push(a);
        Push(b);
        Compare(LuaComparisonOperator.LessOrEqual);
        return ToBoolean(-1);
    }

    public LuaTable CreateTable(int initialArraySize = 0,
        int initialRecordsSize = 0
    )
    {
        NewTable(initialArraySize, initialRecordsSize);
        return new LuaTable(this, Ref());
    }

    public LuaUserData CreateUserData(int size, int userValueCount = 1)
    {
        NewUserData(size, userValueCount);
        return new LuaUserData(this, Ref());
    }

    public LuaFunction CreateFunction(LuaFunc function,
        int upvalueCount = 0
    )
    {
        NewFunction(function, upvalueCount);
        return new LuaFunction(this, Ref());
    }

    public void RegisterFunction(ReadOnlySpan<char> name,
        LuaFunc function,
        int upvalueCount = 0
    )
    {
        NewFunction(function, upvalueCount);
        SetGlobal(name);
    }

    public LuauState CreateThread()
    {
        NewThread();
        return ToThread(-1);
    }

    public int Call(LuaFunction function,
        params ReadOnlySpan<LuaValue> args
    )
    {
        var baseTop = GetTop();
        PushValue(function.Reference);
        foreach (var arg in args)
        {
            Push(arg);
        }
        Call(args.Length, -1);
        return GetTop() - baseTop;
    }

    public ValueTask<int> CallAsync(LuaFunction function,
        LuaValue[] args,
        CancellationToken cancellationToken = default
    )
    {
        return CallAsync(function, args.AsMemory(), cancellationToken);
    }

    public async ValueTask<int> CallAsync(LuaFunction function,
        ReadOnlyMemory<LuaValue> args,
        CancellationToken cancellationToken = default
    )
    {
        var baseTop = GetTop();
        PushValue(function.Reference);
        var span = args.Span;
        for (int i = 0; i < span.Length; i++)
        {
            Push(span[i]);
        }
        await CallAsync(args.Length, -1, cancellationToken).ConfigureAwait(false);
        return GetTop() - baseTop;
    }

    public LuaValue[] Resume(params ReadOnlySpan<LuaValue> args)
    {
        foreach (var arg in args)
        {
            Push(arg);
        }
        Resume(args.Length);

        var resultCount = GetTop();
        var results = new LuaValue[resultCount];
        for (int i = 0; i < resultCount; i++)
        {
            results[i] = ToLuaValue(i + 1);
        }
        SetTop(0);
        return results;
    }

    public LuaValue[] DoString(ReadOnlySpan<char> code,
        ReadOnlySpan<LuaValue> args = default
    )
    {
        var baseTop = GetTop();

        LoadString(code, "chunk");
        for (int i = 0; i < args.Length; i++)
        {
            Push(args[i]);
        }
        Call(args.Length, -1);

        var currentTop = GetTop();
        var returnCount = currentTop - baseTop;

        var results = new LuaValue[returnCount];
        for (int i = 0; i < returnCount; i++)
        {
            results[i] = ToLuaValue(baseTop + 1 + i);
        }
        SetTop(baseTop);

        return results;
    }

    public byte[] Dump(int index, bool strip)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(1024);
        try
        {
            while (true)
            {
                if (TryDump(index, strip, buffer, out var bytesWritten))
                {
                    return buffer.AsSpan(0, bytesWritten).ToArray();
                }
                var newBuffer = ArrayPool<byte>.Shared.Rent(buffer.Length * 2);
                Array.Copy(buffer, newBuffer, buffer.Length);
                ArrayPool<byte>.Shared.Return(buffer);
                buffer = newBuffer;
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    #endregion

    #region LuaAsyncStateExtensions
    
    public LuaFunction CreateFunction(
        AsyncLuaFunc function,
        int upvalueCount = 0
    )
    {
        NewFunction(function, upvalueCount);
        return new LuaFunction(this, Ref());
    }

    public void RegisterFunction(
        ReadOnlySpan<char> name,
        AsyncLuaFunc function,
        int upvalueCount = 0
    )
    {
        NewFunction(function, upvalueCount);
        SetGlobal(name);
    }

    public ValueTask CallAsync(
        int argCount,
        int resultCount,
        CancellationToken cancellationToken = default
    )
    {
        var co = CreateThread();
        Pop(1);
        XMove(co, argCount + 1);
        return RunAndPushAsync(co, argCount, resultCount, cancellationToken);
    }

    public ValueTask<LuaValue[]> ResumeAsync(
        LuaValue[] args,
        CancellationToken cancellationToken = default
    )
    {
        return ResumeAsync(args.AsMemory(), cancellationToken);
    }

    public async ValueTask<LuaValue[]> ResumeAsync(
        ReadOnlyMemory<LuaValue> args,
        CancellationToken cancellationToken = default
    )
    {
        var span = args.Span;
        for (int i = 0; i < span.Length; i++)
        {
            Push(span[i]);
        }
        await ResumeAsync(args.Length, cancellationToken).ConfigureAwait(false);

        var resultCount = GetTop();
        var results = new LuaValue[resultCount];
        for (int i = 0; i < resultCount; i++)
        {
            results[i] = ToLuaValue(i + 1);
        }
        SetTop(0);
        return results;
    }

    public ValueTask<LuaValue[]> DoStringAsync(
        ReadOnlySpan<char> code,
        ReadOnlyMemory<LuaValue> args = default,
        CancellationToken cancellationToken = default
    )
    {
        var co = CreateThread();
        Pop(1);
        co.LoadString(code, "chunk");
        var span = args.Span;
        for (int i = 0; i < span.Length; i++)
        {
            co.Push(span[i]);
        }
        return co.RunAndCollectAsync(args.Length, cancellationToken);
    }

    async ValueTask RunAndPushAsync(
        LuauState co,
        int argCount,
        int resultCount,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await co.ResumeAsync(argCount, cancellationToken).ConfigureAwait(false);
            int actual = co.GetTop();
            if (resultCount < 0)
            {
                if (actual > 0)
                    co.XMove(this, actual);
            }
            else if (actual >= resultCount)
            {
                co.SetTop(resultCount);
                if (resultCount > 0)
                    co.XMove(this, resultCount);
            }
            else
            {
                if (actual > 0)
                    co.XMove(this, actual);
                for (int i = 0; i < resultCount - actual; i++)
                {
                    PushNil();
                }
            }
        }
        finally
        {
            co.Dispose();
        }
    }

    async ValueTask<LuaValue[]> RunAndCollectAsync(
        int initialArgCount,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await ResumeAsync(initialArgCount, cancellationToken).ConfigureAwait(false);
            int top = GetTop();
            if (top == 0)
            {
                return [];
            }
            var results = new LuaValue[top];
            for (int i = 0; i < top; i++)
            {
                results[i] = ToLuaValue(i + 1);
            }
            SetTop(0);
            return results;
        }
        finally
        {
            Dispose();
        }
    }

    #endregion
}
