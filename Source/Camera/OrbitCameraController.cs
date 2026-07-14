using Godot;

namespace GalaxyEngine3D.Camera;

public partial class OrbitCameraController : Camera3D
{
    [Export] public Vector3 CameraFocusPosition { get; set; } = Vector3.Zero;
    [Export(PropertyHint.Range, "0.001,0.05,0.001")] public float OrbitSensitivity { get; set; } = 0.008f;
    [Export(PropertyHint.Range, "0.01,1.0,0.01")] public float ZoomExponent { get; set; } = 0.18f;
    [Export(PropertyHint.Range, "0.0001,0.02,0.0001")] public float PanSpeedFactor { get; set; } = 0.0015f;
    [Export(PropertyHint.Range, "0.05,5.0,0.05")] public float KeyboardMoveSpeedFactor { get; set; } = 0.8f;
    [Export] public float MinimumDistance { get; set; } = 0.25f;
    [Export] public float MaximumDistance { get; set; } = 80.0f;
    [Export] public Vector3 GalaxyPlaneRight { get; set; } = Vector3.Right;
    [Export] public Vector3 GalaxyPlaneForward { get; set; } = Vector3.Forward;
    [Export] public Vector3 GalaxyPlaneNormal { get; set; } = Vector3.Up;

    private bool _isOrbiting;
    private bool _isPanning;
    private float _distance;
    private float _yaw;
    private float _pitch;
    private Vector3 _planeRight;
    private Vector3 _planeForward;
    private Vector3 _planeNormal;

    public float Distance => _distance;

    public override void _Ready()
    {
        NormalizeGalaxyBasis();
        LookAt(CameraFocusPosition, _planeNormal);
        SynchronizeFromTransform();
    }

    public override void _Process(double delta)
    {
        Vector3 movement = Vector3.Zero;
        if (Input.IsKeyPressed(Key.W) || Input.IsKeyPressed(Key.Up))
        {
            movement += _planeForward;
        }

        if (Input.IsKeyPressed(Key.S) || Input.IsKeyPressed(Key.Down))
        {
            movement -= _planeForward;
        }

        if (Input.IsKeyPressed(Key.D) || Input.IsKeyPressed(Key.Right))
        {
            movement += _planeRight;
        }

        if (Input.IsKeyPressed(Key.A) || Input.IsKeyPressed(Key.Left))
        {
            movement -= _planeRight;
        }

        if (Input.IsKeyPressed(Key.E))
        {
            movement += _planeNormal;
        }

        if (Input.IsKeyPressed(Key.Q))
        {
            movement -= _planeNormal;
        }

        if (!movement.IsZeroApprox())
        {
            float speed = Mathf.Max(MinimumDistance, _distance) * KeyboardMoveSpeedFactor;
            MoveFocus(movement.Normalized() * speed * (float)delta);
        }
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

        if (inputEvent is not InputEventMouseMotion mouseMotion)
        {
            return;
        }

        if (_isOrbiting)
        {
            _yaw -= mouseMotion.Relative.X * OrbitSensitivity;
            _pitch = Mathf.Clamp(
                _pitch - mouseMotion.Relative.Y * OrbitSensitivity,
                Mathf.DegToRad(-85.0f),
                Mathf.DegToRad(85.0f));
            ApplyOrbit();
            GetViewport().SetInputAsHandled();
        }
        else if (_isPanning)
        {
            Basis cameraBasis = GlobalTransform.Basis.Orthonormalized();
            float worldUnitsPerPixel = Mathf.Max(MinimumDistance, _distance) * PanSpeedFactor;
            Vector3 displacement =
                (-cameraBasis.X * mouseMotion.Relative.X + cameraBasis.Y * mouseMotion.Relative.Y) *
                worldUnitsPerPixel;
            MoveFocus(displacement);
            GetViewport().SetInputAsHandled();
        }
    }

    public void FocusOn(Vector3 focalPoint)
    {
        CameraFocusPosition = focalPoint;
        ApplyOrbit();
    }

    public void MoveFocus(Vector3 displacement)
    {
        CameraFocusPosition += displacement;
        ApplyOrbit();
    }

    public void SynchronizeFromTransform()
    {
        NormalizeGalaxyBasis();
        Vector3 offset = GlobalPosition - CameraFocusPosition;
        _distance = Mathf.Clamp(offset.Length(), SafeMinimumDistance(), MaximumDistance);

        if (_distance <= Mathf.Epsilon)
        {
            _distance = SafeMinimumDistance();
            _yaw = 0.0f;
            _pitch = 0.0f;
            ApplyOrbit();
            return;
        }

        _pitch = Mathf.Asin(Mathf.Clamp(offset.Dot(_planeNormal) / _distance, -1.0f, 1.0f));
        Vector3 planarOffset = offset - (_planeNormal * offset.Dot(_planeNormal));
        if (!planarOffset.IsZeroApprox())
        {
            planarOffset = planarOffset.Normalized();
            _yaw = Mathf.Atan2(planarOffset.Dot(_planeRight), -planarOffset.Dot(_planeForward));
        }

        ApplyOrbit();
    }

    public void ApplyTransitionPose(Vector3 startFocus, Vector3 targetFocus, float startDistance, float targetDistance, float progress)
    {
        float amount = Mathf.Clamp(progress, 0.0f, 1.0f);
        CameraFocusPosition = startFocus.Lerp(targetFocus, amount);
        float safeStart = Mathf.Max(startDistance, Near * 1.1f);
        float safeTarget = Mathf.Max(targetDistance, Near * 1.1f);
        _distance = Mathf.Exp(Mathf.Lerp(Mathf.Log(safeStart), Mathf.Log(safeTarget), amount));
        ApplyOrbit(false);
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

        if (mouseButton.ButtonIndex == MouseButton.Middle)
        {
            _isPanning = mouseButton.Pressed;
            GetViewport().SetInputAsHandled();
            return;
        }

        if (!mouseButton.Pressed)
        {
            return;
        }

        float zoomDirection = mouseButton.ButtonIndex switch
        {
            MouseButton.WheelUp => -1.0f,
            MouseButton.WheelDown => 1.0f,
            _ => 0.0f,
        };

        if (zoomDirection == 0.0f)
        {
            return;
        }

        _distance = Mathf.Clamp(
            _distance * Mathf.Exp(zoomDirection * ZoomExponent),
            SafeMinimumDistance(),
            MaximumDistance);
        ApplyOrbit();
        GetViewport().SetInputAsHandled();
    }

    private void ApplyOrbit(bool clampToViewLimits = true)
    {
        if (clampToViewLimits)
        {
            _distance = Mathf.Clamp(_distance, SafeMinimumDistance(), MaximumDistance);
        }
        else
        {
            _distance = Mathf.Max(_distance, Near * 1.1f);
        }

        float horizontalDistance = Mathf.Cos(_pitch) * _distance;
        Vector3 offset =
            (_planeRight * (Mathf.Sin(_yaw) * horizontalDistance)) +
            (_planeNormal * (Mathf.Sin(_pitch) * _distance)) -
            (_planeForward * (Mathf.Cos(_yaw) * horizontalDistance));

        GlobalPosition = CameraFocusPosition + offset;
        LookAt(CameraFocusPosition, _planeNormal);
    }

    private float SafeMinimumDistance() => Mathf.Max(MinimumDistance, Near * 1.1f);

    private void NormalizeGalaxyBasis()
    {
        _planeNormal = GalaxyPlaneNormal.IsZeroApprox() ? Vector3.Up : GalaxyPlaneNormal.Normalized();
        Vector3 forward = GalaxyPlaneForward - (_planeNormal * GalaxyPlaneForward.Dot(_planeNormal));
        if (forward.IsZeroApprox())
        {
            forward = _planeNormal.Cross(Vector3.Right);
            if (forward.IsZeroApprox())
            {
                forward = _planeNormal.Cross(Vector3.Forward);
            }
        }

        _planeForward = forward.Normalized();
        _planeRight = _planeForward.Cross(_planeNormal).Normalized();
        if (!GalaxyPlaneRight.IsZeroApprox() && _planeRight.Dot(GalaxyPlaneRight) < 0.0f)
        {
            _planeRight = -_planeRight;
            _planeForward = -_planeForward;
        }
    }
}
