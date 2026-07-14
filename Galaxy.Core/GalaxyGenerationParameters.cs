namespace Galaxy.Core;

public sealed record GalaxyGenerationParameters
{
    public int SpiralArmCount { get; init; } = 4;

    public double Radius { get; init; } = 8.0;

    public double DiskThickness { get; init; } = 0.8;

    public double BulgeRadius { get; init; } = 2.0;

    /// <summary>Total arm rotation from the center to the rim, in radians.</summary>
    public double ArmTwistRadians { get; init; } = 4.5;

    /// <summary>Scale length of the exponential disk density profile.</summary>
    public double DiskScaleLength { get; init; } = 2.4;

    /// <summary>Relative weight of the axisymmetric disk between spiral arms.</summary>
    public double InterArmDensityFactor { get; init; } = 0.35;

    /// <summary>Relative density enhancement contributed by spiral arms.</summary>
    public double ArmDensityMultiplier { get; init; } = 1.5;

    /// <summary>Relative density contributed by the central spheroidal bulge.</summary>
    public double BulgeDensityMultiplier { get; init; } = 1.8;

    /// <summary>Required empty gap between the visual surfaces of two stars.</summary>
    public double MinimumStarDistance { get; init; } = 0.006;

    public int MaxPlacementAttempts { get; init; } = 256;

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

        if (!double.IsFinite(DiskScaleLength) || DiskScaleLength <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(DiskScaleLength), "Disk scale length must be finite and positive.");
        }

        if (!double.IsFinite(InterArmDensityFactor) || InterArmDensityFactor < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(InterArmDensityFactor), "Inter-arm density must be finite and non-negative.");
        }

        if (!double.IsFinite(ArmDensityMultiplier) || ArmDensityMultiplier < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(ArmDensityMultiplier), "Arm density multiplier must be finite and non-negative.");
        }

        if (!double.IsFinite(BulgeDensityMultiplier) || BulgeDensityMultiplier < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(BulgeDensityMultiplier), "Bulge density multiplier must be finite and non-negative.");
        }

        double effectiveDensity = InterArmDensityFactor + ArmDensityMultiplier +
            (BulgeRadius > 0.0 ? BulgeDensityMultiplier : 0.0);
        if (effectiveDensity <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(InterArmDensityFactor), "At least one density component must be positive.");
        }

        if (!double.IsFinite(MinimumStarDistance) || MinimumStarDistance < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(MinimumStarDistance), "Minimum star distance must be finite and non-negative.");
        }

        if (MaxPlacementAttempts is < 1 or > 4096)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxPlacementAttempts), "Placement attempts must be between 1 and 4,096.");
        }

        if (StarCount is < 1 or > 10_000_000)
        {
            throw new ArgumentOutOfRangeException(nameof(StarCount), "Star count must be between 1 and 10,000,000.");
        }
    }
}
