using Godot;

namespace GalaxyEngine3D.Camera;

public partial class OrbitCameraController : Camera3D
{
    private const float PlanarProjectionFallbackThresholdSquared = 0.01f;

    [Export] public Vector3 CameraFocusPosition { get; set; } = Vector3.Zero;
    [Export(PropertyHint.Range, "0.001,0.05,0.001")] public float OrbitSensitivity { get; set; } = 0.008f;
    [Export(PropertyHint.Range, "0.01,1.0,0.01")] public float ZoomExponent { get; set; } = 0.18f;
    [Export(PropertyHint.Range, "0.0001,0.02,0.0001")] public float PanSpeedFactor { get; set; } = 0.0015f;
    [Export(PropertyHint.Range, "0.05,5.0,0.05")] public float CameraMoveSpeed { get; set; } = 0.8f;
    [Export(PropertyHint.Range, "0.1,20.0,0.1")] public float CameraAcceleration { get; set; } = 5.0f;
    [Export(PropertyHint.Range, "0.1,20.0,0.1")] public float CameraDeceleration { get; set; } = 7.0f;
    [Export(PropertyHint.Range, "0.0,3.0,0.05")] public float FocusDuration { get; set; } = 0.45f;
    [Export] public float MinimumDistance { get; set; } = 0.25f;
    [Export] public float MaximumDistance { get; set; } = 80.0f;
    [Export] public Vector3 GalaxyPlaneRight { get; set; } = Vector3.Right;
    [Export] public Vector3 GalaxyPlaneForward { get; set; } = Vector3.Forward;
    [Export] public Vector3 GalaxyPlaneNormal { get; set; } = Vector3.Up;

    private bool _isOrbiting;
    private bool _isPanning;
    private bool _isFocusAnimationActive;
    private float _distance;
    private float _yaw;
    private float _pitch;
    private float _focusElapsed;
    private Vector3 _planeRight;
    private Vector3 _planeForward;
    private Vector3 _planeNormal;
    private Vector3 _lastStablePlanarForward;
    private Vector3 _movementVelocity;
    private Vector3 _focusStart;
    private Vector3 _focusTarget;

    public float Distance => _distance;

    public bool IsFocusAnimationActive => _isFocusAnimationActive;

    public override void _Ready()
    {
        NormalizeGalaxyBasis();
        _lastStablePlanarForward = _planeForward;
        LookAt(CameraFocusPosition, _planeNormal);
        SynchronizeFromTransform();
    }

    public override void _Process(double delta)
    {
        float frameDelta = (float)delta;
        Vector3 movementInput = ReadMovementInput();
        if (!movementInput.IsZeroApprox())
        {
            CancelFocusAnimation();
        }

        if (_isFocusAnimationActive)
        {
            AdvanceFocusAnimation(frameDelta);
            return;
        }

        float distanceScale = Mathf.Max(SafeMinimumDistance(), _distance);
        Vector3 desiredVelocity = movementInput.IsZeroApprox()
            ? Vector3.Zero
            : movementInput.Normalized() * distanceScale * CameraMoveSpeed;
        float response = movementInput.IsZeroApprox()
            ? CameraDeceleration
            : CameraAcceleration;
        _movementVelocity = _movementVelocity.MoveToward(
            desiredVelocity,
            response * distanceScale * frameDelta);

        if (!_movementVelocity.IsZeroApprox())
        {
            CameraFocusPosition += _movementVelocity * frameDelta;
            ApplyOrbit();
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
            CancelUserMotion();
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
            CancelUserMotion();
            Basis cameraBasis = GlobalTransform.Basis.Orthonormalized();
            float worldUnitsPerPixel = Mathf.Max(SafeMinimumDistance(), _distance) * PanSpeedFactor;
            Vector3 displacement =
                (-cameraBasis.X * mouseMotion.Relative.X + cameraBasis.Y * mouseMotion.Relative.Y) *
                worldUnitsPerPixel;
            CameraFocusPosition += displacement;
            ApplyOrbit();
            GetViewport().SetInputAsHandled();
        }
    }

    public void FocusOn(Vector3 focalPoint)
    {
        CancelUserMotion();
        CameraFocusPosition = focalPoint;
        ApplyOrbit();
    }

    public void AnimateFocusTo(Vector3 focalPoint)
    {
        _movementVelocity = Vector3.Zero;
        if (FocusDuration <= Mathf.Epsilon || CameraFocusPosition.IsEqualApprox(focalPoint))
        {
            FocusOn(focalPoint);
            return;
        }

        _focusStart = CameraFocusPosition;
        _focusTarget = focalPoint;
        _focusElapsed = 0.0f;
        _isFocusAnimationActive = true;
    }

    public void MoveFocus(Vector3 displacement)
    {
        CancelUserMotion();
        CameraFocusPosition += displacement;
        ApplyOrbit();
    }

    public void CancelFocusAnimation()
    {
        _isFocusAnimationActive = false;
        _focusElapsed = 0.0f;
    }

    public (Vector3 Forward, Vector3 Right) GetCameraRelativePlanarBasis()
    {
        Vector3 cameraForward = -GlobalTransform.Basis.Orthonormalized().Z;
        Vector3 projected = cameraForward - (_planeNormal * cameraForward.Dot(_planeNormal));
        if (projected.LengthSquared() >= PlanarProjectionFallbackThresholdSquared)
        {
            _lastStablePlanarForward = projected.Normalized();
        }

        return CalculateCameraRelativePlanarBasis(
            cameraForward,
            _planeNormal,
            _lastStablePlanarForward);
    }

    public static (Vector3 Forward, Vector3 Right) CalculateCameraRelativePlanarBasis(
        Vector3 cameraForward,
        Vector3 planeNormal,
        Vector3 fallbackForward)
    {
        Vector3 normal = planeNormal.IsZeroApprox() ? Vector3.Up : planeNormal.Normalized();
        Vector3 projected = cameraForward - (normal * cameraForward.Dot(normal));
        Vector3 forward;
        if (projected.LengthSquared() >= PlanarProjectionFallbackThresholdSquared)
        {
            forward = projected.Normalized();
        }
        else
        {
            forward = fallbackForward - (normal * fallbackForward.Dot(normal));
            if (forward.IsZeroApprox())
            {
                Vector3 reference = Mathf.Abs(normal.Dot(Vector3.Forward)) < 0.95f
                    ? Vector3.Forward
                    : Vector3.Right;
                forward = reference - (normal * reference.Dot(normal));
            }

            forward = forward.Normalized();
        }

        Vector3 right = forward.Cross(normal).Normalized();
        return (forward, right);
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
        GetCameraRelativePlanarBasis();
    }

    public void ApplyTransitionPose(Vector3 startFocus, Vector3 targetFocus, float startDistance, float targetDistance, float progress)
    {
        CancelUserMotion();
        float amount = Mathf.Clamp(progress, 0.0f, 1.0f);
        CameraFocusPosition = startFocus.Lerp(targetFocus, amount);
        float safeStart = Mathf.Max(startDistance, Near * 1.1f);
        float safeTarget = Mathf.Max(targetDistance, Near * 1.1f);
        _distance = Mathf.Exp(Mathf.Lerp(Mathf.Log(safeStart), Mathf.Log(safeTarget), amount));
        ApplyOrbit(false);
    }

    private Vector3 ReadMovementInput()
    {
        (Vector3 forward, Vector3 right) = GetCameraRelativePlanarBasis();
        Vector3 movement = Vector3.Zero;
        if (Input.IsKeyPressed(Key.W) || Input.IsKeyPressed(Key.Up))
        {
            movement += forward;
        }

        if (Input.IsKeyPressed(Key.S) || Input.IsKeyPressed(Key.Down))
        {
            movement -= forward;
        }

        if (Input.IsKeyPressed(Key.D) || Input.IsKeyPressed(Key.Right))
        {
            movement += right;
        }

        if (Input.IsKeyPressed(Key.A) || Input.IsKeyPressed(Key.Left))
        {
            movement -= right;
        }

        if (Input.IsKeyPressed(Key.E))
        {
            movement += _planeNormal;
        }

        if (Input.IsKeyPressed(Key.Q))
        {
            movement -= _planeNormal;
        }

        return movement;
    }

    private void AdvanceFocusAnimation(float delta)
    {
        _focusElapsed += delta;
        float amount = Mathf.Clamp(_focusElapsed / FocusDuration, 0.0f, 1.0f);
        float eased = amount * amount * (3.0f - (2.0f * amount));
        CameraFocusPosition = _focusStart.Lerp(_focusTarget, eased);
        ApplyOrbit();

        if (amount >= 1.0f)
        {
            CameraFocusPosition = _focusTarget;
            _isFocusAnimationActive = false;
            ApplyOrbit();
        }
    }

    private void HandleMouseButton(InputEventMouseButton mouseButton)
    {
        if (mouseButton.ButtonIndex == MouseButton.Right)
        {
            if (mouseButton.Pressed)
            {
                CancelUserMotion();
            }

            _isOrbiting = mouseButton.Pressed;
            Input.MouseMode = _isOrbiting
                ? Input.MouseModeEnum.Captured
                : Input.MouseModeEnum.Visible;
            GetViewport().SetInputAsHandled();
            return;
        }

        if (mouseButton.ButtonIndex == MouseButton.Middle)
        {
            if (mouseButton.Pressed)
            {
                CancelUserMotion();
            }

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

        CancelUserMotion();
        _distance = Mathf.Clamp(
            _distance * Mathf.Exp(zoomDirection * ZoomExponent),
            SafeMinimumDistance(),
            MaximumDistance);
        ApplyOrbit();
        GetViewport().SetInputAsHandled();
    }

    private void CancelUserMotion()
    {
        CancelFocusAnimation();
        _movementVelocity = Vector3.Zero;
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
        _lastStablePlanarForward =
            ((_planeForward * Mathf.Cos(_yaw)) - (_planeRight * Mathf.Sin(_yaw))).Normalized();
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

        if (_lastStablePlanarForward.IsZeroApprox())
        {
            _lastStablePlanarForward = _planeForward;
        }
    }
}
