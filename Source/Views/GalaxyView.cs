using System;
using Galaxy.Core;
using Galaxy.Core.Generation;
using Godot;
using GalaxyEngine3D.Core;

namespace GalaxyEngine3D.Views;

public partial class GalaxyView : GameView
{
    [Export] public long GalaxySeedValue { get; set; } = 4_843_128_229_401_783_567L;
    [Export(PropertyHint.Range, "1,16,1")] public int SpiralArmCount { get; set; } = 4;
    [Export(PropertyHint.Range, "1.0,100.0,0.1")] public float GalaxyRadius { get; set; } = 8.0f;
    [Export(PropertyHint.Range, "0.0,20.0,0.05")] public float DiskThickness { get; set; } = 0.8f;
    [Export(PropertyHint.Range, "0.0,50.0,0.1")] public float BulgeRadius { get; set; } = 2.0f;
    [Export(PropertyHint.Range, "-12.0,12.0,0.1")] public float ArmTwistRadians { get; set; } = 4.5f;
    [Export(PropertyHint.Range, "0.1,50.0,0.1")] public float DiskScaleLength { get; set; } = 2.4f;
    [Export(PropertyHint.Range, "0.0,10.0,0.05")] public float InterArmDensityFactor { get; set; } = 0.35f;
    [Export(PropertyHint.Range, "0.0,10.0,0.05")] public float ArmDensityMultiplier { get; set; } = 1.5f;
    [Export(PropertyHint.Range, "0.0,10.0,0.05")] public float BulgeDensityMultiplier { get; set; } = 1.8f;
    [Export(PropertyHint.Range, "0.0,1.0,0.001")] public float MinimumStarDistance { get; set; } = 0.006f;
    [Export(PropertyHint.Range, "1,500000,1")] public int StarCount { get; set; } = 50_000;
    [Export(PropertyHint.Range, "0.01,1.0,0.01")] public float PickRadius { get; set; } = 0.16f;

    private MultiMeshInstance3D _starInstances = null!;
    private MeshInstance3D _selectionMarker = null!;
    private GeneratedGalaxy? _galaxy;
    private ViewContext? _context;

    public override ViewId Id => ViewId.Galaxy;

    public override string DisplayName => "GalaxyView";

    public override bool CanAdvance => SelectedStarId is not null;

    public GalaxySeed Seed => new(unchecked((ulong)GalaxySeedValue));

    public StarId? SelectedStarId { get; private set; }

    public int GeneratedStarCount => _galaxy?.Stars.Count ?? 0;

    public override void Configure(ViewContext context)
    {
        _context = context;
    }

    public override void _Ready()
    {
        _starInstances = GetNode<MultiMeshInstance3D>("Stars");
        _selectionMarker = GetNode<MeshInstance3D>("SelectionMarker");

        GalaxyGenerationParameters parameters = new()
        {
            SpiralArmCount = SpiralArmCount,
            Radius = GalaxyRadius,
            DiskThickness = DiskThickness,
            BulgeRadius = BulgeRadius,
            ArmTwistRadians = ArmTwistRadians,
            DiskScaleLength = DiskScaleLength,
            InterArmDensityFactor = InterArmDensityFactor,
            ArmDensityMultiplier = ArmDensityMultiplier,
            BulgeDensityMultiplier = BulgeDensityMultiplier,
            MinimumStarDistance = MinimumStarDistance,
            StarCount = StarCount,
        };

        _galaxy = new SpiralGalaxyGenerator().Generate(Seed, parameters);
        BuildMultiMesh(_galaxy);
        RestoreSelection();
    }

    public override void _UnhandledInput(InputEvent inputEvent)
    {
        if (HandleCameraFocusInput(inputEvent))
        {
            return;
        }

        if (inputEvent is not InputEventMouseButton mouseButton ||
            mouseButton.ButtonIndex != MouseButton.Left ||
            !mouseButton.Pressed ||
            _galaxy is null)
        {
            return;
        }

        Vector3 rayOrigin = Camera.ProjectRayOrigin(mouseButton.Position);
        Vector3 rayDirection = Camera.ProjectRayNormal(mouseButton.Position).Normalized();
        int selectedIndex = FindStarAlongRay(rayOrigin, rayDirection);
        if (selectedIndex < 0)
        {
            return;
        }

        SelectStar(selectedIndex);
        GetViewport().SetInputAsHandled();
    }

    public Vector3 GetStarPosition(int stableIndex)
    {
        if (_galaxy is null || (uint)stableIndex >= (uint)_galaxy.Stars.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(stableIndex));
        }

        return ToGodot(_galaxy.Stars[stableIndex].Position);
    }

    public float GetStarVisualRadius(int stableIndex)
    {
        if (_galaxy is null || (uint)stableIndex >= (uint)_galaxy.Stars.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(stableIndex));
        }

        return _galaxy.Stars[stableIndex].VisualRadius;
    }

    private void BuildMultiMesh(GeneratedGalaxy galaxy)
    {
        MultiMesh multiMesh = new()
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
            UseColors = true,
            InstanceCount = galaxy.Stars.Count,
            VisibleInstanceCount = -1,
            Mesh = _starInstances.Multimesh?.Mesh,
        };

        for (int index = 0; index < galaxy.Stars.Count; index++)
        {
            GalaxyStar star = galaxy.Stars[index];
            float scale = star.VisualRadius * 2.0f;
            Basis basis = Basis.Identity.Scaled(Vector3.One * scale);
            multiMesh.SetInstanceTransform(index, new Transform3D(basis, ToGodot(star.Position)));
            multiMesh.SetInstanceColor(index, TemperatureToColor(star.TemperatureKelvin));
        }

        _starInstances.Multimesh = multiMesh;
        float extent = GalaxyRadius + 1.0f;
        float verticalExtent = Mathf.Max(DiskThickness, BulgeRadius) + 1.0f;
        _starInstances.CustomAabb = new Aabb(
            new Vector3(-extent, -verticalExtent, -extent),
            new Vector3(extent * 2.0f, verticalExtent * 2.0f, extent * 2.0f));
    }

    private int FindStarAlongRay(Vector3 rayOrigin, Vector3 rayDirection)
    {
        if (_galaxy is null)
        {
            return -1;
        }

        int bestIndex = -1;
        float bestScore = float.PositiveInfinity;
        float bestDistance = float.PositiveInfinity;

        for (int index = 0; index < _galaxy.Stars.Count; index++)
        {
            Vector3 offset = ToGodot(_galaxy.Stars[index].Position) - rayOrigin;
            float distanceAlongRay = offset.Dot(rayDirection);
            if (distanceAlongRay <= 0.0f)
            {
                continue;
            }

            float perpendicularSquared = Mathf.Max(0.0f, offset.LengthSquared() - (distanceAlongRay * distanceAlongRay));
            float effectiveRadius = PickRadius + (distanceAlongRay * 0.0035f);
            float score = perpendicularSquared / (effectiveRadius * effectiveRadius);
            if (score > 1.0f ||
                score > bestScore ||
                (score == bestScore && distanceAlongRay >= bestDistance))
            {
                continue;
            }

            bestIndex = index;
            bestScore = score;
            bestDistance = distanceAlongRay;
        }

        return bestIndex;
    }

    private void SelectStar(int stableIndex)
    {
        if (_galaxy is null)
        {
            return;
        }

        GalaxyStar star = _galaxy.Stars[stableIndex];
        SelectedStarId = star.Id;
        Vector3 localPosition = ToGodot(star.Position);
        _selectionMarker.Position = localPosition;
        _selectionMarker.Visible = true;
        SetSelectionTarget(
            star.Id.Value.ToString("X16"),
            $"Csillag {star.Id}",
            () => ToGlobal(localPosition),
            star.VisualRadius);
    }

    private void RestoreSelection()
    {
        if (_galaxy is null || _context?.SelectedStarId is not StarId selectedId)
        {
            return;
        }

        for (int index = 0; index < _galaxy.Stars.Count; index++)
        {
            if (_galaxy.Stars[index].Id == selectedId)
            {
                SelectStar(index);
                return;
            }
        }
    }

    private static Vector3 ToGodot(GalaxyVector3 value) =>
        new((float)value.X, (float)value.Y, (float)value.Z);

    private static Color TemperatureToColor(float temperatureKelvin)
    {
        float normalized = Mathf.Clamp((temperatureKelvin - 2_500.0f) / 9_500.0f, 0.0f, 1.0f);
        Color warm = new(1.0f, 0.58f, 0.28f);
        Color white = new(1.0f, 0.94f, 0.82f);
        Color cool = new(0.48f, 0.68f, 1.0f);
        return normalized < 0.55f
            ? warm.Lerp(white, normalized / 0.55f)
            : white.Lerp(cool, (normalized - 0.55f) / 0.45f);
    }
}
