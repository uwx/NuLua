namespace NuLua;

public sealed class AsyncLuaFunction(ILuaState state, LuaReference reference)
    : LuaFunctionBase(state, reference)
{
    public async ValueTask<int> InvokeAsync(
        ReadOnlyMemory<LuaValue> args,
        CancellationToken cancellationToken = default
    )
    {
        int baseTop = State.GetTop();
        State.PushValue(Reference);
        var span = args.Span;
        for (int i = 0; i < span.Length; i++)
        {
            State.Push(span[i]);
        }
        await State.CallAsync(args.Length, -1, cancellationToken).ConfigureAwait(false);
        return State.GetTop() - baseTop;
    }
}
