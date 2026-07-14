namespace Galaxy.Core.Generation;

public sealed class SpiralGalaxyGenerator
{
    private const ulong StarRole = 0x535441525F563032UL; // "STAR_V02"
    private const ulong PlacementRole = 0x504C4143455F5632UL; // "PLACE_V2"
    private const double Tau = Math.PI * 2.0;
    private const double ArmWidthRadians = 0.32;
    private const double MinimumVisualRadius = 0.0275;
    private const double MaximumVisualRadius = 0.045;

    public const int Version = 2;

    public GeneratedGalaxy Generate(GalaxySeed seed, GalaxyGenerationParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        parameters.Validate();

        GalaxyStar[] stars = GenerateStars(seed, parameters, parameters.StarCount);

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

        return GenerateStars(seed, parameters, stableIndex + 1)[stableIndex];
    }

    private static GalaxyStar[] GenerateStars(
        GalaxySeed seed,
        GalaxyGenerationParameters parameters,
        int count)
    {
        GalaxyStar[] stars = new GalaxyStar[count];
        double cellSize = parameters.MinimumStarDistance + ((double)(float)MaximumVisualRadius * 2.0);
        Dictionary<CellKey, List<int>> spatialGrid = new(count);

        for (int stableIndex = 0; stableIndex < count; stableIndex++)
        {
            ulong starSeed = DeterministicHash.Derive(
                seed.Value,
                StarRole ^ (uint)Version,
                (uint)stableIndex);
            StarId id = new(DeterministicHash.Derive(starSeed, StarRole, (uint)stableIndex));

            double heatBias = Random(starSeed, 100);
            float temperature = (float)(2_500.0 + (9_500.0 * heatBias * heatBias));
            float luminosity = (float)(0.35 + (3.65 * Random(starSeed, 101) * Math.Pow(temperature / 12_000.0, 1.5)));
            float visualRadius = (float)(MinimumVisualRadius +
                ((MaximumVisualRadius - MinimumVisualRadius) * Math.Clamp(luminosity / 4.0, 0.0, 1.0)));

            bool placed = false;
            for (int attempt = 0; attempt < parameters.MaxPlacementAttempts; attempt++)
            {
                ulong candidateSeed = DeterministicHash.Derive(starSeed, PlacementRole, (uint)attempt);
                if (!TryGeneratePosition(candidateSeed, parameters, out GalaxyVector3 position) ||
                    !HasClearance(position, visualRadius, parameters.MinimumStarDistance, stars, spatialGrid, cellSize))
                {
                    continue;
                }

                GalaxyStar star = new(id, stableIndex, position, temperature, luminosity, visualRadius);
                stars[stableIndex] = star;
                CellKey cell = CellKey.From(position, cellSize);
                if (!spatialGrid.TryGetValue(cell, out List<int>? occupants))
                {
                    occupants = new List<int>(1);
                    spatialGrid.Add(cell, occupants);
                }

                occupants.Add(stableIndex);
                placed = true;
                break;
            }

            if (!placed)
            {
                throw new InvalidOperationException(
                    $"Could not place star {stableIndex} after {parameters.MaxPlacementAttempts} attempts. " +
                    "Reduce MinimumStarDistance or density, increase the galaxy volume, or increase MaxPlacementAttempts.");
            }
        }

        return stars;
    }

    private static bool TryGeneratePosition(
        ulong candidateSeed,
        GalaxyGenerationParameters parameters,
        out GalaxyVector3 position)
    {
        double diskWeight = parameters.InterArmDensityFactor;
        double armWeight = parameters.ArmDensityMultiplier;
        double bulgeWeight = parameters.BulgeRadius > 0.0
            ? parameters.BulgeDensityMultiplier
            : 0.0;
        double component = Random(candidateSeed, 0) * (diskWeight + armWeight + bulgeWeight);

        if (component >= diskWeight + armWeight)
        {
            position = GenerateBulgePosition(candidateSeed, parameters);
            return true;
        }

        double radialSample = Math.Max(double.Epsilon, Random(candidateSeed, 1) * Random(candidateSeed, 2));
        double radius = -parameters.DiskScaleLength * Math.Log(radialSample);
        if (radius > parameters.Radius)
        {
            position = default;
            return false;
        }

        double normalizedRadius = radius / parameters.Radius;
        double angle;
        if (component < diskWeight)
        {
            angle = Tau * Random(candidateSeed, 3);
        }
        else
        {
            int armIndex = Math.Min(
                parameters.SpiralArmCount - 1,
                (int)(Random(candidateSeed, 3) * parameters.SpiralArmCount));
            double armPhase = Tau * armIndex / parameters.SpiralArmCount;
            double gaussianNoise = Gaussian(candidateSeed, 4) * ArmWidthRadians * (0.55 + (0.45 * normalizedRadius));
            angle = armPhase + (parameters.ArmTwistRadians * normalizedRadius) + gaussianNoise;
        }

        double verticalTaper = 0.35 + (0.65 * (1.0 - normalizedRadius));
        double y = Triangular(candidateSeed, 6) * parameters.DiskThickness * verticalTaper;
        position = new GalaxyVector3(radius * Math.Cos(angle), y, radius * Math.Sin(angle));
        return true;
    }

    private static GalaxyVector3 GenerateBulgePosition(ulong seed, GalaxyGenerationParameters parameters)
    {
        double radius = parameters.BulgeRadius * Math.Cbrt(Random(seed, 1));
        double azimuth = Tau * Random(seed, 2);
        double vertical = (Random(seed, 3) * 2.0) - 1.0;
        double planar = Math.Sqrt(Math.Max(0.0, 1.0 - (vertical * vertical)));

        return new GalaxyVector3(
            radius * planar * Math.Cos(azimuth),
            radius * vertical * 0.65,
            radius * planar * Math.Sin(azimuth));
    }

    private static bool HasClearance(
        GalaxyVector3 position,
        float visualRadius,
        double minimumDistance,
        GalaxyStar[] stars,
        Dictionary<CellKey, List<int>> spatialGrid,
        double cellSize)
    {
        CellKey center = CellKey.From(position, cellSize);
        for (int x = center.X - 1; x <= center.X + 1; x++)
        {
            for (int y = center.Y - 1; y <= center.Y + 1; y++)
            {
                for (int z = center.Z - 1; z <= center.Z + 1; z++)
                {
                    if (!spatialGrid.TryGetValue(new CellKey(x, y, z), out List<int>? occupants))
                    {
                        continue;
                    }

                    foreach (int index in occupants)
                    {
                        GalaxyStar other = stars[index];
                        double requiredDistance = minimumDistance + visualRadius + other.VisualRadius;
                        double dx = position.X - other.Position.X;
                        double dy = position.Y - other.Position.Y;
                        double dz = position.Z - other.Position.Z;
                        if ((dx * dx) + (dy * dy) + (dz * dz) < requiredDistance * requiredDistance)
                        {
                            return false;
                        }
                    }
                }
            }
        }

        return true;
    }

    private static double Gaussian(ulong seed, ulong stream)
    {
        double u1 = Math.Max(double.Epsilon, Random(seed, stream));
        double u2 = Random(seed, stream + 1);
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(Tau * u2);
    }

    private static double Random(ulong starSeed, ulong stream) =>
        DeterministicHash.UnitDouble(starSeed, stream);

    private static double Triangular(ulong starSeed, ulong stream) =>
        Random(starSeed, stream) + Random(starSeed, stream + 1) - 1.0;

    private readonly record struct CellKey(int X, int Y, int Z)
    {
        public static CellKey From(GalaxyVector3 position, double cellSize) => new(
            (int)Math.Floor(position.X / cellSize),
            (int)Math.Floor(position.Y / cellSize),
            (int)Math.Floor(position.Z / cellSize));
    }
}
