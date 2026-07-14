using Galaxy.Core;

namespace GalaxyEngine3D.Core;

public sealed class ViewContext
{
    public StarId? SelectedStarId { get; set; }

    public string? SelectedStarName { get; set; }

    public string? SelectedPlanetName { get; set; }
}
