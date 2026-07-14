using System;
using Godot;
using GalaxyEngine3D.Camera;
using GalaxyEngine3D.Core;

namespace GalaxyEngine3D.Views;

public abstract partial class GameView : Node3D
{
    private MeshInstance3D? _selectedMesh;
    private Vector3 _selectedMeshScale;

    public abstract ViewId Id { get; }

    public abstract string DisplayName { get; }

    public virtual string? SelectableGroup => null;

    public Camera3D Camera => GetNode<Camera3D>("Camera3D");

    public string? SelectionName { get; private set; }

    public ViewSelectionTarget? SelectedTarget { get; private set; }

    public virtual bool CanAdvance => SelectedTarget is not null;

    public event Action<string?>? SelectionChanged;

    public virtual void Configure(ViewContext context)
    {
    }

    public override void _UnhandledInput(InputEvent inputEvent)
    {
        if (HandleCameraFocusInput(inputEvent))
        {
            return;
        }

        if (SelectableGroup is null ||
            inputEvent is not InputEventMouseButton mouseButton ||
            mouseButton.ButtonIndex != MouseButton.Left ||
            !mouseButton.Pressed)
        {
            return;
        }

        Vector3 rayOrigin = Camera.ProjectRayOrigin(mouseButton.Position);
        Vector3 rayEnd = rayOrigin + Camera.ProjectRayNormal(mouseButton.Position) * 1000.0f;
        PhysicsRayQueryParameters3D query = PhysicsRayQueryParameters3D.Create(rayOrigin, rayEnd);
        Godot.Collections.Dictionary result = GetWorld3D().DirectSpaceState.IntersectRay(query);

        if (result.Count == 0 || result["collider"].AsGodotObject() is not Node collider)
        {
            return;
        }

        if (!collider.IsInGroup(SelectableGroup))
        {
            return;
        }

        MeshInstance3D? mesh = collider.GetParent() as MeshInstance3D;
        if (mesh is null)
        {
            return;
        }

        Select(mesh);
        GetViewport().SetInputAsHandled();
    }

    private void Select(MeshInstance3D mesh)
    {
        if (_selectedMesh is not null && IsInstanceValid(_selectedMesh))
        {
            _selectedMesh.Scale = _selectedMeshScale;
        }

        _selectedMesh = mesh;
        _selectedMeshScale = mesh.Scale;
        mesh.Scale *= 1.2f;
        string displayName = mesh.Name.ToString().Replace('_', ' ');
        Aabb bounds = mesh.GetAabb();
        Vector3 scale = mesh.GlobalTransform.Basis.Scale.Abs();
        float maximumScale = Mathf.Max(scale.X, Mathf.Max(scale.Y, scale.Z));
        float coverRadius = Mathf.Max(0.05f, bounds.Size.Length() * maximumScale * 0.5f);
        SetSelectionTarget(
            mesh.Name.ToString(),
            displayName,
            () => IsInstanceValid(mesh) ? mesh.GlobalPosition : GlobalPosition,
            coverRadius);
    }

    protected bool HandleCameraFocusInput(InputEvent inputEvent)
    {
        if (inputEvent is not InputEventKey keyEvent ||
            !keyEvent.Pressed ||
            keyEvent.Echo ||
            Camera is not OrbitCameraController orbitCamera)
        {
            return false;
        }

        if (keyEvent.Keycode == Key.F && SelectedTarget is not null)
        {
            orbitCamera.AnimateFocusTo(SelectedTarget.Position);
            GetViewport().SetInputAsHandled();
            return true;
        }

        if (keyEvent.Keycode == Key.Home)
        {
            orbitCamera.AnimateFocusTo(GlobalPosition);
            GetViewport().SetInputAsHandled();
            return true;
        }

        return false;
    }

    protected void SetSelectionTarget(
        string objectId,
        string selectionName,
        Func<Vector3> positionProvider,
        float coverRadius)
    {
        SelectedTarget = new ViewSelectionTarget(objectId, selectionName, positionProvider, coverRadius);
        SelectionName = selectionName;
        SelectionChanged?.Invoke(selectionName);
    }
}

public sealed class ViewSelectionTarget
{
    private readonly Func<Vector3> _positionProvider;

    public ViewSelectionTarget(string objectId, string displayName, Func<Vector3> positionProvider, float coverRadius)
    {
        ObjectId = objectId;
        DisplayName = displayName;
        _positionProvider = positionProvider;
        CoverRadius = coverRadius;
    }

    public string ObjectId { get; }

    public string DisplayName { get; }

    public Vector3 Position => _positionProvider();

    public float CoverRadius { get; }
}
