using NuLua.Interop.Lua55;

namespace NuLua.Lua55;

internal static class Lua55AsyncDriver
{
    public static async ValueTask RunAsync(
        Lua55State state,
        int initialArgCount,
        CancellationToken cancellationToken
    )
    {
        state.SetAsyncCancellationToken(cancellationToken);
        int currentArgs = initialArgCount;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var status = state.RunResumeStep(currentArgs);
            if (status == (int)NativeMethods.LUA_OK)
            {
                state.SetAsyncCancellationToken(default);
                return;
            }

            if (!state.TryTakePendingAsyncTask(out var task))
            {
                state.SetAsyncCancellationToken(default);
                throw new LuaException(
                    NativeMethods.LUA_ERRRUN,
                    "Coroutine yielded without a pending async task."
                );
            }

            currentArgs = await task.ConfigureAwait(false);
        }
    }
}
