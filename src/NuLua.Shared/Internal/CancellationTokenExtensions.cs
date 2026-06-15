#if !NET8_0_OR_GREATER

namespace NuLua;

internal static class CancellationTokenExtensions
{
    public static CancellationTokenRegistration UnsafeRegister(this CancellationToken cancellationToken, Action<object?> callback, object? state)
    {
        var restoreFlow = false;
        if (!ExecutionContext.IsFlowSuppressed())
        {
            ExecutionContext.SuppressFlow();
            restoreFlow = true;
        }

        try
        {
            return cancellationToken.Register(callback, state, false);
        }
        finally
        {
            if (restoreFlow)
            {
                ExecutionContext.RestoreFlow();
            }
        }
    }
}
#endif