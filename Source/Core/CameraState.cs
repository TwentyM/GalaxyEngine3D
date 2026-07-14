using Godot;

namespace GalaxyEngine3D.Core;

public readonly record struct CameraState(
    Transform3D Transform,
    float FieldOfView,
    float Size,
    Camera3D.ProjectionType Projection)
{
    public static CameraState Capture(Camera3D camera) => new(
        camera.Transform,
        camera.Fov,
        camera.Size,
        camera.Projection);

    public void Restore(Camera3D camera)
    {
        camera.Transform = Transform;
        camera.Fov = FieldOfView;
        camera.Size = Size;
        camera.Projection = Projection;
    }
}
