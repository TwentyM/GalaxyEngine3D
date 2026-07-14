namespace Galaxy.Core.Generation;

public sealed class SpiralGalaxyGenerator
{
    private const ulong StarRole = 0x535441525F563033UL; // "STAR_V03"
    private const ulong PlacementRole = 0x504C4143455F5633UL; // "PLACE_V3"
    private const ulong EdgeRole = 0x454447455F563033UL; // "EDGE_V03"
    private const double Tau = Math.PI * 2.0;
    private const double BaseArmWidthRadians = 0.30;
    private const double MinimumVisualRadius = 0.0275;
    private const double MaximumVisualRadius = 0.045;

    public const int Version = 3;

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
        ulong edgeSeed = DeterministicHash.Derive(seed.Value, EdgeRole, (uint)Version);

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
                if (!TryGeneratePosition(candidateSeed, edgeSeed, parameters, out GalaxyVector3 position) ||
                    !HasClearance(position, visualRadius, parameters, stars, spatialGrid, cellSize))
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
        ulong edgeSeed,
        GalaxyGenerationParameters parameters,
        out GalaxyVector3 position)
    {
        double radialSample = Math.Max(double.Epsilon, Random(candidateSeed, 1) * Random(candidateSeed, 2));
        double radius = -parameters.DiskScaleLength * Math.Log(radialSample);
        double normalizedRadius = Math.Min(1.0, radius / parameters.Radius);
        double coreInfluence = CoreInfluence(radius, parameters.CoreRadius);

        double innerDensity = 1.0 + ((parameters.InnerDensityMultiplier - 1.0) * coreInfluence);
        if (Random(candidateSeed, 10) > innerDensity / parameters.InnerDensityMultiplier)
        {
            position = default;
            return false;
        }

        double totalWeight = parameters.InterArmDensityFactor + parameters.ArmDensityMultiplier;
        bool armComponent = Random(candidateSeed, 0) * totalWeight >= parameters.InterArmDensityFactor;
        int armIndex;
        double angle;
        if (armComponent)
        {
            armIndex = Math.Min(
                parameters.SpiralArmCount - 1,
                (int)(Random(candidateSeed, 3) * parameters.SpiralArmCount));
            double armPhase = Tau * armIndex / parameters.SpiralArmCount;
            double armWidth = BaseArmWidthRadians *
                (1.0 + ((parameters.InnerArmWidthMultiplier - 1.0) * coreInfluence));
            angle = armPhase +
                (parameters.ArmTwistRadians * normalizedRadius) +
                (Gaussian(candidateSeed, 4) * armWidth);
        }
        else
        {
            angle = Tau * Random(candidateSeed, 3);
            armIndex = FindNearestArm(angle, normalizedRadius, parameters);
        }

        if (!PassesEdgeFade(candidateSeed, edgeSeed, radius, angle, armIndex, parameters))
        {
            position = default;
            return false;
        }

        double thicknessMultiplier = 1.0 +
            ((parameters.InnerThicknessMultiplier - 1.0) * coreInfluence);
        double y = Triangular(candidateSeed, 6) * parameters.DiskThickness * thicknessMultiplier;
        position = new GalaxyVector3(radius * Math.Cos(angle), y, radius * Math.Sin(angle));
        return true;
    }

    private static bool PassesEdgeFade(
        ulong candidateSeed,
        ulong edgeSeed,
        double radius,
        double angle,
        int armIndex,
        GalaxyGenerationParameters parameters)
    {
        if (radius <= parameters.EdgeFadeStart)
        {
            return true;
        }

        double armVariation =
            (DeterministicHash.UnitDouble(edgeSeed, (ulong)armIndex + 20UL) * 2.0) - 1.0;
        double phaseA = DeterministicHash.UnitDouble(edgeSeed, 1) * Tau;
        double phaseB = DeterministicHash.UnitDouble(edgeSeed, 2) * Tau;
        double angularNoise =
            (Math.Sin((angle * 3.0) + phaseA) * 0.65) +
            (Math.Sin((angle * 7.0) + phaseB) * 0.35);
        double edgeScale = 1.0 + (parameters.EdgeNoiseStrength *
            ((armVariation * 0.5) + angularNoise));
        double edgeRadius = Math.Max(
            parameters.EdgeFadeStart + 1e-6,
            parameters.Radius * edgeScale);

        if (radius >= edgeRadius)
        {
            return false;
        }

        double fade = 1.0 - SmoothStep(
            parameters.EdgeFadeStart,
            edgeRadius,
            radius);
        return Random(candidateSeed, 11) < fade;
    }

    private static int FindNearestArm(
        double angle,
        double normalizedRadius,
        GalaxyGenerationParameters parameters)
    {
        double untwistedAngle = WrapAngle(angle - (parameters.ArmTwistRadians * normalizedRadius));
        double armPosition = untwistedAngle / Tau * parameters.SpiralArmCount;
        int nearest = (int)Math.Round(armPosition, MidpointRounding.AwayFromZero);
        nearest %= parameters.SpiralArmCount;
        return nearest < 0 ? nearest + parameters.SpiralArmCount : nearest;
    }

    private static bool HasClearance(
        GalaxyVector3 position,
        float visualRadius,
        GalaxyGenerationParameters parameters,
        GalaxyStar[] stars,
        Dictionary<CellKey, List<int>> spatialGrid,
        double cellSize)
    {
        CellKey center = CellKey.From(position, cellSize);
        double candidateClearance = LocalClearanceFactor(position, parameters);
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
                        double localFactor = (candidateClearance + LocalClearanceFactor(other.Position, parameters)) * 0.5;
                        double requiredDistance =
                            (parameters.MinimumStarDistance * localFactor) + visualRadius + other.VisualRadius;
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

    private static double LocalClearanceFactor(
        GalaxyVector3 position,
        GalaxyGenerationParameters parameters)
    {
        double radius = Math.Sqrt((position.X * position.X) + (position.Z * position.Z));
        double coreInfluence = CoreInfluence(radius, parameters.CoreRadius);
        return 1.0 - ((1.0 - parameters.InnerMinimumDistanceFactor) * coreInfluence);
    }

    private static double CoreInfluence(double radius, double coreRadius) =>
        1.0 - SmoothStep(0.0, coreRadius, radius);

    private static double SmoothStep(double minimum, double maximum, double value)
    {
        double amount = Math.Clamp((value - minimum) / (maximum - minimum), 0.0, 1.0);
        return amount * amount * (3.0 - (2.0 * amount));
    }

    private static double WrapAngle(double angle)
    {
        angle %= Tau;
        if (angle > Math.PI)
        {
            angle -= Tau;
        }
        else if (angle < -Math.PI)
        {
            angle += Tau;
        }

        return angle;
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
