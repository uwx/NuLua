namespace NuLua.Polyfills;

#if !NET6_0_OR_GREATER

public readonly struct CLong : IEquatable<CLong>
{
    public CLong(int value)
    {
        Value = value;
    }

    public CLong(nint value)
    {
        Value = value;
    }

    public nint Value { get; }

    public bool Equals(CLong other) => Value == other.Value;

    public override bool Equals(object? obj) => obj is CLong other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString();
}

public readonly struct CULong : IEquatable<CULong>
{
    public CULong(uint value)
    {
        Value = value;
    }

    public CULong(nuint value)
    {
        Value = value;
    }

    public nuint Value { get; }

    public bool Equals(CULong other) => Value == other.Value;

    public override bool Equals(object? obj) => obj is CULong other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString();
}
#endif
