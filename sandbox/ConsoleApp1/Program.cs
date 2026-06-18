using NuLua;
using NuLua.Luau;

using var state = LuauState.Create();
state.OpenBaseLibrary();

state["wait"] = state.CreateFunction(
    async (_, args, ct) =>
    {
        var seconds = args[0].Read<double>();
        await Task.Delay(TimeSpan.FromSeconds(seconds), ct);
        return 0;
    }
);

var t1 = state
    .DoStringAsync(
        """
    print("A: Waiting for 2 seconds...")
    wait(2)
    print("A: Done!")
"""
    )
    .AsTask();
var t2 = state
    .DoStringAsync(
        """
    print("B: Waiting for 2 seconds...")
    wait(2)
    print("B: Done!")
"""
    )
    .AsTask();
await Task.WhenAll(t1, t2);
