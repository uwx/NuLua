using NuLua.Luau;

namespace NuLua;

public sealed class LuaFunction(LuauState state, LuaReference reference) : LuaObject(state, reference)
{
    readonly LuauState state = state;

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
}
