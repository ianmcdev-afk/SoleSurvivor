using System.Windows;
using System.Windows.Input;
using SoleSurvivor.Core;

namespace SoleSurvivor.App;

/// <summary>
/// Core of the main window: game lifecycle, input, and the render pass.
/// Board drawing/animation lives in MainWindow.Board.cs, the tide sequence in MainWindow.Tide.cs,
/// and the sidebar in MainWindow.Sidebar.cs.
/// </summary>
public partial class MainWindow : Window
{
    private enum Stage { YourTurn, TideCard, SheMoves, SheStrikes }

    /// <summary>What the board looked like before an action, so we can animate the difference.</summary>
    private sealed record Snapshot(int Survivor, int Tidewife, int[] Villagers, int Health, int TidewifeHealth, int Saved, DiceRoll? Roll,
        int Time, int HandCount, int MarketCount, int InventoryCount, int HorrorDice, int Locked)
    {
        public Snapshot(Game g)
            : this(g.SurvivorLocation, g.TidewifeLocation, g.Villagers.ToArray(), g.Health, g.TidewifeHealth, g.VillagersSaved, g.LastRoll,
                   g.Time, g.Hand.Count, g.Market.Count, g.Inventory.Count, g.HorrorDice, g.LockedDoors.Count()) { }
    }

    private static bool _clickSoundRegistered;

    private Game _game = null!;
    private int _logShown;
    private bool _busy;
    private Stage _stage = Stage.YourTurn;
    private readonly Settings _settings = Settings.Load();

    /// <summary>True once a night has been started from the menu (and until it ends).</summary>
    private bool _inProgress;

    public MainWindow()
    {
        InitializeComponent();
        Anim.Speed = _settings.FastAnimations ? 0.4 : 1.0;
        Sound.Init(_settings.Muted, _settings.Volume, _settings.AmbientSound);
        if (!_clickSoundRegistered)
        {
            // Every button, toggle and checkbox in the app gets a soft click.
            EventManager.RegisterClassHandler(typeof(System.Windows.Controls.Primitives.ButtonBase),
                System.Windows.Controls.Primitives.ButtonBase.ClickEvent,
                new RoutedEventHandler((_, _) => Sound.Play("ui_click", 0.7)));
            _clickSoundRegistered = true;
        }
        RenderMuteButton();
        HintText.Visibility = _settings.ShowHints ? Visibility.Visible : Visibility.Collapsed;
        // Size the window once it exists, so the screen's DPI scaling is known.
        SourceInitialized += (_, _) => ApplyDisplay();

        // A preview game sits behind the title screen so its board animates there.
        _game = new Game(null, _settings.Difficulty, Scenarios.ById(_settings.LastScenario));
        BuildScene();
        BuildPortraits();
        Render();
        ShowMenu(MenuPage.Main);
    }

    /// <summary>Start a fresh night in the given scenario.</summary>
    private void StartNewGame(ScenarioDef scenario, Difficulty difficulty)
    {
        _game = new Game(null, difficulty, scenario);
        _inProgress = true;
        _settings.LastScenario = scenario.Id;
        _settings.Save();
        BuildScene();
        BuildPortraits();
        _logShown = 0;
        _trayRoll = null;
        _trayEnemy = false;
        LogPanel.Children.Clear();
        GameOverOverlay.Visibility = Visibility.Collapsed;
        GameOverOverlay.Tag = null;
        TideCardOverlay.Visibility = Visibility.Collapsed;
        Anim.Set(PhaseBanner, OpacityProperty, 0);
        Anim.Set(FloodOverlay, OpacityProperty, 0);
        WalkMode.IsChecked = true;
        Title = $"Sole Survivor: {_game.Scenario.Title} ({_game.Difficulty})";
        ResetFigures();
        _stage = Stage.YourTurn;
        _startPicks.Clear();
        Render();
        ShowBriefing(); // the objective first; the starting-card picker and ROUND 1 banner follow
    }

    // ================================================================ input

    private void SetBusy(bool busy)
    {
        _busy = busy;
        InputBlocker.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>Run a player action, then animate whatever changed.</summary>
    private async void Perform(Action action)
    {
        if (_busy || _game.IsOver) return;
        var before = new Snapshot(_game);
        action();
        SetBusy(true);
        try
        {
            await AnimateActionAsync(before);
        }
        finally
        {
            SetBusy(false);
            Render();
        }
    }

    private async void DoEndTurn()
    {
        if (_busy || !_game.CanEndTurn) return;
        var before = new Snapshot(_game);
        _game.EndTurn();
        SetBusy(true);
        try
        {
            await PlayTideAsync(before);
        }
        finally
        {
            SetBusy(false);
            _stage = Stage.YourTurn;
            Render();
        }
        if (!_game.IsOver)
        {
            Sound.Play("round", 0.7);
            await ShowBannerAsync($"ROUND {_game.Round}",
                _game.InFinale ? $"Your turn. Survive the {_game.Scenario.FinaleName}." : $"Your turn. You have {_game.Time} Time.", Art.Warm);
        }
    }

    private void Mute_Click(object sender, RoutedEventArgs e) => ToggleMute();

    private void ToggleMute()
    {
        _settings.Muted = !_settings.Muted;
        _settings.Save();
        Sound.SetMuted(_settings.Muted);
        RenderMuteButton();
        if (OptionsPanel.Visibility == Visibility.Visible) LoadOptionControls();
    }

    private void RenderMuteButton()
    {
        MuteButton.Content = _settings.Muted ? "♪  Sound off" : "♪  Sound on";
        MuteButton.Foreground = _settings.Muted ? Art.Dim : Art.TextColor;
        MuteButton.ToolTip = _settings.Muted ? "Unmute (M)" : "Mute all sound (M)";
    }

    private void PlayAgain_Click(object sender, RoutedEventArgs e) => StartNewGame(_game.Scenario, _game.Difficulty);

    private void GameOverMenu_Click(object sender, RoutedEventArgs e)
    {
        GameOverOverlay.Visibility = Visibility.Collapsed;
        ShowMenu(MenuPage.Main);
    }

    private void Rules_Click(object sender, RoutedEventArgs e) => new RulesWindow(_game.Scenario) { Owner = this }.ShowDialog();

    private void HideOverlay_Click(object sender, RoutedEventArgs e) => GameOverOverlay.Visibility = Visibility.Collapsed;

    private void EndTurn_Click(object sender, RoutedEventArgs e) => DoEndTurn();

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F11)
        {
            e.Handled = true;
            _settings.Fullscreen = !_settings.Fullscreen;
            _settings.Save();
            ApplyDisplay();
            return;
        }
        if (e.Key == Key.M)
        {
            e.Handled = true;
            ToggleMute();
            return;
        }
        if (BriefingOpen)
        {
            // Esc, Enter or Space close the objective panel; nothing else happens underneath it.
            if (e.Key is Key.Escape or Key.Enter or Key.Space)
            {
                e.Handled = true;
                HideBriefing();
            }
            return;
        }
        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            OnEscape();
            return;
        }
        if (MenuOpen) return;
        if (e.Key == Key.E && !_busy && _game.CanEndTurn && GameOverOverlay.Visibility != Visibility.Visible)
        {
            e.Handled = true;
            DoEndTurn();
        }
    }

    private void Blocker_Click(object sender, MouseButtonEventArgs e) => SkipCard();

    private void TideCard_Click(object sender, MouseButtonEventArgs e) => SkipCard();

    private void Mode_Click(object sender, RoutedEventArgs e) => Render();

    private void Search_Click(object sender, RoutedEventArgs e) => Perform(() => _game.Search());

    private void Fight_Click(object sender, RoutedEventArgs e) => Perform(() => _game.Fight());

    private void Rest_Click(object sender, RoutedEventArgs e) => Perform(() => _game.Rest());

    private void Steel_Click(object sender, RoutedEventArgs e)
    {
        _game.SteelYourself = SteelCheck.IsChecked == true;
        RenderHint();
    }

    private void OnLocationClicked(int id)
    {
        if (_busy) return;
        if (EscortMode.IsChecked == true && _game.CanEscort(id)) Perform(() => _game.Escort(id));
        else if (_game.CanMove(id)) Perform(() => _game.Move(id));
    }

    private bool IsReachable(int id) =>
        !_busy && (_game.CanMove(id) || (EscortMode.IsChecked == true && _game.CanEscort(id)));

    // ================================================================ render

    private void Render()
    {
        RenderBoard();
        RenderStage();
        RenderStatus();
        RenderActions();
        RenderDice();
        RenderCards();
        RenderItems();
        RenderTideReport();
        RenderLog();
        RenderGameOver();
    }
}
