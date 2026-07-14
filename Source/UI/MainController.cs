using System.Threading.Tasks;
using Godot;
using GalaxyEngine3D.Core;
using GalaxyEngine3D.Navigation;
using GalaxyEngine3D.Views;

namespace GalaxyEngine3D.UI;

public partial class MainController : Node
{
    private ViewRouter _router = null!;
    private TransitionController _transition = null!;
    private Label _titleLabel = null!;
    private Label _selectionLabel = null!;
    private Label _hintLabel = null!;
    private Label _developerInfoLabel = null!;
    private Button _backButton = null!;
    private Button _nextButton = null!;
    private GameView? _activeView;
    private bool _navigationLocked;
    private ulong _galaxySeed;
    private int _galaxyStarCount;
    private int _galaxyArmCount;

    public override void _Ready()
    {
        _router = GetNode<ViewRouter>("ViewRouter");
        _transition = GetNode<TransitionController>("TransitionController");
        _titleLabel = GetNode<Label>("Hud/Panel/Margin/VBox/Title");
        _selectionLabel = GetNode<Label>("Hud/Panel/Margin/VBox/Selection");
        _hintLabel = GetNode<Label>("Hud/Panel/Margin/VBox/Hint");
        _developerInfoLabel = GetNode<Label>("Hud/DeveloperPanel/Margin/Info");
        _backButton = GetNode<Button>("Hud/Panel/Margin/VBox/Buttons/Back");
        _nextButton = GetNode<Button>("Hud/Panel/Margin/VBox/Buttons/Next");

        _router.CurrentViewChanged += OnCurrentViewChanged;
        _backButton.Pressed += OnBackPressed;
        _nextButton.Pressed += OnNextPressed;
        _router.Initialize();
    }

    public override void _Process(double delta)
    {
        RefreshDeveloperInfo();
    }

    private void RefreshDeveloperInfo()
    {
        _developerInfoLabel.Text =
            $"Seed: {_galaxySeed}\n" +
            $"Csillagok: {_galaxyStarCount:N0}\n" +
            $"Spirálkarok: {_galaxyArmCount}\n" +
            $"FPS: {Engine.GetFramesPerSecond()}";
    }

    private void OnCurrentViewChanged(GameView view)
    {
        if (_activeView is not null)
        {
            _activeView.SelectionChanged -= OnSelectionChanged;
        }

        _activeView = view;
        _activeView.SelectionChanged += OnSelectionChanged;
        _router.ReceiveSelectedObject(view.SelectedTarget);
        if (view is GalaxyView galaxyView)
        {
            _galaxySeed = galaxyView.Seed.Value;
            _galaxyStarCount = galaxyView.GeneratedStarCount;
            _galaxyArmCount = galaxyView.SpiralArmCount;
        }
        RefreshDeveloperInfo();
        RefreshHud();
    }

    private void OnSelectionChanged(string? selectionName)
    {
        _router.ReceiveSelectedObject(_activeView?.SelectedTarget);

        if (_activeView?.Id == ViewId.Galaxy)
        {
            _router.Context.SelectedStarId = (_activeView as GalaxyView)?.SelectedStarId;
            _router.Context.SelectedStarName = selectionName;
            _router.Context.SelectedPlanetName = null;
            _router.Context.SelectedPlanetId = null;
        }
        else if (_activeView?.Id == ViewId.System)
        {
            _router.Context.SelectedPlanetName = selectionName;
            _router.Context.SelectedPlanetId = _activeView.SelectedTarget?.ObjectId;
        }

        RefreshHud();
    }

    private async void OnNextPressed()
    {
        if (_activeView is null || !_activeView.CanAdvance)
        {
            return;
        }

        ViewId target = _activeView.Id switch
        {
            ViewId.Galaxy => ViewId.System,
            ViewId.System => ViewId.Planet,
            _ => _activeView.Id,
        };

        await NavigateAsync(target);
    }

    private async void OnBackPressed()
    {
        if (_activeView is null || _activeView.Id == ViewId.Galaxy)
        {
            return;
        }

        ViewId target = _activeView.Id == ViewId.Planet
            ? ViewId.System
            : ViewId.Galaxy;
        await NavigateAsync(target);
    }

    private async Task NavigateAsync(ViewId target)
    {
        if (_navigationLocked || _activeView is null)
        {
            return;
        }

        bool enteringChildView =
            (_activeView.Id == ViewId.Galaxy && target == ViewId.System) ||
            (_activeView.Id == ViewId.System && target == ViewId.Planet);
        ViewSelectionTarget? transitionTarget = enteringChildView
            ? _activeView.SelectedTarget
            : null;
        if (enteringChildView && transitionTarget is null)
        {
            return;
        }

        _router.ReceiveSelectedObject(transitionTarget);
        _navigationLocked = true;
        _router.CaptureCurrentCameraState();
        RefreshHud();

        try
        {
            await _transition.PlayAsync(
                _activeView.Camera,
                transitionTarget,
                () => _router.SwitchTo(target));
        }
        finally
        {
            _navigationLocked = false;
            RefreshHud();
        }
    }

    private void RefreshHud()
    {
        if (_activeView is null)
        {
            return;
        }

        _titleLabel.Text = _activeView.Id switch
        {
            ViewId.Galaxy => "GalaxyView — Galaxis",
            ViewId.System => $"SystemView — {_router.Context.SelectedStarName ?? "Csillagrendszer"}",
            ViewId.Planet => $"PlanetView — {_router.Context.SelectedPlanetName ?? "Bolygó"}",
            _ => _activeView.DisplayName,
        };

        _selectionLabel.Text = _activeView.SelectionName is null
            ? "Kijelölés: nincs"
            : $"Kijelölés: {_activeView.SelectionName}";

        _hintLabel.Text = _activeView.Id switch
        {
            ViewId.Galaxy => "Bal kattintás: csillag kijelölése · F: fókusz",
            ViewId.System => "Bal kattintás: bolygó kijelölése · F: fókusz",
            ViewId.Planet => "Jobb egér: forgatás · Középső egér: mozgatás · Görgő: zoom",
            _ => string.Empty,
        };

        _backButton.Visible = _activeView.Id != ViewId.Galaxy;
        _backButton.Disabled = _navigationLocked;
        _nextButton.Visible = _activeView.Id != ViewId.Planet;
        _nextButton.Disabled = _navigationLocked || !_activeView.CanAdvance;
        _nextButton.Text = _activeView.Id == ViewId.Galaxy
            ? "Rendszer megnyitása"
            : "Bolygó megnyitása";
    }
}
