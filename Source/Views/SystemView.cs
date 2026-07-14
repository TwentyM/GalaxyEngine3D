using Godot;
using GalaxyEngine3D.Core;

namespace GalaxyEngine3D.Views;

public partial class SystemView : GameView
{
    public override ViewId Id => ViewId.System;

    public override string DisplayName => "SystemView";

    public override string SelectableGroup => "selectable_planet";

    public override void Configure(ViewContext context)
    {
        Label3D label = GetNode<Label3D>("ContextLabel");
        label.Text = $"{context.SelectedStarName ?? "Ismeretlen csillag"} rendszere";
    }
}
