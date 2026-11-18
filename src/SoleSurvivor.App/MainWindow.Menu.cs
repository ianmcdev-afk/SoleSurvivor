using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using SoleSurvivor.Core;

namespace SoleSurvivor.App;

/// <summary>The front end: title screen, scenario select, options, pause and quit.</summary>
public partial class MainWindow
{
    private enum MenuPage { Main, Scenario, Options, Pause }

    private MenuPage _menuPage = MenuPage.Main;
    private MenuPage _optionsReturn = MenuPage.Main;
    private ScenarioDef _pickScenario = Scenarios.Gallowmouth;
    private Difficulty _pickDifficulty = Difficulty.Normal;
    private Action? _confirmAction;

    private bool MenuOpen => MenuOverlay.Visibility == Visibility.Visible;

    // ------------------------------------------------------------------ show / hide

    /// <summary>"Six nights. Six horrors." — counted from the scenario list, so it stays right when nights are added.</summary>
    private static string TitleLine()
    {
        string[] words = ["No", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten", "Eleven", "Twelve"];
        int n = Scenarios.All.Count;
        string w = n < words.Length ? words[n] : n.ToString();
        return n == 1 ? "One night. One horror. Only one of you walks out." : $"{w} nights. {w} horrors. Only one of you walks out.";
    }

    private void ShowMenu(MenuPage page)
    {
        TitleTagline.Text = TitleLine();
        if (page == MenuPage.Options && _menuPage != MenuPage.Options) _optionsReturn = MenuOpen ? _menuPage : MenuPage.Pause;
        _menuPage = page;
        MenuOverlay.Visibility = Visibility.Visible;
        GameRoot.Effect = new BlurEffect { Radius = 5, RenderingBias = RenderingBias.Performance };
        ConfirmPanel.Visibility = Visibility.Collapsed;

        MainMenuPanel.Visibility = page == MenuPage.Main ? Visibility.Visible : Visibility.Collapsed;
        ScenarioPanel.Visibility = page == MenuPage.Scenario ? Visibility.Visible : Visibility.Collapsed;
        OptionsPanel.Visibility = page == MenuPage.Options ? Visibility.Visible : Visibility.Collapsed;
        PausePanel.Visibility = page == MenuPage.Pause ? Visibility.Visible : Visibility.Collapsed;

        switch (page)
        {
            case MenuPage.Main:
                bool canContinue = _inProgress && !_game.IsOver;
                ContinueButton.Visibility = canContinue ? Visibility.Visible : Visibility.Collapsed;
                ContinueSub.Text = canContinue ? $"{_game.Scenario.Title}  ·  round {_game.Round}  ·  {_game.Difficulty}" : "";
                var v = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version;
                VersionText.Text = v is null ? "" : $"Version {v.Major}.{v.Minor}.{v.Build}";
                break;
            case MenuPage.Scenario:
                _pickScenario = Scenarios.ById(_settings.LastScenario);
                _pickDifficulty = _settings.Difficulty;
                PreviewScenario(_pickScenario);
                RenderScenarioCards();
                RenderDifficultyPick();
                break;
            case MenuPage.Options:
                LoadOptionControls();
                break;
            case MenuPage.Pause:
                PauseSub.Text = $"{_game.Scenario.Title}  ·  {_game.Scenario.Survivor.Name}  ·  round {_game.Round}";
                break;
        }
        RenderPlanning();
    }

    private void HideMenu()
    {
        MenuOverlay.Visibility = Visibility.Collapsed;
        GameRoot.Effect = null;
        Render();
    }

    private void OnEscape()
    {
        if (ConfirmPanel.Visibility == Visibility.Visible)
        {
            ConfirmPanel.Visibility = Visibility.Collapsed;
            return;
        }
        if (MenuOpen)
        {
            switch (_menuPage)
            {
                case MenuPage.Options: ShowMenu(_optionsReturn); break;
                case MenuPage.Scenario: ShowMenu(MenuPage.Main); break;
                case MenuPage.Pause: HideMenu(); break;
                case MenuPage.Main when _inProgress && !_game.IsOver: HideMenu(); break;
            }
            return;
        }
        if (!_busy) ShowMenu(_inProgress && !_game.IsOver ? MenuPage.Pause : MenuPage.Main);
    }

    private void Confirm(string title, string text, string yes, Action onYes)
    {
        ConfirmTitle.Text = title;
        ConfirmText.Text = text;
        ConfirmYes.Content = yes;
        _confirmAction = onYes;
        ConfirmPanel.Visibility = Visibility.Visible;
    }

    // ------------------------------------------------------------------ buttons

    private void Menu_Click(object sender, RoutedEventArgs e)
    {
        if (!_busy) ShowMenu(_inProgress && !_game.IsOver ? MenuPage.Pause : MenuPage.Main);
    }

    private void Continue_Click(object sender, RoutedEventArgs e) => HideMenu();

    private void MenuNewGame_Click(object sender, RoutedEventArgs e)
    {
        if (_inProgress && !_game.IsOver)
            Confirm("Abandon this night?", $"Your game in {_game.Scenario.Title} (round {_game.Round}) will be lost.", "Abandon",
                () => { _inProgress = false; ShowMenu(MenuPage.Scenario); });
        else
            ShowMenu(MenuPage.Scenario);
    }

    private void MenuOptions_Click(object sender, RoutedEventArgs e) => ShowMenu(MenuPage.Options);

    private void MenuBack_Click(object sender, RoutedEventArgs e) =>
        ShowMenu(_menuPage == MenuPage.Options ? _optionsReturn : MenuPage.Main);

    private void PauseMainMenu_Click(object sender, RoutedEventArgs e) => ShowMenu(MenuPage.Main);

    private void Restart_Click(object sender, RoutedEventArgs e) =>
        Confirm("Restart this night?", $"You'll start {_game.Scenario.Title} again from round 1.", "Restart",
            () => { StartNewGame(_game.Scenario, _game.Difficulty); HideMenu(); });

    private void Quit_Click(object sender, RoutedEventArgs e)
    {
        if (_inProgress && !_game.IsOver)
            Confirm("Quit to desktop?", "Your current night will be lost.", "Quit", () => Application.Current.Shutdown());
        else
            Application.Current.Shutdown();
    }

    private void ConfirmYes_Click(object sender, RoutedEventArgs e)
    {
        ConfirmPanel.Visibility = Visibility.Collapsed;
        var action = _confirmAction;
        _confirmAction = null;
        action?.Invoke();
    }

    private void ConfirmNo_Click(object sender, RoutedEventArgs e)
    {
        ConfirmPanel.Visibility = Visibility.Collapsed;
        _confirmAction = null;
    }

    private void Begin_Click(object sender, RoutedEventArgs e)
    {
        StartNewGame(_pickScenario, _pickDifficulty);
        HideMenu();
    }

    // ------------------------------------------------------------------ scenario select

    /// <summary>Swap the board behind the menu to the chosen scenario.</summary>
    private void PreviewScenario(ScenarioDef scenario)
    {
        if (_inProgress) return;
        if (_game.Scenario == scenario) return;
        _game = new Game(null, _pickDifficulty, scenario);
        _logShown = 0;
        LogPanel.Children.Clear();
        BuildScene();
        BuildPortraits();
        Render();
    }

    private void RenderScenarioCards()
    {
        ScenarioCards.Children.Clear();
        // Lay the cards out in two rows (three if there are only a few), as wide as fits the 1600-wide screen.
        int count = Scenarios.All.Count;
        int columns = count <= 3 ? count : (int)Math.Ceiling(count / 2.0);
        double cardWidth = Math.Min(360, 1440.0 / columns - 16);
        bool compact = count > 6;
        ScenarioCards.MaxWidth = columns * (cardWidth + 16) + 1;
        foreach (var sc in Scenarios.All)
        {
            var scenario = sc;
            bool selected = sc == _pickScenario;
            var theme = Art.ThemeFor(sc.Id);

            var art = new Grid { Height = compact ? 92 : 104 };
            art.ColumnDefinitions.Add(new ColumnDefinition());
            art.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            art.ColumnDefinitions.Add(new ColumnDefinition());
            var you = new Viewbox { Child = Art.Survivor(sc.Id).Root, Height = compact ? 66 : 76, VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(0, 0, 0, 8) };
            var them = new Viewbox { Child = Art.Killer(sc.Id).Root, Height = compact ? 82 : 94, VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(0, 0, 0, 6) };
            var vs = new TextBlock { Text = "vs", FontFamily = new FontFamily("Georgia"), FontStyle = FontStyles.Italic, FontSize = 18, Foreground = Art.Dim, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(vs, 1);
            Grid.SetColumn(them, 2);
            art.Children.Add(you);
            art.Children.Add(vs);
            art.Children.Add(them);

            var artFrame = new Border
            {
                CornerRadius = new CornerRadius(8),
                ClipToBounds = true,
                Background = new LinearGradientBrush(Art.C(theme.LandTop), Art.C(theme.Board), 90),
                Child = art,
                Margin = new Thickness(0, 0, 0, 8)
            };

            var body = new StackPanel();
            body.Children.Add(artFrame);
            body.Children.Add(new TextBlock { Text = sc.Title, FontFamily = new FontFamily("Georgia"), FontSize = compact ? 20 : 22, FontWeight = FontWeights.Bold, Foreground = Art.Warm, TextTrimming = TextTrimming.CharacterEllipsis });
            body.Children.Add(new TextBlock { Text = sc.Tagline, FontFamily = new FontFamily("Georgia"), FontStyle = FontStyles.Italic, FontSize = 12.5, Foreground = Art.Dim, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 1, 0, 6) });
            var who = new TextBlock { TextWrapping = TextWrapping.Wrap, FontSize = 12.5, Margin = new Thickness(0, 0, 0, 4) };
            who.Inlines.Add(new System.Windows.Documents.Run("You  ") { Foreground = Art.Dim });
            who.Inlines.Add(new System.Windows.Documents.Run(sc.Survivor.Name) { Foreground = Art.Accent, FontWeight = FontWeights.Bold });
            who.Inlines.Add(new System.Windows.Documents.Run($", {sc.Survivor.Role.ToLowerInvariant()}") { Foreground = Art.Dim });
            body.Children.Add(who);
            var foe = new TextBlock { TextWrapping = TextWrapping.Wrap, FontSize = 12.5, Margin = new Thickness(0, 0, 0, 4) };
            foe.Inlines.Add(new System.Windows.Documents.Run("Hunted by  ") { Foreground = Art.Dim });
            foe.Inlines.Add(new System.Windows.Documents.Run(sc.Killer.Name) { Foreground = Art.Danger, FontWeight = FontWeights.Bold });
            body.Children.Add(foe);
            // Map features worth knowing before you pick: locked doors, a vehicle, indoors.
            var features = new List<string>();
            if (theme.Indoor) features.Add("Indoors");
            if (sc.LockedEdges.Length > 0) features.Add(sc.LockedEdges.Length == 1 ? "1 locked door" : $"{sc.LockedEdges.Length} locked doors");
            if (sc.VehicleName is not null) features.Add($"{sc.VehicleName} (moves {sc.VehicleRange})");
            body.Children.Add(new TextBlock
            {
                Text = $"Lead the {sc.Victims} to the {sc.SanctuaryName}." + (features.Count > 0 ? "\n" + string.Join("  ·  ", features) : ""),
                FontSize = 12, Foreground = features.Count > 0 ? Art.Accent : Art.Dim, TextWrapping = TextWrapping.Wrap
            });

            var card = new Button
            {
                Style = (Style)FindResource("ActionButton"),
                Width = cardWidth,
                Margin = new Thickness(8, 0, 8, 14),
                Padding = new Thickness(12, 10, 12, 10),
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                Background = selected ? Art.Hex("#1E2E2A") : Art.Hex("#121E22"),
                BorderBrush = selected ? Art.Warm : Art.Edge,
                BorderThickness = new Thickness(selected ? 3 : 1),
                Content = body,
                ToolTip = $"{sc.Blurb}\n\n{sc.Survivor.Name}: {sc.Survivor.Bio}\n\n{sc.Killer.Name}: {sc.Killer.Bio}"
            };
            if (selected)
                card.Effect = new DropShadowEffect { Color = Art.C("#E8C26A"), BlurRadius = 24, ShadowDepth = 0, Opacity = 0.55 };
            card.Click += (_, _) =>
            {
                _pickScenario = scenario;
                PreviewScenario(scenario);
                RenderScenarioCards();
            };
            card.MouseDoubleClick += (_, _) => Begin_Click(card, new RoutedEventArgs());
            ScenarioCards.Children.Add(card);
        }
        BeginButton.Content = $"Begin: {_pickScenario.Title}";
    }

    private void RenderDifficultyPick()
    {
        PickEasy.IsChecked = _pickDifficulty == Difficulty.Easy;
        PickNormal.IsChecked = _pickDifficulty == Difficulty.Normal;
        PickHard.IsChecked = _pickDifficulty == Difficulty.Hard;
        PickDifficultyText.Text = _pickDifficulty switch
        {
            Difficulty.Easy => "More health for you, less for the killer.",
            Difficulty.Hard => "Less Time every round. For people who've survived a night or two.",
            _ => "The night as intended."
        };
    }

    private void PickDifficulty_Click(object sender, RoutedEventArgs e)
    {
        _pickDifficulty = PickEasy.IsChecked == true ? Difficulty.Easy : PickHard.IsChecked == true ? Difficulty.Hard : Difficulty.Normal;
        RenderDifficultyPick();
    }

    // ------------------------------------------------------------------ options

    private void LoadOptionControls()
    {
        OptEasy.IsChecked = _settings.Difficulty == Difficulty.Easy;
        OptNormal.IsChecked = _settings.Difficulty == Difficulty.Normal;
        OptHard.IsChecked = _settings.Difficulty == Difficulty.Hard;
        OptAnimNormal.IsChecked = !_settings.FastAnimations;
        OptAnimFast.IsChecked = _settings.FastAnimations;
        OptAmbient.IsChecked = _settings.AmbientEffects;
        OptHints.IsChecked = _settings.ShowHints;
        OptFullscreen.IsChecked = _settings.Fullscreen;
        BuildWindowSizeOptions();
        OptMute.IsChecked = _settings.Muted;
        OptAmbientSound.IsChecked = _settings.AmbientSound;
        _loadingOptions = true;
        OptVolume.Value = Math.Round(_settings.Volume * 100);
        _loadingOptions = false;
        OptVolumeText.Text = $"{OptVolume.Value:0}%";
        OptionsSaved.Text = $"Settings are saved automatically to {Settings.Location}";
    }

    private void Option_Changed(object sender, RoutedEventArgs e)
    {
        _settings.Difficulty = OptEasy.IsChecked == true ? Difficulty.Easy : OptHard.IsChecked == true ? Difficulty.Hard : Difficulty.Normal;
        _settings.FastAnimations = OptAnimFast.IsChecked == true;
        _settings.AmbientEffects = OptAmbient.IsChecked == true;
        _settings.ShowHints = OptHints.IsChecked == true;
        _settings.Fullscreen = OptFullscreen.IsChecked == true;
        _settings.Muted = OptMute.IsChecked == true;
        _settings.AmbientSound = OptAmbientSound.IsChecked == true;
        Sound.SetMuted(_settings.Muted);
        Sound.SetAmbientOn(_settings.AmbientSound);
        RenderMuteButton();

        Anim.Speed = _settings.FastAnimations ? 0.4 : 1.0;
        ApplyAmbientSetting();
        bool sizeChanged = sender is RadioButton { Tag: string key } && key != _settings.WindowSize;
        if (sender is RadioButton { Tag: string picked }) _settings.WindowSize = picked;
        bool fullscreenChanged = (WindowStyle == WindowStyle.None) != _settings.Fullscreen;
        if (sizeChanged || fullscreenChanged) ApplyDisplay();
        BuildWindowSizeOptions();
        HintText.Visibility = _settings.ShowHints ? Visibility.Visible : Visibility.Collapsed;

        OptionsSaved.Text = _settings.Save()
            ? $"Saved to {Settings.Location}"
            : "Couldn't save settings. They'll apply until you close the game.";
    }

    private bool _loadingOptions;

    private void Volume_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_loadingOptions || OptVolumeText is null) return;
        OptVolumeText.Text = $"{OptVolume.Value:0}%";
        _settings.Volume = OptVolume.Value / 100.0;
        Sound.SetVolume(_settings.Volume);
        _settings.Save();
    }

    /// <summary>Let the player hear the new volume when they let go of the slider.</summary>
    private void Volume_Released(object sender, System.Windows.Input.MouseButtonEventArgs e) => Sound.Play("card_buy");

    // ------------------------------------------------------------------ window size

    /// <summary>One toggle per window size. Sizes bigger than this screen are disabled; all are disabled in full screen.</summary>
    private void BuildWindowSizeOptions()
    {
        OptSizes.Children.Clear();
        var (dpiX, dpiY) = DpiScale();
        var work = SystemParameters.WorkArea;
        foreach (var (w, h) in Settings.WindowSizes)
        {
            string key = Settings.SizeKey(w, h);
            bool fits = w / dpiX <= work.Width && h / dpiY <= work.Height;
            var toggle = new RadioButton
            {
                GroupName = "OptSize",
                Style = (Style)FindResource("ModeToggle"),
                Width = 98,
                Margin = new Thickness(0, 0, 6, 6),
                Tag = key,
                IsChecked = key == _settings.WindowSize,
                IsEnabled = fits && !_settings.Fullscreen,
                ToolTip = fits ? $"Window {w} × {h} pixels" : "Bigger than this screen",
                Content = new TextBlock { Text = $"{w} × {h}", HorizontalAlignment = HorizontalAlignment.Center }
            };
            toggle.Click += Option_Changed;
            OptSizes.Children.Add(toggle);
        }
        OptSizeNote.Text = _settings.Fullscreen
            ? "Full screen fills the whole screen; the game scales to fit."
            : "The game scales to fit the window, so you can also drag the edges to any size.";
    }

    private (double X, double Y) DpiScale()
    {
        var dpi = VisualTreeHelper.GetDpi(this);
        return (dpi.DpiScaleX > 0 ? dpi.DpiScaleX : 1, dpi.DpiScaleY > 0 ? dpi.DpiScaleY : 1);
    }

    /// <summary>Full screen, or a window whose inside is exactly the chosen size in screen pixels.
    /// The content is a Viewbox over a 1600x900 layout, so any size shows the whole game.</summary>
    private void ApplyDisplay()
    {
        ApplyFullscreen();
        if (_settings.Fullscreen) return;

        var (w, h) = _settings.ParsedWindowSize;
        var (dpiX, dpiY) = DpiScale();
        double cw = w / dpiX, ch = h / dpiY; // the client area we want, in WPF units
        var work = SystemParameters.WorkArea;
        if (cw > work.Width || ch > work.Height)
        {
            WindowState = WindowState.Maximized; // too big for this screen: use all of it
            return;
        }

        WindowState = WindowState.Normal;
        // Let WPF add the title bar and borders by sizing to the content, then hand control back to the user.
        RootScale.Width = cw;
        RootScale.Height = ch;
        SizeToContent = SizeToContent.WidthAndHeight;
        Dispatcher.BeginInvoke(new Action(() =>
        {
            SizeToContent = SizeToContent.Manual;
            RootScale.Width = double.NaN;
            RootScale.Height = double.NaN;
            Left = work.Left + Math.Max(0, (work.Width - ActualWidth) / 2);
            Top = work.Top + Math.Max(0, (work.Height - ActualHeight) / 2);
        }), System.Windows.Threading.DispatcherPriority.Loaded);
    }

    private void ApplyFullscreen()
    {
        if (_settings.Fullscreen)
        {
            if (WindowStyle == WindowStyle.None && WindowState == WindowState.Maximized) return;
            WindowState = WindowState.Normal;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            WindowState = WindowState.Maximized;
        }
        else
        {
            if (WindowStyle != WindowStyle.None) return;
            WindowStyle = WindowStyle.SingleBorderWindow;
            ResizeMode = ResizeMode.CanResize;
            WindowState = WindowState.Normal;
        }
    }
}
