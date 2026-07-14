using Godot;
using GalaxyEngine3D.Core;

namespace GalaxyEngine3D.Views;

public partial class PlanetView : GameView
{
    public override ViewId Id => ViewId.Planet;

    public override string DisplayName => "PlanetView";

    public override bool CanAdvance => false;

    public override void Configure(ViewContext context)
    {
        string planetName = context.SelectedPlanetName ?? "Ismeretlen bolygó";
        GetNode<Label3D>("ContextLabel").Text = planetName;

        if (GetNode<MeshInstance3D>("Planet").MaterialOverride is ShaderMaterial material)
        {
            material.SetShaderParameter("seed_offset", (float)(planetName.GetHashCode() & 0xffff) / 65535.0f);
        }
    }
}
