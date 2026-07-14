namespace Galaxy.Core;

public sealed record GalaxyGenerationParameters
{
    public int SpiralArmCount { get; init; } = 4;

    public double Radius { get; init; } = 8.0;

    public double DiskThickness { get; init; } = 0.8;

    /// <summary>Total arm rotation from the center to the rim, in radians.</summary>
    public double ArmTwistRadians { get; init; } = 4.5;

    /// <summary>Scale length of the exponential disk density profile.</summary>
    public double DiskScaleLength { get; init; } = 2.4;

    /// <summary>Relative weight of the axisymmetric disk between spiral arms.</summary>
    public double InterArmDensityFactor { get; init; } = 0.35;

    /// <summary>Relative density enhancement contributed by spiral arms.</summary>
    public double ArmDensityMultiplier { get; init; } = 1.5;

    /// <summary>Finite density enhancement at the center of the inner disk.</summary>
    public double InnerDensityMultiplier { get; init; } = 2.2;

    /// <summary>Vertical disk thickness multiplier reached at the center.</summary>
    public double InnerThicknessMultiplier { get; init; } = 2.4;

    /// <summary>Spiral arm width multiplier reached at the center.</summary>
    public double InnerArmWidthMultiplier { get; init; } = 3.0;

    /// <summary>Radius over which the spiral arms merge smoothly into the inner disk.</summary>
    public double CoreRadius { get; init; } = 2.0;

    /// <summary>Radius at which the probabilistic outer fade starts.</summary>
    public double EdgeFadeStart { get; init; } = 6.2;

    /// <summary>Relative angular and per-arm variation of the outer fade boundary.</summary>
    public double EdgeNoiseStrength { get; init; } = 0.12;

    /// <summary>Required empty gap between the visual surfaces of two stars.</summary>
    public double MinimumStarDistance { get; init; } = 0.006;

    /// <summary>Multiplier applied to minimum surface clearance at the center.</summary>
    public double InnerMinimumDistanceFactor { get; init; } = 0.7;

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

        if (!double.IsFinite(InnerDensityMultiplier) || InnerDensityMultiplier < 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(InnerDensityMultiplier), "Inner density multiplier must be finite and at least one.");
        }

        if (!double.IsFinite(InnerThicknessMultiplier) || InnerThicknessMultiplier < 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(InnerThicknessMultiplier), "Inner thickness multiplier must be finite and at least one.");
        }

        if (!double.IsFinite(InnerArmWidthMultiplier) || InnerArmWidthMultiplier < 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(InnerArmWidthMultiplier), "Inner arm width multiplier must be finite and at least one.");
        }

        if (!double.IsFinite(CoreRadius) || CoreRadius <= 0.0 || CoreRadius > Radius)
        {
            throw new ArgumentOutOfRangeException(nameof(CoreRadius), "Core radius must be finite, positive, and no greater than the galaxy radius.");
        }

        if (!double.IsFinite(EdgeFadeStart) || EdgeFadeStart < CoreRadius || EdgeFadeStart >= Radius)
        {
            throw new ArgumentOutOfRangeException(nameof(EdgeFadeStart), "Edge fade start must be finite, outside the core, and below the nominal radius.");
        }

        if (!double.IsFinite(EdgeNoiseStrength) || EdgeNoiseStrength is < 0.0 or > 0.35)
        {
            throw new ArgumentOutOfRangeException(nameof(EdgeNoiseStrength), "Edge noise strength must be finite and between zero and 0.35.");
        }

        if (InterArmDensityFactor + ArmDensityMultiplier <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(InterArmDensityFactor), "At least one density component must be positive.");
        }

        if (!double.IsFinite(MinimumStarDistance) || MinimumStarDistance < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(MinimumStarDistance), "Minimum star distance must be finite and non-negative.");
        }

        if (!double.IsFinite(InnerMinimumDistanceFactor) || InnerMinimumDistanceFactor is <= 0.0 or > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(InnerMinimumDistanceFactor), "Inner minimum-distance factor must be finite, positive, and no greater than one.");
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
