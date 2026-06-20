namespace NuLua;

public sealed class LuaFunction(ILuaState state, LuaReference reference) : ILuaObject
{
    public LuaReference Reference => reference;

    public LuaValue[] Invoke(params ReadOnlySpan<LuaValue> args)
    {
        var resultCount = state.Call(this, args);
        var results = new LuaValue[resultCount];
        for (int i = 0; i < resultCount; i++)
        {
            results[i] = state.ToLuaValue(-resultCount + i);
        }
        state.SetTop(-resultCount - 1);
        return results;
    }

    public async ValueTask<LuaValue[]> InvokeAsync(
        ReadOnlyMemory<LuaValue> args,
        CancellationToken cancellationToken = default
    )
    {
        var resultCount = await state.CallAsync(this, args, cancellationToken);
        var results = new LuaValue[resultCount];
        for (int i = 0; i < resultCount; i++)
        {
            results[i] = state.ToLuaValue(-resultCount + i);
        }
        state.SetTop(-resultCount - 1);
        return results;
    }

    public void Dispose()
    {
        state.Unref(Reference);
    }
}
