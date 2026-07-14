using System;
using System.Threading.Tasks;
using Godot;

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

    public async Task PlayAsync(Camera3D outgoingCamera, Action swapView)
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
            Tween zoomIn = CreateTween().SetParallel();
            zoomIn.SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Cubic);
            zoomIn.TweenProperty(outgoingCamera, "fov", 14.0f, HalfDuration);
            zoomIn.TweenProperty(_overlay, "color", opaque, HalfDuration);
            await ToSignal(zoomIn, Tween.SignalName.Finished);

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
