using System;
using System.Collections.Generic;
using Godot;
using GalaxyEngine3D.Camera;
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

    public void SwitchTo(ViewId target)
    {
        PackedScene scene = GetScene(target);

        if (CurrentView is not null)
        {
            RemoveChild(CurrentView);
            CurrentView.Free();
        }

        GameView nextView = scene.Instantiate<GameView>();
        nextView.Configure(Context);
        AddChild(nextView);
        CurrentView = nextView;

        if (_cameraStates.TryGetValue(target, out CameraState cameraState))
        {
            cameraState.Restore(nextView.Camera);
            if (nextView.Camera is OrbitCameraController orbitCamera)
            {
                orbitCamera.SynchronizeFromTransform();
            }
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
