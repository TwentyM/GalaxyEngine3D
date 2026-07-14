using Galaxy.Core;
using Galaxy.Core.Generation;
using System.Diagnostics;
using Xunit;

namespace Galaxy.Core.Tests;

public sealed class SpiralGalaxyGeneratorTests
{
    private static readonly GalaxyGenerationParameters Parameters = new()
    {
        SpiralArmCount = 4,
        Radius = 8.0,
        DiskThickness = 0.8,
        BulgeRadius = 2.0,
        ArmTwistRadians = 4.5,
        DiskScaleLength = 2.4,
        InterArmDensityFactor = 0.35,
        ArmDensityMultiplier = 1.5,
        BulgeDensityMultiplier = 1.8,
        MinimumStarDistance = 0.006,
        StarCount = 512,
    };

    [Fact]
    public void SameSeedProducesExactlySameStars()
    {
        SpiralGalaxyGenerator generator = new();

        GeneratedGalaxy first = generator.Generate(new GalaxySeed(0x0123456789ABCDEFUL), Parameters);
        GeneratedGalaxy second = generator.Generate(new GalaxySeed(0x0123456789ABCDEFUL), Parameters);

        Assert.Equal(first.GeneratorVersion, second.GeneratorVersion);
        Assert.Equal(first.Stars, second.Stars);
    }

    [Fact]
    public void IndividualQueriesAreIndependentOfTraversalOrder()
    {
        SpiralGalaxyGenerator generator = new();
        GalaxySeed seed = new(42UL);
        GeneratedGalaxy complete = generator.Generate(seed, Parameters);

        for (int index = Parameters.StarCount - 1; index >= 0; index--)
        {
            Assert.Equal(complete.Stars[index], generator.GenerateStar(seed, Parameters, index));
        }
    }

    [Fact]
    public void DifferentSeedsProduceDifferentStableIdsAndPositions()
    {
        SpiralGalaxyGenerator generator = new();

        GalaxyStar first = generator.GenerateStar(new GalaxySeed(1UL), Parameters, 17);
        GalaxyStar second = generator.GenerateStar(new GalaxySeed(2UL), Parameters, 17);

        Assert.NotEqual(first.Id, second.Id);
        Assert.NotEqual(first.Position, second.Position);
    }

    [Fact]
    public void GeneratedStarsStayInsideConfiguredBounds()
    {
        SpiralGalaxyGenerator generator = new();
        GeneratedGalaxy galaxy = generator.Generate(new GalaxySeed(987654321UL), Parameters);

        Assert.Equal(galaxy.Stars.Count, galaxy.Stars.Select(star => star.Id).Distinct().Count());

        foreach (GalaxyStar star in galaxy.Stars)
        {
            double planarRadius = Math.Sqrt(
                (star.Position.X * star.Position.X) +
                (star.Position.Z * star.Position.Z));

            Assert.InRange(planarRadius, 0.0, Parameters.Radius + 1e-12);
            Assert.InRange(Math.Abs(star.Position.Y), 0.0, Math.Max(Parameters.DiskThickness, Parameters.BulgeRadius * 0.65) + 1e-12);
            Assert.InRange(star.TemperatureKelvin, 2_500.0f, 12_000.0f);
            Assert.True(star.Luminosity > 0.0f);
            Assert.InRange(star.VisualRadius, 0.0275f, 0.045f);
        }
    }

    [Fact]
    public void StarsRespectConfiguredSurfaceClearance()
    {
        SpiralGalaxyGenerator generator = new();
        GeneratedGalaxy galaxy = generator.Generate(new GalaxySeed(0x5EEDUL), Parameters);

        for (int firstIndex = 0; firstIndex < galaxy.Stars.Count; firstIndex++)
        {
            GalaxyStar first = galaxy.Stars[firstIndex];
            for (int secondIndex = firstIndex + 1; secondIndex < galaxy.Stars.Count; secondIndex++)
            {
                GalaxyStar second = galaxy.Stars[secondIndex];
                double dx = first.Position.X - second.Position.X;
                double dy = first.Position.Y - second.Position.Y;
                double dz = first.Position.Z - second.Position.Z;
                double distance = Math.Sqrt((dx * dx) + (dy * dy) + (dz * dz));
                double required = Parameters.MinimumStarDistance + first.VisualRadius + second.VisualRadius;

                Assert.True(distance + 1e-12 >= required,
                    $"Stars {firstIndex} and {secondIndex} overlap: {distance} < {required}.");
            }
        }
    }

    [Fact]
    public void ExponentialDiskIncludesInterArmStars()
    {
        GalaxyGenerationParameters diskOnly = Parameters with
        {
            BulgeRadius = 0.0,
            BulgeDensityMultiplier = 0.0,
            InterArmDensityFactor = 1.0,
            ArmDensityMultiplier = 2.0,
            StarCount = 4_096,
        };
        GeneratedGalaxy galaxy = new SpiralGalaxyGenerator().Generate(new GalaxySeed(0xA11CEUL), diskOnly);
        int clearlyBetweenArms = 0;

        foreach (GalaxyStar star in galaxy.Stars)
        {
            double radius = Math.Sqrt((star.Position.X * star.Position.X) + (star.Position.Z * star.Position.Z));
            double angle = Math.Atan2(star.Position.Z, star.Position.X);
            double twist = diskOnly.ArmTwistRadians * (radius / diskOnly.Radius);
            double nearestArmDistance = double.PositiveInfinity;

            for (int arm = 0; arm < diskOnly.SpiralArmCount; arm++)
            {
                double armAngle = (Math.PI * 2.0 * arm / diskOnly.SpiralArmCount) + twist;
                nearestArmDistance = Math.Min(nearestArmDistance, Math.Abs(WrapAngle(angle - armAngle)));
            }

            if (nearestArmDistance > 0.6)
            {
                clearlyBetweenArms++;
            }
        }

        Assert.True(clearlyBetweenArms >= 80,
            $"Expected a sparse inter-arm population, found only {clearlyBetweenArms} stars.");
    }

    [Fact]
    public void FiftyThousandStarsGenerateWithinInteractiveBudget()
    {
        GalaxyGenerationParameters large = Parameters with { StarCount = 50_000 };
        Stopwatch stopwatch = Stopwatch.StartNew();
        GeneratedGalaxy galaxy = new SpiralGalaxyGenerator().Generate(new GalaxySeed(0xC0FFEEUL), large);
        stopwatch.Stop();

        Assert.Equal(50_000, galaxy.Stars.Count);
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(15),
            $"50,000-star generation took {stopwatch.Elapsed.TotalSeconds:F2} seconds.");
    }

    [Fact]
    public void ReferenceSeedHasStableFingerprint()
    {
        SpiralGalaxyGenerator generator = new();
        GeneratedGalaxy galaxy = generator.Generate(new GalaxySeed(0xC0FFEE1234567890UL), Parameters);
        ulong fingerprint = 14_695_981_039_346_656_037UL;

        foreach (GalaxyStar star in galaxy.Stars)
        {
            fingerprint = Fnv1A(fingerprint, star.Id.Value);
            fingerprint = Fnv1A(fingerprint, (ulong)BitConverter.DoubleToInt64Bits(star.Position.X));
            fingerprint = Fnv1A(fingerprint, (ulong)BitConverter.DoubleToInt64Bits(star.Position.Y));
            fingerprint = Fnv1A(fingerprint, (ulong)BitConverter.DoubleToInt64Bits(star.Position.Z));
            fingerprint = Fnv1A(fingerprint, BitConverter.SingleToUInt32Bits(star.TemperatureKelvin));
            fingerprint = Fnv1A(fingerprint, BitConverter.SingleToUInt32Bits(star.Luminosity));
            fingerprint = Fnv1A(fingerprint, BitConverter.SingleToUInt32Bits(star.VisualRadius));
        }

        Assert.Equal(6_885_551_133_730_373_599UL, fingerprint);
    }

    [Fact]
    public void InvalidParametersAreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => (Parameters with { SpiralArmCount = 0 }).Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() => (Parameters with { Radius = 0.0 }).Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() => (Parameters with { DiskThickness = -0.1 }).Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() => (Parameters with { BulgeRadius = 9.0 }).Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() => (Parameters with { ArmTwistRadians = double.NaN }).Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() => (Parameters with { DiskScaleLength = 0.0 }).Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() => (Parameters with { InterArmDensityFactor = -0.1 }).Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() => (Parameters with { ArmDensityMultiplier = -0.1 }).Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() => (Parameters with { BulgeDensityMultiplier = -0.1 }).Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() => (Parameters with
        {
            BulgeRadius = 0.0,
            InterArmDensityFactor = 0.0,
            ArmDensityMultiplier = 0.0,
            BulgeDensityMultiplier = 1.0,
        }).Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() => (Parameters with { MinimumStarDistance = -0.1 }).Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() => (Parameters with { MaxPlacementAttempts = 0 }).Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() => (Parameters with { StarCount = 0 }).Validate());
    }

    private static double WrapAngle(double angle)
    {
        double tau = Math.PI * 2.0;
        angle %= tau;
        if (angle > Math.PI)
        {
            angle -= tau;
        }
        else if (angle < -Math.PI)
        {
            angle += tau;
        }

        return angle;
    }

    private static ulong Fnv1A(ulong hash, ulong value)
    {
        for (int shift = 0; shift < 64; shift += 8)
        {
            hash ^= (byte)(value >> shift);
            hash *= 1_099_511_628_211UL;
        }

        return hash;
    }
}
