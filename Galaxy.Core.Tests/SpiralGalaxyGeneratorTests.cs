using Galaxy.Core;
using Galaxy.Core.Generation;
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
        }
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
        }

        Assert.Equal(6_494_796_118_537_854_448UL, fingerprint);
    }

    [Fact]
    public void InvalidParametersAreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => (Parameters with { SpiralArmCount = 0 }).Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() => (Parameters with { Radius = 0.0 }).Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() => (Parameters with { DiskThickness = -0.1 }).Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() => (Parameters with { BulgeRadius = 9.0 }).Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() => (Parameters with { ArmTwistRadians = double.NaN }).Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() => (Parameters with { StarCount = 0 }).Validate());
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
