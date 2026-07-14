using Godot;
using GalaxyEngine3D.Camera;

namespace GalaxyEngine3D.Core;

public readonly record struct CameraState(
    Transform3D Transform,
    float FieldOfView,
    float Size,
    Camera3D.ProjectionType Projection,
    Vector3 LocalFocusPosition)
{
    public static CameraState Capture(Camera3D camera) => new(
        camera.Transform,
        camera.Fov,
        camera.Size,
        camera.Projection,
        camera is OrbitCameraController orbitCamera && camera.GetParent() is Node3D parent
            ? parent.ToLocal(orbitCamera.CameraFocusPosition)
            : Vector3.Zero);

    public void Restore(Camera3D camera)
    {
        camera.Transform = Transform;
        camera.Fov = FieldOfView;
        camera.Size = Size;
        camera.Projection = Projection;

        if (camera is OrbitCameraController orbitCamera)
        {
            orbitCamera.CameraFocusPosition = camera.GetParent() is Node3D parent
                ? parent.ToGlobal(LocalFocusPosition)
                : LocalFocusPosition;
            orbitCamera.SynchronizeFromTransform();
        }
    }
}
