namespace Galaxy.Core.Generation;

public sealed class SpiralGalaxyGenerator
{
    private const ulong StarRole = 0x535441525F563031UL; // "STAR_V01"
    private const double Tau = Math.PI * 2.0;

    public const int Version = 1;

    public GeneratedGalaxy Generate(GalaxySeed seed, GalaxyGenerationParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        parameters.Validate();

        GalaxyStar[] stars = new GalaxyStar[parameters.StarCount];
        for (int index = 0; index < stars.Length; index++)
        {
            stars[index] = GenerateStar(seed, parameters, index);
        }

        return new GeneratedGalaxy(seed, Version, parameters, stars);
    }

    public GalaxyStar GenerateStar(
        GalaxySeed seed,
        GalaxyGenerationParameters parameters,
        int stableIndex)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        parameters.Validate();

        if ((uint)stableIndex >= (uint)parameters.StarCount)
        {
            throw new ArgumentOutOfRangeException(nameof(stableIndex));
        }

        ulong starSeed = DeterministicHash.Derive(
            seed.Value,
            StarRole ^ (uint)Version,
            (uint)stableIndex);
        StarId id = new(DeterministicHash.Derive(starSeed, StarRole, (uint)stableIndex));

        double bulgeFraction = Math.Clamp(
            (parameters.BulgeRadius / parameters.Radius) * 0.55,
            0.0,
            0.35);
        GalaxyVector3 position = Random(starSeed, 0) < bulgeFraction
            ? GenerateBulgePosition(starSeed, parameters)
            : GenerateDiskPosition(starSeed, parameters, stableIndex);

        double heatBias = Random(starSeed, 8);
        float temperature = (float)(2_500.0 + (9_500.0 * heatBias * heatBias));
        float luminosity = (float)(0.35 + (3.65 * Random(starSeed, 9) * Math.Pow(temperature / 12_000.0, 1.5)));

        return new GalaxyStar(id, stableIndex, position, temperature, luminosity);
    }

    private static GalaxyVector3 GenerateDiskPosition(
        ulong starSeed,
        GalaxyGenerationParameters parameters,
        int stableIndex)
    {
        double normalizedRadius = Math.Sqrt(Random(starSeed, 1));
        double radialNoise = Triangular(starSeed, 2) * parameters.Radius * 0.035;
        double radius = Math.Clamp(
            (normalizedRadius * parameters.Radius) + radialNoise,
            0.0,
            parameters.Radius);

        int armIndex = stableIndex % parameters.SpiralArmCount;
        double armPhase = Tau * armIndex / parameters.SpiralArmCount;
        double armNoise = Triangular(starSeed, 4) * (0.18 + (0.34 * normalizedRadius));
        double angle = armPhase + (parameters.ArmTwistRadians * normalizedRadius) + armNoise;
        double verticalTaper = 0.35 + (0.65 * (1.0 - normalizedRadius));
        double y = Triangular(starSeed, 6) * parameters.DiskThickness * verticalTaper;

        return new GalaxyVector3(
            radius * Math.Cos(angle),
            y,
            radius * Math.Sin(angle));
    }

    private static GalaxyVector3 GenerateBulgePosition(
        ulong starSeed,
        GalaxyGenerationParameters parameters)
    {
        double radius = parameters.BulgeRadius * Math.Cbrt(Random(starSeed, 1));
        double azimuth = Tau * Random(starSeed, 2);
        double vertical = (Random(starSeed, 3) * 2.0) - 1.0;
        double planar = Math.Sqrt(Math.Max(0.0, 1.0 - (vertical * vertical)));

        return new GalaxyVector3(
            radius * planar * Math.Cos(azimuth),
            radius * vertical * 0.65,
            radius * planar * Math.Sin(azimuth));
    }

    private static double Random(ulong starSeed, ulong stream) =>
        DeterministicHash.UnitDouble(starSeed, stream);

    private static double Triangular(ulong starSeed, ulong stream) =>
        Random(starSeed, stream) + Random(starSeed, stream + 1) - 1.0;
}
