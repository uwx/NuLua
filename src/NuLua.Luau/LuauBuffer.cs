namespace NuLua.Luau;

public sealed class LuauBuffer(LuauState state, LuaReference reference) : ILuaObject
{
    readonly LuauState state = state;

    public LuaReference Reference => reference;

    // Luau buffers are not moved by the GC; the data pointer remains valid as long
    // as a reference keeps the buffer alive, so AsSpan can outlive the temporary
    // stack push it performs internally.
    public Span<byte> AsSpan()
    {
        state.PushValue(reference);
        var span = state.ToBufferSpan(state.GetTop());
        state.Pop(1);
        return span;
    }

    public int Length => AsSpan().Length;

    public ref byte this[int index]
    {
        get => ref AsSpan()[index];
    }

    public void CopyTo(Span<byte> destination) => AsSpan().CopyTo(destination);

    public void CopyFrom(ReadOnlySpan<byte> source) => source.CopyTo(AsSpan());

    public byte[] ToArray() => AsSpan().ToArray();

    public void Dispose()
    {
        state.Unref(reference);
    }
}
