using System;
using System.Collections.Generic;
using Godot;
using GalaxyEngine3D.Core;
using GalaxyEngine3D.Views;

namespace GalaxyEngine3D.Navigation;

public partial class ViewRouter : Node3D
{
    [Export] public PackedScene? GalaxyScene { get; set; }
    [Export] public PackedScene? SystemScene { get; set; }
    [Export] public PackedScene? PlanetScene { get; set; }

    private readonly Dictionary<ViewId, CameraState> _cameraStates = new();

    public GameView? CurrentView { get; private set; }

    public ViewContext Context { get; } = new();

    public ViewSelectionTarget? NavigationTarget { get; private set; }

    public string? SelectedObjectId => NavigationTarget?.ObjectId;

    public Vector3? SelectedObjectPosition => NavigationTarget?.Position;

    public event Action<GameView>? CurrentViewChanged;

    public void Initialize()
    {
        SwitchTo(ViewId.Galaxy);
    }

    public void CaptureCurrentCameraState()
    {
        if (CurrentView is not null)
        {
            _cameraStates[CurrentView.Id] = CameraState.Capture(CurrentView.Camera);
        }
    }

    public void ReceiveSelectedObject(ViewSelectionTarget? target)
    {
        NavigationTarget = target;
    }

    public void SwitchTo(ViewId target)
    {
        PackedScene scene = GetScene(target);

        if (CurrentView is not null)
        {
            RemoveChild(CurrentView);
            CurrentView.Free();
        }

        NavigationTarget = null;

        GameView nextView = scene.Instantiate<GameView>();
        nextView.Configure(Context);
        AddChild(nextView);
        CurrentView = nextView;

        if (_cameraStates.TryGetValue(target, out CameraState cameraState))
        {
            cameraState.Restore(nextView.Camera);
        }

        CurrentViewChanged?.Invoke(nextView);
    }

    private PackedScene GetScene(ViewId target) => target switch
    {
        ViewId.Galaxy => GalaxyScene,
        ViewId.System => SystemScene,
        ViewId.Planet => PlanetScene,
        _ => null,
    } ?? throw new InvalidOperationException($"No scene is configured for {target}.");
}
