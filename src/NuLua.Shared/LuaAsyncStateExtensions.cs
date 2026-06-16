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

    public static ValueTask<LuaValue[]> CallAsync(
        this ILuaState state,
        LuaFunctionBase function,
        ReadOnlyMemory<LuaValue> args = default,
        CancellationToken cancellationToken = default
    )
    {
        var co = state.CreateThread();
        co.PushValue(function.Reference);
        var span = args.Span;
        for (int i = 0; i < span.Length; i++)
        {
            co.Push(span[i]);
        }
        return RunAndCollectAsync(state, co, args.Length, cancellationToken);
    }

    public static ValueTask<LuaValue[]> DoStringAsync(
        this ILuaState state,
        ReadOnlySpan<char> code,
        ReadOnlyMemory<LuaValue> args = default,
        CancellationToken cancellationToken = default
    )
    {
        var co = state.CreateThread();
        co.LoadString(code);
        var span = args.Span;
        for (int i = 0; i < span.Length; i++)
        {
            co.Push(span[i]);
        }
        return RunAndCollectAsync(state, co, args.Length, cancellationToken);
    }

    static async ValueTask<LuaValue[]> RunAndCollectAsync(
        ILuaState state,
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
