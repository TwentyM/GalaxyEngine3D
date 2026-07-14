using System;
using System.Threading.Tasks;
using Godot;
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

        GalaxyView galaxy = RequireView<GalaxyView>(router);
        SelectMesh(galaxy, galaxy.GetNode<MeshInstance3D>("Solara"));
        Assert(galaxy.SelectionName == "Solara", "The placeholder star was not selected.");
        Assert(!nextButton.Disabled, "The next button stayed disabled after selecting a star.");

        galaxy.Camera.Position += new Vector3(1.25f, 0.5f, 0.0f);
        Transform3D savedGalaxyCamera = galaxy.Camera.Transform;

        nextButton.EmitSignal(BaseButton.SignalName.Pressed);
        await WaitForTransitionAsync();

        SystemView system = RequireView<SystemView>(router);
        Assert(system.GetNode<Label3D>("ContextLabel").Text.Contains("Solara"),
            "The selected star context did not reach SystemView.");
        SelectMesh(system, system.GetNode<MeshInstance3D>("Pelagos"));
        Assert(system.SelectionName == "Pelagos", "The placeholder planet was not selected.");
        Assert(!nextButton.Disabled, "The next button stayed disabled after selecting a planet.");

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

        Assert(galaxy.Camera.Transform.IsEqualApprox(savedGalaxyCamera),
            "GalaxyView did not restore its saved camera transform.");

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

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
