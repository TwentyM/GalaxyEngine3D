namespace Galaxy.Core.Generation;

internal static class DeterministicHash
{
    private const ulong GoldenRatio = 0x9E3779B97F4A7C15UL;

    public static ulong Derive(ulong parent, ulong role, ulong stableIndex)
    {
        ulong namespacedParent = Mix(parent ^ role);
        return Mix(namespacedParent ^ Mix(stableIndex + GoldenRatio));
    }

    public static ulong Mix(ulong value)
    {
        value += GoldenRatio;
        value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
        value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
        return value ^ (value >> 31);
    }

    public static double UnitDouble(ulong seed, ulong stream)
    {
        ulong bits = Mix(seed + (stream * GoldenRatio)) >> 11;
        return bits * (1.0 / 9_007_199_254_740_992.0);
    }
}
