using System.Runtime.InteropServices;
using NuLua.Interop.Luau;

namespace NuLua.Luau;

public sealed unsafe partial class LuauState
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void LuauDebugCallbackDelegate(lua_State* L, lua_Debug* ar);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void LuauInterruptDelegate(lua_State* L, int gc);

    LuauDebugCallbackDelegate? debugBreakDelegate;
    LuauDebugCallbackDelegate? debugStepDelegate;
    LuauDebugCallbackDelegate? debugInterruptDelegate;

    LuaHook<LuauState>? debugBreakHook;
    LuaHook<LuauState>? debugStepHook;
    LuaHook<LuauState>? debugInterruptHook;

    // Generic hook plumbing modeled on mlua's Luau backend: Line uses
    // singlestep+debugstep, Count uses the interrupt callback. Call/Return
    // are not natively dispatched by Luau, so we expose them via the same
    // interrupt path by checking event boundaries when feasible.
    LuauDebugCallbackDelegate? hookStepCallback;
    LuauInterruptDelegate? hookInterruptCallback;
    LuaHook<LuauState>? hookDelegate;
    LuaHook? nonGenericHookDelegate;
    LuaHookMask hookMask;
    int hookCount;
    int hookCountdown;

    int ILuaDebug.GetStackDepth()
    {
        CheckDisposed();
        return NativeMethods.lua_stackdepth(ptr);
    }

    bool ILuaDebug.TryGetStackInfo(int level, LuaDebugInfoFields fields, out LuaDebugInfo info)
    {
        CheckDisposed();
        var what = BuildWhatString(fields);
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

    string? ILuaDebug.GetLocal(int level, int n)
    {
        CheckDisposed();
        var name = NativeMethods.lua_getlocal(ptr, level, n);
        return name == null ? null : Utf8NullTerminated(name);
    }

    string? ILuaDebug.SetLocal(int level, int n)
    {
        CheckDisposed();
        var name = NativeMethods.lua_setlocal(ptr, level, n);
        return name == null ? null : Utf8NullTerminated(name);
    }

    string? ILuaDebug.GetUpvalue(int funcIndex, int n)
    {
        CheckDisposed();
        var name = NativeMethods.lua_getupvalue(ptr, funcIndex, n);
        return name == null ? null : Utf8NullTerminated(name);
    }

    string? ILuaDebug.SetUpvalue(int funcIndex, int n)
    {
        CheckDisposed();
        var name = NativeMethods.lua_setupvalue(ptr, funcIndex, n);
        return name == null ? null : Utf8NullTerminated(name);
    }

    nint ILuaDebug.UpvalueId(int funcIndex, int n) =>
        throw new NotSupportedException("lua_upvalueid is not available on Luau.");

    void ILuaDebug.UpvalueJoin(int fIdx1, int n1, int fIdx2, int n2) =>
        throw new NotSupportedException("lua_upvaluejoin is not available on Luau.");

    void ILuaDebug<LuauState>.SetHook(LuaHook<LuauState>? hook, LuaHookMask mask, int count)
    {
        CheckDisposed();
        InstallHook(hook, nonGenericHook: null, mask, count);
    }

    void ILuaDebug.SetHook(LuaHook? hook, LuaHookMask mask, int count)
    {
        CheckDisposed();
        InstallHook(typedHook: null, hook, mask, count);
    }

    LuaHook<LuauState>? ILuaDebug<LuauState>.GetHook() => hookDelegate;

    LuaHook? ILuaDebug.GetHook()
    {
        if (nonGenericHookDelegate != null) return nonGenericHookDelegate;
        var typed = hookDelegate;
        return typed == null ? null : (s, ev, line) => typed((LuauState)s, ev, line);
    }

    LuaHookMask ILuaDebug.GetHookMask() => hookMask;

    int ILuaDebug.GetHookCount() => hookCount;

    void InstallHook(LuaHook<LuauState>? typedHook, LuaHook? nonGenericHook, LuaHookMask mask, int count)
    {
        var clearing = (typedHook == null && nonGenericHook == null) || mask == LuaHookMask.None;
        var callbacks = NativeMethods.lua_callbacks(ptr);

        if (clearing)
        {
            hookDelegate = null;
            nonGenericHookDelegate = null;
            hookMask = LuaHookMask.None;
            hookCount = 0;
            hookCountdown = 0;
            hookStepCallback = null;
            hookInterruptCallback = null;
            NativeMethods.lua_singlestep(ptr, 0);
            callbacks->debugstep = null;
            callbacks->interrupt = null;
            return;
        }

        // Luau does not provide call/return hooks. Reject unsupported bits up front
        // so callers get a clear error instead of silent no-ops, matching the
        // expectations the rest of NuLua's API sets for hooks.
        if ((mask & (LuaHookMask.Call | LuaHookMask.Return)) != 0)
        {
            throw new NotSupportedException(
                "Luau does not support Call/Return hooks"
            );
        }

        hookDelegate = typedHook;
        nonGenericHookDelegate = nonGenericHook;
        hookMask = mask;
        hookCount = count;
        hookCountdown = count;

        if ((mask & LuaHookMask.Line) != 0)
        {
            hookStepCallback = HookStepEntry;
            NativeMethods.lua_singlestep(ptr, 1);
            callbacks->debugstep = (void*)Marshal.GetFunctionPointerForDelegate(hookStepCallback);
        }
        else
        {
            NativeMethods.lua_singlestep(ptr, 0);
            callbacks->debugstep = null;
            hookStepCallback = null;
        }

        if ((mask & LuaHookMask.Count) != 0 && count > 0)
        {
            hookInterruptCallback = HookInterruptEntry;
            callbacks->interrupt = (void*)Marshal.GetFunctionPointerForDelegate(hookInterruptCallback);
        }
        else
        {
            callbacks->interrupt = null;
            hookInterruptCallback = null;
        }
    }

    static void HookStepEntry(lua_State* L, lua_Debug* ar)
    {
        if (!ptrToState.TryGetValue((nint)L, out var state)) return;
        InvokeHook(state, LuaHookEvent.Line, ar->currentline);
    }

    static void HookInterruptEntry(lua_State* L, int gc)
    {
        // Luau invokes interrupt with gc != -1 only for GC events; -1 is the
        // ordinary per-instruction poll. Ignore the GC variant.
        if (gc >= 0) return;
        if (!ptrToState.TryGetValue((nint)L, out var state)) return;

        if (state.hookCount <= 0) return;
        state.hookCountdown--;
        if (state.hookCountdown > 0) return;
        state.hookCountdown = state.hookCount;

        int currentLine = -1;
        lua_Debug ar = default;
        fixed (byte* what = "l\0"u8)
        {
            if (NativeMethods.lua_getinfo(L, 0, what, &ar) != 0)
            {
                currentLine = ar.currentline;
            }
        }
        InvokeHook(state, LuaHookEvent.Count, currentLine);
    }

    static void InvokeHook(LuauState state, LuaHookEvent ev, int currentLine)
    {
        var typed = state.hookDelegate;
        if (typed != null)
        {
            typed(state, ev, currentLine);
            return;
        }
        var nonGeneric = state.nonGenericHookDelegate;
        nonGeneric?.Invoke(state, ev, currentLine);
    }

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
                CoverageCallback
            );
        }
        finally
        {
            handle.Free();
        }
    }

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

    public void SetDebugBreakCallback(LuaHook<LuauState>? callback)
    {
        CheckDisposed();
        debugBreakHook = callback;
        debugBreakDelegate = callback == null ? null : DebugBreakEntry;
        var callbacks = NativeMethods.lua_callbacks(ptr);
        callbacks->debugbreak =
            callback == null
                ? null
                : (void*)Marshal.GetFunctionPointerForDelegate(debugBreakDelegate!);
    }

    public void SetDebugStepCallback(LuaHook<LuauState>? callback)
    {
        CheckDisposed();
        debugStepHook = callback;
        debugStepDelegate = callback == null ? null : DebugStepEntry;
        var callbacks = NativeMethods.lua_callbacks(ptr);
        callbacks->debugstep =
            callback == null
                ? null
                : (void*)Marshal.GetFunctionPointerForDelegate(debugStepDelegate!);
    }

    public void SetDebugInterruptCallback(LuaHook<LuauState>? callback)
    {
        CheckDisposed();
        debugInterruptHook = callback;
        debugInterruptDelegate = callback == null ? null : DebugInterruptEntry;
        var callbacks = NativeMethods.lua_callbacks(ptr);
        callbacks->debuginterrupt =
            callback == null
                ? null
                : (void*)Marshal.GetFunctionPointerForDelegate(debugInterruptDelegate!);
    }

    static void DebugBreakEntry(lua_State* L, lua_Debug* ar)
    {
        if (!ptrToState.TryGetValue((nint)L, out var state))
            return;
        var hook = state.debugBreakHook;
        hook?.Invoke(state, LuaHookEvent.Line, ar->currentline);
    }

    static void DebugStepEntry(lua_State* L, lua_Debug* ar)
    {
        if (!ptrToState.TryGetValue((nint)L, out var state))
            return;
        var hook = state.debugStepHook;
        hook?.Invoke(state, LuaHookEvent.Line, ar->currentline);
    }

    static void DebugInterruptEntry(lua_State* L, lua_Debug* ar)
    {
        if (!ptrToState.TryGetValue((nint)L, out var state))
            return;
        var hook = state.debugInterruptHook;
        hook?.Invoke(state, LuaHookEvent.Line, ar->currentline);
    }

    static byte[] BuildWhatString(LuaDebugInfoFields fields)
    {
        Span<byte> buffer = stackalloc byte[16];
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
        return buffer[..(len + 1)].ToArray();
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
