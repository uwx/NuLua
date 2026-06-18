namespace NuLua.Tests;

public class AsyncCancellationTests
{
    static AsyncLuaFunction RegisterWait(LuaStateCase lua, ILuaState state, string name = "wait")
    {
        var func = lua.CreateAsyncFunction(
            state,
            async (s, args, ct) =>
            {
                var ms = args[0].Read<double>();
                await Task.Delay(TimeSpan.FromMilliseconds(ms), ct).ConfigureAwait(false);
                return 0;
            }
        );
        state[name] = func;
        return func;
    }

    [Test]
    [MethodDataSource(typeof(LuaStateCases), nameof(LuaStateCases.All))]
    public async Task DoStringAsyncThrowsWhenTokenAlreadyCancelled(LuaStateCase lua)
    {
        using var state = lua.CreateState();
        state.OpenLibraries();
        using var _ = RegisterWait(lua, state);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert
            .That(async () => await state.DoStringAsync("wait(1000)", default, cts.Token))
            .Throws<OperationCanceledException>();
    }

    [Test]
    [MethodDataSource(typeof(LuaStateCases), nameof(LuaStateCases.All))]
    public async Task DoStringAsyncCancelsDuringAwait(LuaStateCase lua)
    {
        using var state = lua.CreateState();
        state.OpenLibraries();
        using var _ = RegisterWait(lua, state);

        using var cts = new CancellationTokenSource();
        var task = state.DoStringAsync("wait(60000)", default, cts.Token).AsTask();
        cts.CancelAfter(TimeSpan.FromMilliseconds(50));

        await Assert.That(async () => await task).Throws<OperationCanceledException>();
    }

    [Test]
    [MethodDataSource(typeof(LuaStateCases), nameof(LuaStateCases.All))]
    public async Task DoStringAsyncCompletesNormallyWhenTokenIsNotCancelled(LuaStateCase lua)
    {
        using var state = lua.CreateState();
        state.OpenLibraries();
        using var _ = RegisterWait(lua, state);

        var results = await state.DoStringAsync("wait(1); return 42");

        await Assert.That(results).Count().IsEqualTo(1);
        await Assert.That(results[0].Read<double>()).IsEqualTo(42);
        await Assert.That(state.GetTop()).IsEqualTo(0);
    }

    [Test]
    [MethodDataSource(typeof(LuaStateCases), nameof(LuaStateCases.All))]
    public async Task StateCanRunAnotherAsyncCallAfterCancellation(LuaStateCase lua)
    {
        using var state = lua.CreateState();
        state.OpenLibraries();
        using var _ = RegisterWait(lua, state);

        using (var cts = new CancellationTokenSource())
        {
            var task = state.DoStringAsync("wait(60000)", default, cts.Token).AsTask();
            cts.CancelAfter(TimeSpan.FromMilliseconds(50));
            await Assert.That(async () => await task).Throws<OperationCanceledException>();
        }

        var results = await state.DoStringAsync("wait(1); return 'ok'");

        await Assert.That(results).Count().IsEqualTo(1);
        await Assert.That(results[0].Read<string>()).IsEqualTo("ok");
        await Assert.That(state.GetTop()).IsEqualTo(0);
    }

    [Test]
    [MethodDataSource(typeof(LuaStateCases), nameof(LuaStateCases.All))]
    public async Task CancellationStopsBeforeFurtherResumesWhenUserIgnoresToken(LuaStateCase lua)
    {
        using var state = lua.CreateState();
        state.OpenLibraries();

        int callCount = 0;
        var func = lua.CreateAsyncFunction(
            state,
            async (s, args, ct) =>
            {
                Interlocked.Increment(ref callCount);
                await Task.Delay(TimeSpan.FromMilliseconds(20)).ConfigureAwait(false);
                return 0;
            }
        );
        state["step"] = func;

        using var cts = new CancellationTokenSource();
        var task = state
            .DoStringAsync("for i=1,10 do step() end", default, cts.Token)
            .AsTask();

        await Task.Delay(50).ConfigureAwait(false);
        cts.Cancel();

        await Assert.That(async () => await task).Throws<OperationCanceledException>();
        await Assert.That(callCount).IsLessThan(10);
        func.Dispose();
    }

    [Test]
    [MethodDataSource(typeof(LuaStateCases), nameof(LuaStateCases.All))]
    public async Task TokenIsPropagatedToAsyncFunction(LuaStateCase lua)
    {
        using var state = lua.CreateState();
        state.OpenLibraries();

        CancellationToken observed = default;
        var func = lua.CreateAsyncFunction(
            state,
            (s, args, ct) =>
            {
                observed = ct;
                return new ValueTask<int>(0);
            }
        );
        state["capture"] = func;

        using var cts = new CancellationTokenSource();
        await state.DoStringAsync("capture()", default, cts.Token);

        await Assert.That(observed).IsEqualTo(cts.Token);
        func.Dispose();
    }

    [Test]
    [MethodDataSource(typeof(LuaStateCases), nameof(LuaStateCases.All))]
    public async Task InvokeAsyncRespectsCancellation(LuaStateCase lua)
    {
        using var state = lua.CreateState();
        state.OpenLibraries();
        using var wait = RegisterWait(lua, state);

        using var cts = new CancellationTokenSource();
        var task = wait.InvokeAsync(new LuaValue[] { LuaValue.FromNumber(60000) }, cts.Token)
            .AsTask();
        cts.CancelAfter(TimeSpan.FromMilliseconds(50));

        await Assert.That(async () => await task).Throws<OperationCanceledException>();
    }
}
