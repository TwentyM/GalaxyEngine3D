namespace Galaxy.Core;

public sealed class GeneratedGalaxy
{
    internal GeneratedGalaxy(
        GalaxySeed seed,
        int generatorVersion,
        GalaxyGenerationParameters parameters,
        GalaxyStar[] stars)
    {
        Seed = seed;
        GeneratorVersion = generatorVersion;
        Parameters = parameters;
        Stars = Array.AsReadOnly(stars);
    }

    public GalaxySeed Seed { get; }

    public int GeneratorVersion { get; }

    public GalaxyGenerationParameters Parameters { get; }

    public IReadOnlyList<GalaxyStar> Stars { get; }
}
