namespace NuLua;

public static class LuaAsyncStateExtensions
{
    public static AsyncLuaFunction CreateFunction<TState>(
        this ILuaState<TState> state,
        AsyncLuaFunc<TState> function,
        int upvalueCount = 0
    )
        where TState : ILuaState<TState>
    {
        state.NewFunction(function, upvalueCount);
        return new AsyncLuaFunction(state, state.Ref());
    }

    public static void RegisterFunction<TState>(
        this ILuaState<TState> state,
        ReadOnlySpan<char> name,
        AsyncLuaFunc<TState> function,
        int upvalueCount = 0
    )
        where TState : ILuaState<TState>
    {
        state.NewFunction(function, upvalueCount);
        state.SetGlobal(name);
    }

    public static ValueTask CallAsync(
        this ILuaState state,
        int argCount,
        int resultCount,
        CancellationToken cancellationToken = default
    )
    {
        var co = state.CreateThread();
        state.Pop(1);
        state.XMove(co, argCount + 1);
        return RunAndPushAsync(state, co, argCount, resultCount, cancellationToken);
    }

    public static ValueTask<LuaValue[]> DoStringAsync(
        this ILuaState state,
        ReadOnlySpan<char> code,
        ReadOnlyMemory<LuaValue> args = default,
        CancellationToken cancellationToken = default
    )
    {
        var co = state.CreateThread();
        state.Pop(1);
        co.LoadString(code, "chunk");
        var span = args.Span;
        for (int i = 0; i < span.Length; i++)
        {
            co.Push(span[i]);
        }
        return RunAndCollectAsync(co, args.Length, cancellationToken);
    }

    static async ValueTask RunAndPushAsync(
        ILuaState state,
        ILuaState co,
        int argCount,
        int resultCount,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await co.CompleteAsync(argCount, cancellationToken).ConfigureAwait(false);
            int actual = co.GetTop();
            if (resultCount < 0)
            {
                if (actual > 0)
                    co.XMove(state, actual);
            }
            else if (actual >= resultCount)
            {
                co.SetTop(resultCount);
                if (resultCount > 0)
                    co.XMove(state, resultCount);
            }
            else
            {
                if (actual > 0)
                    co.XMove(state, actual);
                for (int i = 0; i < resultCount - actual; i++)
                {
                    state.PushNil();
                }
            }
        }
        finally
        {
            co.Dispose();
        }
    }

    static async ValueTask<LuaValue[]> RunAndCollectAsync(
        ILuaState co,
        int initialArgCount,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await co.CompleteAsync(initialArgCount, cancellationToken).ConfigureAwait(false);
            int top = co.GetTop();
            if (top == 0)
            {
                return [];
            }
            var results = new LuaValue[top];
            for (int i = 0; i < top; i++)
            {
                results[i] = co.ToLuaValue(i + 1);
            }
            co.SetTop(0);
            return results;
        }
        finally
        {
            co.Dispose();
        }
    }
}
