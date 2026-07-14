using System;
using Godot;
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

    public virtual bool CanAdvance => SelectableGroup is not null && SelectionName is not null;

    public event Action<string?>? SelectionChanged;

    public virtual void Configure(ViewContext context)
    {
    }

    public override void _UnhandledInput(InputEvent inputEvent)
    {
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
        SetSelectionName(mesh.Name.ToString().Replace('_', ' '));
    }

    protected void SetSelectionName(string? selectionName)
    {
        SelectionName = selectionName;
        SelectionChanged?.Invoke(selectionName);
    }
}
