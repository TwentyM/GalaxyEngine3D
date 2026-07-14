using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Godot;
using GalaxyEngine3D.Camera;
using GalaxyEngine3D.Navigation;
using GalaxyEngine3D.Views;

namespace GalaxyEngine3D.Tests;

public partial class RuntimeSmokeTest : Node
{
    public override async void _Ready()
    {
        try
        {
            await RunAsync();
            GD.Print("RUNTIME_SMOKE_TEST: PASS");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError($"RUNTIME_SMOKE_TEST: FAIL\n{exception}");
            GetTree().Quit(1);
        }
    }

    private async Task RunAsync()
    {
        PackedScene mainScene = GD.Load<PackedScene>("res://Scenes/Main.tscn");
        Node main = mainScene.Instantiate();
        AddChild(main);
        await WaitForPhysicsAsync();

        ViewRouter router = main.GetNode<ViewRouter>("ViewRouter");
        Button nextButton = main.GetNode<Button>("Hud/Panel/Margin/VBox/Buttons/Next");
        Button backButton = main.GetNode<Button>("Hud/Panel/Margin/VBox/Buttons/Back");
        Label developerInfo = main.GetNode<Label>("Hud/DeveloperPanel/Margin/Info");

        GalaxyView galaxy = RequireView<GalaxyView>(router);
        Assert(galaxy.GeneratedStarCount == 50_000, "The generated galaxy does not contain 50,000 stars.");
        double measuredFrameRate = await MeasureFrameRateAsync(90);
        GD.Print($"RUNTIME_FRAME_RATE: {measuredFrameRate:F1} FPS");
        Assert(measuredFrameRate >= 50.0,
            $"The 50,000-star GalaxyView averaged only {measuredFrameRate:F1} FPS.");
        Assert(developerInfo.Text.Contains($"Seed: {galaxy.Seed.Value}"), "The developer panel does not show the galaxy seed.");
        Assert(developerInfo.Text.Contains("Spirálkarok: 4"), "The developer panel does not show the spiral arm count.");
        Assert(developerInfo.Text.Contains("FPS:"), "The developer panel does not show FPS.");
        Assert(nextButton.Disabled, "The next button was enabled without a valid star selection.");
        SelectGeneratedStar(galaxy, 0);
        Galaxy.Core.StarId selectedStarId = galaxy.SelectedStarId ??
            throw new InvalidOperationException("A generated star was not selected.");
        string selectedStarName = galaxy.SelectionName ?? throw new InvalidOperationException("The selected star has no display name.");
        ViewSelectionTarget selectedStarTarget = galaxy.SelectedTarget ??
            throw new InvalidOperationException("The selected star has no navigation target.");
        Assert(!nextButton.Disabled, "The next button stayed disabled after selecting a star.");
        Assert(router.SelectedObjectId == selectedStarId.Value.ToString("X16"),
            "ViewRouter did not receive the selected star ID.");
        Assert(router.SelectedObjectPosition is Vector3 selectedStarPosition &&
            selectedStarPosition.IsEqualApprox(selectedStarTarget.Position),
            "ViewRouter did not receive the selected star position.");

        OrbitCameraController galaxyCamera = (OrbitCameraController)galaxy.Camera;
        galaxy._UnhandledInput(new InputEventKey { Keycode = Key.F, Pressed = true });
        Assert(galaxyCamera.CameraFocusPosition.IsEqualApprox(selectedStarTarget.Position),
            "F did not focus the selected star.");
        galaxy._UnhandledInput(new InputEventKey { Keycode = Key.Home, Pressed = true });
        Assert(galaxyCamera.CameraFocusPosition.IsEqualApprox(galaxy.GlobalPosition),
            "Home did not restore the view center focus.");

        float distanceBeforeZoom = galaxyCamera.Distance;
        galaxyCamera._UnhandledInput(new InputEventMouseButton
        {
            ButtonIndex = MouseButton.WheelUp,
            Pressed = true,
        });
        Assert(galaxyCamera.Distance < distanceBeforeZoom,
            "Wheel zoom did not reduce distance exponentially.");

        Vector3 focusBeforePan = galaxyCamera.CameraFocusPosition;
        galaxyCamera._UnhandledInput(new InputEventMouseButton
        {
            ButtonIndex = MouseButton.Middle,
            Pressed = true,
        });
        galaxyCamera._UnhandledInput(new InputEventMouseMotion { Relative = new Vector2(16.0f, -8.0f) });
        galaxyCamera._UnhandledInput(new InputEventMouseButton
        {
            ButtonIndex = MouseButton.Middle,
            Pressed = false,
        });
        Assert(!galaxyCamera.CameraFocusPosition.IsEqualApprox(focusBeforePan),
            "Middle mouse drag did not move the focal point in the camera plane.");

        galaxyCamera.FocusOn(new Vector3(0.8f, 0.2f, -0.4f));
        Transform3D savedGalaxyCamera = galaxy.Camera.Transform;
        Vector3 savedGalaxyFocus = galaxyCamera.CameraFocusPosition;

        nextButton.EmitSignal(BaseButton.SignalName.Pressed);
        await WaitForTransitionAsync();

        SystemView system = RequireView<SystemView>(router);
        Assert(router.Context.SelectedStarId == selectedStarId,
            "The stable star ID did not reach the SystemView context.");
        Assert(system.GetNode<Label3D>("ContextLabel").Text.Contains(selectedStarName),
            "The selected star context did not reach SystemView.");
        SelectMesh(system, system.GetNode<MeshInstance3D>("Pelagos"));
        Assert(system.SelectionName == "Pelagos", "The placeholder planet was not selected.");
        Assert(!nextButton.Disabled, "The next button stayed disabled after selecting a planet.");
        Assert(router.SelectedObjectId == "Pelagos",
            "ViewRouter did not receive the selected planet ID.");
        Assert(router.SelectedObjectPosition is Vector3 selectedPlanetPosition &&
            selectedPlanetPosition.IsEqualApprox(system.GetNode<MeshInstance3D>("Pelagos").GlobalPosition),
            "ViewRouter did not receive the selected planet position.");

        nextButton.EmitSignal(BaseButton.SignalName.Pressed);
        await WaitForTransitionAsync();

        PlanetView planet = RequireView<PlanetView>(router);
        Assert(planet.GetNode<Label3D>("ContextLabel").Text == "Pelagos",
            "The selected planet context did not reach PlanetView.");

        backButton.EmitSignal(BaseButton.SignalName.Pressed);
        await WaitForTransitionAsync();
        RequireView<SystemView>(router);

        backButton.EmitSignal(BaseButton.SignalName.Pressed);
        await WaitForTransitionAsync();
        galaxy = RequireView<GalaxyView>(router);

        Assert(galaxy.SelectedStarId == selectedStarId,
            "GalaxyView did not restore the selected generated star by stable ID.");
        Assert(galaxy.Camera.Transform.IsEqualApprox(savedGalaxyCamera),
            "GalaxyView did not restore its saved camera transform.");
        Assert(((OrbitCameraController)galaxy.Camera).CameraFocusPosition.IsEqualApprox(savedGalaxyFocus),
            "GalaxyView did not restore its saved focal point.");

        main.QueueFree();
    }

    private static T RequireView<T>(ViewRouter router) where T : GameView
    {
        if (router.CurrentView is not T view)
        {
            throw new InvalidOperationException(
                $"Expected {typeof(T).Name}, got {router.CurrentView?.GetType().Name ?? "null"}.");
        }

        return view;
    }

    private static void SelectMesh(GameView view, MeshInstance3D mesh)
    {
        Vector2 screenPosition = view.Camera.UnprojectPosition(mesh.GlobalPosition);
        view._UnhandledInput(new InputEventMouseButton
        {
            ButtonIndex = MouseButton.Left,
            Position = screenPosition,
            Pressed = true,
        });
    }

    private static void SelectGeneratedStar(GalaxyView view, int stableIndex)
    {
        Vector2 screenPosition = view.Camera.UnprojectPosition(view.GetStarPosition(stableIndex));
        view._UnhandledInput(new InputEventMouseButton
        {
            ButtonIndex = MouseButton.Left,
            Position = screenPosition,
            Pressed = true,
        });
    }

    private async Task WaitForPhysicsAsync()
    {
        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    private async Task WaitForTransitionAsync()
    {
        await ToSignal(GetTree().CreateTimer(0.9), SceneTreeTimer.SignalName.Timeout);
        await WaitForPhysicsAsync();
    }

    private async Task<double> MeasureFrameRateAsync(int frameCount)
    {
        for (int warmup = 0; warmup < 10; warmup++)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }

        Stopwatch stopwatch = Stopwatch.StartNew();
        for (int frame = 0; frame < frameCount; frame++)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }

        stopwatch.Stop();
        return frameCount / stopwatch.Elapsed.TotalSeconds;
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
