namespace NuLua;

public sealed class LuaFunction(ILuaState state, LuaReference reference) : ILuaObject
{
    public LuaReference Reference => reference;

    public int Invoke(ReadOnlySpan<LuaValue> args)
    {
        state.PushValue(Reference);
        foreach (var arg in args)
        {
            state.Push(arg);
        }
        state.Call(args.Length, 0);
        return 0;
    }

    public async ValueTask<int> InvokeAsync(
        ReadOnlyMemory<LuaValue> args,
        CancellationToken cancellationToken = default
    )
    {
        int baseTop = state.GetTop();
        state.PushValue(Reference);
        var span = args.Span;
        for (int i = 0; i < span.Length; i++)
        {
            state.Push(span[i]);
        }
        await state.CallAsync(args.Length, -1, cancellationToken).ConfigureAwait(false);
        return state.GetTop() - baseTop;
    }

    public void Dispose()
    {
        state.Unref(Reference);
    }
}
