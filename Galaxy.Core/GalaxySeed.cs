namespace Galaxy.Core;

/// <summary>A galaxy's complete 64-bit procedural root seed.</summary>
public readonly record struct GalaxySeed(ulong Value)
{
    public override string ToString() => Value.ToString();
}
