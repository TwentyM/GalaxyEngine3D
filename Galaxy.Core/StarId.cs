namespace Galaxy.Core;

/// <summary>A stable star identifier derived from the generator version, galaxy seed and star index.</summary>
public readonly record struct StarId(ulong Value)
{
    public override string ToString() => Value.ToString("X16");
}
