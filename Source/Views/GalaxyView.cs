using GalaxyEngine3D.Core;

namespace GalaxyEngine3D.Views;

public partial class GalaxyView : GameView
{
    public override ViewId Id => ViewId.Galaxy;

    public override string DisplayName => "GalaxyView";

    public override string SelectableGroup => "selectable_star";
}
