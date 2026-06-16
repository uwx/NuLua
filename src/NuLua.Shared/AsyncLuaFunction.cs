namespace NuLua;

public sealed class AsyncLuaFunction(ILuaState state, LuaReference reference)
    : LuaFunctionBase(state, reference)
{
    public ValueTask<LuaValue[]> InvokeAsync(
        ReadOnlyMemory<LuaValue> args,
        CancellationToken cancellationToken = default
    )
    {
        return State.CallAsync(this, args, cancellationToken);
    }
}
