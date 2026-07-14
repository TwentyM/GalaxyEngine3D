using System;
using System.Threading.Tasks;
using Godot;
using GalaxyEngine3D.Camera;
using GalaxyEngine3D.Views;

namespace GalaxyEngine3D.Navigation;

public partial class TransitionController : CanvasLayer
{
    [Export(PropertyHint.Range, "0.1,2.0,0.05")] public float HalfDuration { get; set; } = 0.35f;

    private ColorRect _overlay = null!;

    public bool IsRunning { get; private set; }

    public override void _Ready()
    {
        _overlay = GetNode<ColorRect>("FlashOverlay");
        _overlay.Color = new Color(1.0f, 0.96f, 0.82f, 0.0f);
    }

    public async Task PlayAsync(
        Camera3D outgoingCamera,
        ViewSelectionTarget? selectedTarget,
        Action swapView)
    {
        if (IsRunning)
        {
            return;
        }

        IsRunning = true;
        Color transparent = new(1.0f, 0.96f, 0.82f, 0.0f);
        Color opaque = new(1.0f, 0.96f, 0.82f, 1.0f);

        try
        {
            Tween cover = CreateTween().SetParallel();
            cover.SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Cubic);
            cover.TweenProperty(_overlay, "color", opaque, HalfDuration);

            if (selectedTarget is not null && outgoingCamera is OrbitCameraController orbitCamera)
            {
                Vector3 startFocus = orbitCamera.CameraFocusPosition;
                float startDistance = orbitCamera.Distance;
                float targetDistance = Mathf.Max(
                    selectedTarget.CoverRadius * 1.05f,
                    outgoingCamera.Near * 1.1f);
                cover.TweenMethod(
                    Callable.From<float>(progress => orbitCamera.ApplyTransitionPose(
                        startFocus,
                        selectedTarget.Position,
                        startDistance,
                        targetDistance,
                        progress)),
                    0.0f,
                    1.0f,
                    HalfDuration);
            }
            else
            {
                cover.TweenProperty(outgoingCamera, "fov", 14.0f, HalfDuration);
            }

            await ToSignal(cover, Tween.SignalName.Finished);
            _overlay.Color = opaque;
            swapView();

            Tween reveal = CreateTween();
            reveal.SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
            reveal.TweenProperty(_overlay, "color", transparent, HalfDuration);
            await ToSignal(reveal, Tween.SignalName.Finished);
        }
        finally
        {
            _overlay.Color = transparent;
            IsRunning = false;
        }
    }
}
