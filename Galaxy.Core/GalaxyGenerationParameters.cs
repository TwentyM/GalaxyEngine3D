namespace Galaxy.Core;

public sealed record GalaxyGenerationParameters
{
    public int SpiralArmCount { get; init; } = 4;

    public double Radius { get; init; } = 8.0;

    public double DiskThickness { get; init; } = 0.8;

    public double BulgeRadius { get; init; } = 2.0;

    /// <summary>Total arm rotation from the center to the rim, in radians.</summary>
    public double ArmTwistRadians { get; init; } = 4.5;

    public int StarCount { get; init; } = 50_000;

    public void Validate()
    {
        if (SpiralArmCount is < 1 or > 32)
        {
            throw new ArgumentOutOfRangeException(nameof(SpiralArmCount), "Spiral arm count must be between 1 and 32.");
        }

        if (!double.IsFinite(Radius) || Radius <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(Radius), "Radius must be finite and positive.");
        }

        if (!double.IsFinite(DiskThickness) || DiskThickness < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(DiskThickness), "Disk thickness must be finite and non-negative.");
        }

        if (!double.IsFinite(BulgeRadius) || BulgeRadius < 0.0 || BulgeRadius > Radius)
        {
            throw new ArgumentOutOfRangeException(nameof(BulgeRadius), "Bulge radius must be finite and between zero and the galaxy radius.");
        }

        if (!double.IsFinite(ArmTwistRadians))
        {
            throw new ArgumentOutOfRangeException(nameof(ArmTwistRadians), "Arm twist must be finite.");
        }

        if (StarCount is < 1 or > 10_000_000)
        {
            throw new ArgumentOutOfRangeException(nameof(StarCount), "Star count must be between 1 and 10,000,000.");
        }
    }
}
