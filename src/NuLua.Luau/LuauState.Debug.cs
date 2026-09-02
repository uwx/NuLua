using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NuLua.Interop.Luau;

namespace NuLua.Luau;

public sealed unsafe partial class LuauState
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void LuauDebugCallbackDelegate(lua_State* L, lua_Debug* ar);

    delegate* unmanaged[Cdecl]<lua_State*, lua_Debug*, void> debugBreakDelegate;
    delegate* unmanaged[Cdecl]<lua_State*, lua_Debug*, void> debugStepDelegate;
    delegate* unmanaged[Cdecl]<lua_State*, lua_Debug*, void> debugInterruptDelegate;

    LuaHook? debugBreakHook;
    LuaHook? debugStepHook;
    LuaHook? debugInterruptHook;

    public int GetStackDepth()
    {
        CheckDisposed();
        return NativeMethods.lua_stackdepth(ptr);
    }

    public bool TryGetStackInfo(int level, LuaDebugInfoFields fields, out LuaDebugInfo info)
    {
        CheckDisposed();

        scoped Span<byte> buffer = stackalloc byte[16];
        int len = 0;
        if ((fields & LuaDebugInfoFields.Name) != 0)
            buffer[len++] = (byte)'n';
        if ((fields & LuaDebugInfoFields.Source) != 0)
            buffer[len++] = (byte)'s';
        if ((fields & LuaDebugInfoFields.CurrentLine) != 0)
            buffer[len++] = (byte)'l';
        if ((fields & LuaDebugInfoFields.Upvalues) != 0)
        {
            buffer[len++] = (byte)'u';
            buffer[len++] = (byte)'a';
        }
        if ((fields & LuaDebugInfoFields.Function) != 0)
            buffer[len++] = (byte)'f';
        buffer[len] = 0;
        var what = buffer[..(len + 1)];

        lua_Debug ar = default;
        fixed (byte* whatPtr = what)
        {
            if (NativeMethods.lua_getinfo(ptr, level, whatPtr, &ar) == 0)
            {
                info = default;
                return false;
            }
        }
        info = ReadDebugInfo(&ar);
        return true;
    }

    public string? GetLocal(int level, int n)
    {
        CheckDisposed();
        var name = NativeMethods.lua_getlocal(ptr, level, n);
        return name == null ? null : Utf8NullTerminated(name);
    }

    public string? SetLocal(int level, int n)
    {
        CheckDisposed();
        var name = NativeMethods.lua_setlocal(ptr, level, n);
        return name == null ? null : Utf8NullTerminated(name);
    }

    public string? GetUpvalue(int funcIndex, int n)
    {
        CheckDisposed();
        var name = NativeMethods.lua_getupvalue(ptr, funcIndex, n);
        return name == null ? null : Utf8NullTerminated(name);
    }

    public string? SetUpvalue(int funcIndex, int n)
    {
        CheckDisposed();
        var name = NativeMethods.lua_setupvalue(ptr, funcIndex, n);
        return name == null ? null : Utf8NullTerminated(name);
    }

    public LuaHook? GetHook() => null;

    public LuaHookMask GetHookMask() => LuaHookMask.None;

    public int GetHookCount() => 0;

    public int GetArgument(int level, int n)
    {
        CheckDisposed();
        return NativeMethods.lua_getargument(ptr, level, n);
    }

    public void SetSingleStep(bool enabled)
    {
        CheckDisposed();
        NativeMethods.lua_singlestep(ptr, enabled ? 1 : 0);
    }

    public int SetBreakpoint(int funcIndex, int line, bool enabled)
    {
        CheckDisposed();
        return NativeMethods.lua_breakpoint(ptr, funcIndex, line, enabled ? 1 : 0);
    }

    public string GetDebugTrace()
    {
        CheckDisposed();
        var p = NativeMethods.lua_debugtrace(ptr);
        return Utf8NullTerminated(p) ?? string.Empty;
    }

    public void GetCoverage(int funcIndex, Action<LuauCoverageEntry> visit)
    {
        CheckDisposed();
        if (visit == null)
            throw new ArgumentNullException(nameof(visit));

        var handle = GCHandle.Alloc(visit);
        try
        {
            NativeMethods.lua_getcoverage(
                ptr,
                funcIndex,
                (void*)GCHandle.ToIntPtr(handle),
                &CoverageCallback
            );
        }
        finally
        {
            handle.Free();
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    static void CoverageCallback(
        void* context,
        byte* function,
        int linedefined,
        int depth,
        int* hits,
        nuint size
    )
    {
        var handle = GCHandle.FromIntPtr((nint)context);
        if (handle.Target is not Action<LuauCoverageEntry> visit)
        {
            return;
        }

        var hitArray = new int[(int)size];
        for (int i = 0; i < hitArray.Length; i++)
        {
            hitArray[i] = hits[i];
        }

        visit(
            new LuauCoverageEntry
            {
                Function = Utf8NullTerminated(function),
                LineDefined = linedefined,
                Depth = depth,
                Hits = hitArray,
            }
        );
    }

    public void SetDebugBreakCallback(LuaHook? callback)
    {
        CheckDisposed();
        debugBreakHook = callback;
        debugBreakDelegate = callback == null ? null : &DebugBreakEntry;
        var callbacks = NativeMethods.lua_callbacks(ptr);
        callbacks->debugbreak =
            callback == null
                ? null
                : debugBreakDelegate!;
    }

    public void SetDebugStepCallback(LuaHook? callback)
    {
        CheckDisposed();
        debugStepHook = callback;
        debugStepDelegate = callback == null ? null : &DebugStepEntry;
        var callbacks = NativeMethods.lua_callbacks(ptr);
        callbacks->debugstep =
            callback == null
                ? null
                : debugStepDelegate!;
    }

    public void SetDebugInterruptCallback(LuaHook? callback)
    {
        CheckDisposed();
        debugInterruptHook = callback;
        debugInterruptDelegate = callback == null ? null : &DebugInterruptEntry;
        var callbacks = NativeMethods.lua_callbacks(ptr);
        callbacks->debuginterrupt =
            callback == null
                ? null
                : debugInterruptDelegate!;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    static void DebugBreakEntry(lua_State* L, lua_Debug* ar)
    {
        if (!ptrToState.TryGetValue((nint)L, out var state))
            return;
        var hook = state.debugBreakHook;
        hook?.Invoke(state, LuaHookEvent.Line, ar->currentline);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    static void DebugStepEntry(lua_State* L, lua_Debug* ar)
    {
        if (!ptrToState.TryGetValue((nint)L, out var state))
            return;
        var hook = state.debugStepHook;
        hook?.Invoke(state, LuaHookEvent.Line, ar->currentline);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    static void DebugInterruptEntry(lua_State* L, lua_Debug* ar)
    {
        if (!ptrToState.TryGetValue((nint)L, out var state))
            return;
        var hook = state.debugInterruptHook;
        hook?.Invoke(state, LuaHookEvent.Line, ar->currentline);
    }

    static LuaDebugInfo ReadDebugInfo(lua_Debug* ar)
    {
        return new LuaDebugInfo
        {
            Name = Utf8NullTerminated(ar->name),
            NameWhat = null,
            What = Utf8NullTerminated(ar->what),
            Source = Utf8NullTerminated(ar->source),
            ShortSource = Utf8NullTerminated(ar->short_src),
            CurrentLine = ar->currentline,
            LineDefined = ar->linedefined,
            LastLineDefined = -1,
            Upvalues = ar->nupvals,
            Parameters = ar->nparams,
            IsVararg = ar->isvararg != 0,
            IsTailCall = false,
            FirstTransferred = 0,
            TransferredCount = 0,
        };
    }

    static string? Utf8NullTerminated(byte* p)
    {
        if (p == null)
            return null;
        int n = 0;
        while (p[n] != 0)
            n++;
        return new string((sbyte*)p, 0, n);
    }
}

public readonly struct LuauCoverageEntry
{
    public string? Function { get; init; }
    public int LineDefined { get; init; }
    public int Depth { get; init; }
    public int[] Hits { get; init; }
}
