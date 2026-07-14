using Godot;

namespace GalaxyEngine3D.Camera;

public partial class OrbitCameraController : Camera3D
{
    [Export] public Vector3 Target { get; set; } = Vector3.Zero;
    [Export(PropertyHint.Range, "0.001,0.05,0.001")] public float OrbitSensitivity { get; set; } = 0.008f;
    [Export(PropertyHint.Range, "0.1,10.0,0.1")] public float ZoomStep { get; set; } = 1.5f;
    [Export] public float MinimumDistance { get; set; } = 2.5f;
    [Export] public float MaximumDistance { get; set; } = 80.0f;

    private bool _isOrbiting;
    private float _distance;
    private float _yaw;
    private float _pitch;

    public override void _Ready()
    {
        LookAt(Target, Vector3.Up);
        SynchronizeFromTransform();
    }

    public override void _ExitTree()
    {
        if (_isOrbiting)
        {
            Input.MouseMode = Input.MouseModeEnum.Visible;
        }
    }

    public override void _UnhandledInput(InputEvent inputEvent)
    {
        if (inputEvent is InputEventMouseButton mouseButton)
        {
            HandleMouseButton(mouseButton);
            return;
        }

        if (inputEvent is InputEventMouseMotion mouseMotion && _isOrbiting)
        {
            _yaw -= mouseMotion.Relative.X * OrbitSensitivity;
            _pitch = Mathf.Clamp(
                _pitch - mouseMotion.Relative.Y * OrbitSensitivity,
                Mathf.DegToRad(-85.0f),
                Mathf.DegToRad(85.0f));
            ApplyOrbit();
            GetViewport().SetInputAsHandled();
        }
    }

    public void SynchronizeFromTransform()
    {
        Vector3 offset = GlobalPosition - Target;
        _distance = Mathf.Clamp(offset.Length(), MinimumDistance, MaximumDistance);

        if (_distance <= Mathf.Epsilon)
        {
            _distance = MinimumDistance;
            _yaw = 0.0f;
            _pitch = 0.0f;
            ApplyOrbit();
            return;
        }

        _pitch = Mathf.Asin(Mathf.Clamp(offset.Y / _distance, -1.0f, 1.0f));
        _yaw = Mathf.Atan2(offset.X, offset.Z);
    }

    private void HandleMouseButton(InputEventMouseButton mouseButton)
    {
        if (mouseButton.ButtonIndex == MouseButton.Right)
        {
            _isOrbiting = mouseButton.Pressed;
            Input.MouseMode = _isOrbiting
                ? Input.MouseModeEnum.Captured
                : Input.MouseModeEnum.Visible;
            GetViewport().SetInputAsHandled();
            return;
        }

        if (!mouseButton.Pressed)
        {
            return;
        }

        if (mouseButton.ButtonIndex == MouseButton.WheelUp)
        {
            _distance = Mathf.Max(MinimumDistance, _distance - ZoomStep);
            ApplyOrbit();
            GetViewport().SetInputAsHandled();
        }
        else if (mouseButton.ButtonIndex == MouseButton.WheelDown)
        {
            _distance = Mathf.Min(MaximumDistance, _distance + ZoomStep);
            ApplyOrbit();
            GetViewport().SetInputAsHandled();
        }
    }

    private void ApplyOrbit()
    {
        float horizontalDistance = Mathf.Cos(_pitch) * _distance;
        Vector3 offset = new(
            Mathf.Sin(_yaw) * horizontalDistance,
            Mathf.Sin(_pitch) * _distance,
            Mathf.Cos(_yaw) * horizontalDistance);

        GlobalPosition = Target + offset;
        LookAt(Target, Vector3.Up);
    }
}
