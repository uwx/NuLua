using NuLua.Luau;

namespace NuLua;

public sealed class LuaFunctionRef(LuauState state, LuaReference reference) : LuaObjectRef(state, reference)
{
    readonly LuauState state = state;

    public LuaRefValue[] Invoke(params ReadOnlySpan<LuaRefValue> args)
    {
        var resultCount = state.Call(this, args);
        var results = new LuaRefValue[resultCount];
        for (int i = 0; i < resultCount; i++)
        {
            results[i] = state.ToLuaValue(-resultCount + i);
        }
        state.SetTop(-resultCount - 1);
        return results;
    }

    public async ValueTask<LuaRefValue[]> InvokeAsync(
        ReadOnlyMemory<LuaRefValue> args,
        CancellationToken cancellationToken = default
    )
    {
        var resultCount = await state.CallAsync(this, args, cancellationToken);
        var results = new LuaRefValue[resultCount];
        for (int i = 0; i < resultCount; i++)
        {
            results[i] = state.ToLuaValue(-resultCount + i);
        }
        state.SetTop(-resultCount - 1);
        return results;
    }
}
