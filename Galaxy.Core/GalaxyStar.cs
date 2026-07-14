namespace Galaxy.Core;

public readonly record struct GalaxyStar(
    StarId Id,
    int StableIndex,
    GalaxyVector3 Position,
    float TemperatureKelvin,
    float Luminosity);
