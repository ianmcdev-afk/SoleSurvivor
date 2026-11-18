using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using SoleSurvivor.Core;
using Ellipse = System.Windows.Shapes.Ellipse;

namespace SoleSurvivor.App;

/// <summary>The right-hand panel: portraits, stats, actions, dice tray, cards and items. Also the journal and game-over screen.</summary>
public partial class MainWindow
{
    private DiceRoll? _trayRoll;
    private bool _trayEnemy;
    private readonly Random _uiRng = new();

    private void BuildPortraits()
    {
        var sc = _game.Scenario;
        SurvivorPortraitHost.Child = Art.Portrait(Art.Survivor(sc.Id), Art.Warm, Art.Hex("#1F3A40"));
        TidewifePortraitHost.Child = Art.Portrait(Art.Killer(sc.Id), Art.Danger, Art.Hex("#10262A"));
        SurvivorName.Text = sc.Survivor.Name;
        SurvivorName.ToolTip = $"{sc.Survivor.Role}. {sc.Survivor.Bio}";
        KillerName.Text = sc.Killer.Name;
        KillerName.ToolTip = sc.Killer.Bio;
        HeaderTitle.Text = sc.Title.ToUpperInvariant();
        HeaderSubtitle.Text = sc.Tagline;
    }

    // ------------------------------------------------------------------ status

    private static void Pips(Panel panel, int filled, int total, string glyph, Brush on, Brush off, double size)
    {
        panel.Children.Clear();
        for (int i = 0; i < total; i++)
            panel.Children.Add(new TextBlock
            {
                Text = glyph,
                FontSize = size,
                Foreground = i < filled ? on : off,
                Margin = new Thickness(0, 0, 3, 0),
                VerticalAlignment = VerticalAlignment.Center
            });
    }

    private void RenderStatus()
    {
        var g = _game;
        Pips(HealthPips, g.Health, g.SurvivorMaxHealth, "♥", Art.Danger, Art.Empty, 19);
        Pips(TimePips, g.Time, Game.MaxTime, "◆", Art.Accent, Art.Empty, 14);
        if (g.Hope == 0)
        {
            HopePips.Children.Clear();
            HopePips.Children.Add(new TextBlock { Text = $"none yet: save {g.Scenario.Victims}", Foreground = Art.Dim, FontSize = 11.5 });
        }
        else
        {
            Pips(HopePips, g.Hope, Math.Min(g.Hope, 10), "✦", Art.Warm, Art.Empty, 15);
            if (g.Hope > 10) HopePips.Children.Add(new TextBlock { Text = $"({g.Hope})", Foreground = Art.Warm });
        }
        SurvivorWhere.Text = $"At the {Board.Locations[g.SurvivorLocation].Name}";

        RenderHorrorTrack(g);
        RenderKillerBoard(g);
        RenderEvents(g);

        double ratio = g.TidewifeMaxHealth == 0 ? 0 : (double)g.TidewifeHealth / g.TidewifeMaxHealth;
        TidewifeBar.RenderTransform = new ScaleTransform(Math.Clamp(ratio, 0, 1), 1);
        TidewifeText.Text = $"{g.TidewifeHealth} / {g.TidewifeMaxHealth}";
        TidewifeWhere.Text = $"At the {Board.Locations[g.TidewifeLocation].Name}"
                             + (g.TidewifeHere ? ", right beside you!" : g.TidewifeNear ? ", one step away." : ".")
                             + (g.Scenario.HasSwarm
                                 ? $"\n{g.SwarmTotal} {g.Scenario.SwarmNames} on the board (at most {g.Scenario.SwarmMax})"
                                   + (g.SwarmHere > 0 ? $", {g.SwarmHere} with you." : ".")
                                 : "");
        TidewifeWhere.Foreground = g.TidewifeHere ? Art.Danger : g.TidewifeNear ? Art.Warm : Art.Dim;
        VillagerText.Text = g.InFinale
            ? $"Saved {g.VillagersSaved} · lost {g.VillagersLost}. {g.Scenario.FinaleTakes}."
            : $"Saved {g.VillagersSaved} · lost {g.VillagersLost} · still out there {g.VillagersRemaining}. Each one {g.Scenario.Killer.SheLower} kills raises {g.Scenario.Killer.Her} Bloodlust.";
    }

    // ------------------------------------------------------------------ horror track

    /// <summary>
    /// The Horror track: Dread 1-8 in three bands, each showing how many Horror dice you roll in it
    /// (Uneasy 3, Afraid 2, Terror 1). The marker sits on the current Dread.
    /// </summary>
    private void RenderHorrorTrack(Game g)
    {
        var bands = new (int From, int To, string Name, string Colour)[]
        {
            (1, 2, "Uneasy", "#6E8B5A"),
            (3, 5, "Afraid", "#C9893E"),
            (6, 8, "Terror", "#C8553D"),
        };
        int current = g.HorrorDice;

        DreadGauge.Children.Clear();
        DreadGauge.ColumnDefinitions.Clear();
        DreadGauge.RowDefinitions.Clear();
        DreadGauge.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        DreadGauge.RowDefinitions.Add(new RowDefinition { Height = new GridLength(24) });
        for (int i = 1; i <= Game.MaxDread; i++) DreadGauge.ColumnDefinitions.Add(new ColumnDefinition());

        // Zone headers: how many dice each band gives you.
        foreach (var (from, to, name, colour) in bands)
        {
            int dice = Game.HorrorDiceFor(from, false);
            bool here = !g.InFinale && g.Dread >= from && g.Dread <= to || (g.InFinale && to == Game.MaxDread);
            // Dice on one line, the band's name underneath, both centred: three dice fit even in the two-cell Uneasy band.
            var dicePanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
            for (int d = 0; d < dice; d++)
                dicePanel.Children.Add(new TextBlock
                {
                    Text = "⚄",
                    FontSize = here ? 16 : 14,
                    Margin = new Thickness(0.5, -2, 0.5, -2),
                    Foreground = here ? Art.Hex(colour) : Art.Hex("#4A5E5E")
                });
            var label = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
            label.Children.Add(dicePanel);
            label.Children.Add(new TextBlock
            {
                Text = name,
                FontSize = 10,
                FontWeight = here ? FontWeights.Bold : FontWeights.Normal,
                Foreground = here ? Art.Hex(colour) : Art.Hex("#5F7676"),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, -1, 0, 0)
            });
            var zone = new Border
            {
                Margin = new Thickness(0, 0, 3, 3),
                Padding = new Thickness(0, 1, 0, 1),
                CornerRadius = new CornerRadius(4, 4, 0, 0),
                Background = here ? new SolidColorBrush(Color.FromArgb(40, Art.C(colour).R, Art.C(colour).G, Art.C(colour).B)) : Brushes.Transparent,
                BorderBrush = here ? Art.Hex(colour) : Art.Hex("#2A3A40"),
                BorderThickness = new Thickness(0, 0, 0, 2),
                Child = label,
                ToolTip = $"{name} (Dread {from}-{to}): you roll {dice} Horror {(dice == 1 ? "die" : "dice")}"
            };
            Grid.SetRow(zone, 0);
            Grid.SetColumn(zone, from - 1);
            Grid.SetColumnSpan(zone, to - from + 1);
            DreadGauge.Children.Add(zone);
        }

        // Cells 1-8, lit up to the current Dread, with a marker on the current one.
        for (int i = 1; i <= Game.MaxDread; i++)
        {
            var band = bands.First(b => i >= b.From && i <= b.To);
            Color c = Art.C(band.Colour);
            bool lit = g.InFinale || i <= g.Dread;
            bool marker = !g.InFinale && i == g.Dread;
            var cell = new Border
            {
                Margin = new Thickness(0, 0, 3, 0),
                CornerRadius = new CornerRadius(3),
                Background = lit ? new LinearGradientBrush(c, Color.FromArgb(255, (byte)(c.R * 0.6), (byte)(c.G * 0.6), (byte)(c.B * 0.6)), 90) : Art.Empty,
                BorderBrush = marker ? Brushes.White : i == Game.MaxDread ? Art.Dim : Brushes.Transparent,
                BorderThickness = new Thickness(marker ? 2 : i == Game.MaxDread ? 1 : 0),
                ToolTip = i == Game.MaxDread ? $"At {Game.MaxDread}: the {g.Scenario.FinaleName} (finale)" : $"Dread {i}",
                Child = new TextBlock
                {
                    Text = marker ? "▼" : i == Game.MaxDread ? "≋" : i.ToString(),
                    FontSize = marker ? 11 : 9.5,
                    FontWeight = FontWeights.Bold,
                    Foreground = lit ? Brushes.Black : Art.Hex("#5F7676"),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
            if (marker)
                cell.Effect = new System.Windows.Media.Effects.DropShadowEffect { Color = Colors.White, BlurRadius = 8, ShadowDepth = 0, Opacity = 0.7 };
            Grid.SetRow(cell, 1);
            Grid.SetColumn(cell, i - 1);
            DreadGauge.Children.Add(cell);
        }

        DreadText.Text = g.InFinale ? g.DreadLabel : $"{g.DreadLabel}  ·  Dread {g.Dread}/{Game.MaxDread}";
        DreadText.Foreground = g.InFinale || g.Dread >= 6 ? Art.Danger : g.Dread >= 3 ? Art.Hex("#C9893E") : Art.TextColor;
        ThresholdText.Text = $"Horror roll: {current} {(current == 1 ? "die" : "dice")}, plus cards and gear. ★ 5-6 succeeds; ▮▮ 3 for 2 cards, ▮ 4 for 1 card.";
    }

    // ------------------------------------------------------------------ events

    private static Brush EventTone(EventCardDef e) => e.Tone > 0 ? Art.Accent : e.Tone < 0 ? Art.Danger : Art.Warm;

    /// <summary>Ongoing events in play, each with its effect and when it is discarded.</summary>
    private void RenderEvents(Game g)
    {
        EventsList.Children.Clear();
        EventsHeader.Text = $"EVENTS IN PLAY · {g.EventCardsLeft} left in the deck";
        if (g.ActiveEvents.Count == 0)
        {
            EventsList.Children.Add(new TextBlock { Text = "None right now.", FontSize = 11.5, Foreground = Art.Dim });
            return;
        }
        foreach (var e in g.ActiveEvents)
        {
            string status = e.Card.Kind switch
            {
                EventKind.Adrenaline or EventKind.Blackout => e.RoundsLeft > 0 ? $"{e.RoundsLeft} more round{(e.RoundsLeft == 1 ? "" : "s")}" : "last round",
                EventKind.Companion or EventKind.Wounded when e.Location >= 0 => $"at the {Board.Locations[e.Location].Name}",
                EventKind.Trap when e.Location >= 0 => $"set at the {Board.Locations[e.Location].Name}",
                _ => "until discarded"
            };
            var head = new DockPanel();
            var st = new TextBlock { Text = status, FontSize = 11, Foreground = Art.Dim, VerticalAlignment = VerticalAlignment.Center };
            DockPanel.SetDock(st, Dock.Right);
            head.Children.Add(st);
            head.Children.Add(new TextBlock
            {
                Text = $"{EventCards.Icon(e.Card)}  {e.Card.Name}", FontWeight = FontWeights.Bold, FontSize = 12.5, Foreground = EventTone(e.Card)
            });
            var block = new StackPanel { Margin = new Thickness(0, 3, 0, 3), ToolTip = e.Card.Flavour };
            block.Children.Add(head);
            block.Children.Add(new TextBlock { Text = e.Card.Text, FontSize = 11, Foreground = Art.TextColor, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(16, 1, 0, 0) });
            if (e.Card.Discard is not null)
                block.Children.Add(new TextBlock { Text = e.Card.Discard, FontSize = 10.5, FontStyle = FontStyles.Italic, Foreground = Art.Dim, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(16, 0, 0, 0) });
            EventsList.Children.Add(block);
        }
    }

    // ------------------------------------------------------------------ killer board

    /// <summary>
    /// The killer board's Bloodlust track, bottom (left) to top (right). Each column shows the killer's
    /// move and attack at that level, with a special-effect icon above. The marker sits on the current level.
    /// </summary>
    private void RenderKillerBoard(Game g)
    {
        var track = g.Scenario.BloodlustTrack;
        var blood = Art.C("#C8553D");
        BloodlustGrid.Children.Clear();
        BloodlustGrid.ColumnDefinitions.Clear();
        BloodlustGrid.RowDefinitions.Clear();
        BloodlustGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(22) });
        foreach (var _ in track) BloodlustGrid.ColumnDefinitions.Add(new ColumnDefinition());
        BloodlustGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(16) });
        BloodlustGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(20) });
        BloodlustGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(20) });

        void Cell(UIElement e, int row, int col) { Grid.SetRow(e, row); Grid.SetColumn(e, col); BloodlustGrid.Children.Add(e); }
        TextBlock Glyph(string text, Brush fg, double size = 12, string? tip = null) => new()
        {
            Text = text, Foreground = fg, FontSize = size, FontWeight = FontWeights.Bold, ToolTip = tip,
            HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center
        };

        Cell(Glyph("➤", Art.Dim, 12, "Move: spaces the killer moves when a card says move"), 1, 0);
        Cell(Glyph("⚔", Art.Dim, 12, "Attack: damage to you, or victims killed"), 2, 0);

        for (int i = 0; i < track.Length; i++)
        {
            var lvl = track[i];
            bool reached = i <= g.Bloodlust;
            bool marker = i == g.Bloodlust;
            string tip = $"Bloodlust {i}: moves {lvl.Move}, attacks for {lvl.Attack}"
                         + (lvl.Effect != BloodlustEffect.None ? $". On reaching it: {Bloodlust.Describe(lvl.Effect, g.Scenario)}" : "")
                         + (i == track.Length - 1 ? $" Past the top, every kill heals {g.Scenario.Killer.Her} {Bloodlust.FinalEffectHeal}." : "");

            // Special-effect icon above the column (skull = Dread +1, heart = Frenzy).
            if (lvl.Effect != BloodlustEffect.None)
                Cell(Glyph(Bloodlust.Icon(lvl.Effect), reached ? Art.Hex("#8A6A6A") : Art.Danger, 11, tip), 0, i + 1);
            else if (i == track.Length - 1)
                Cell(Glyph("↺", Art.Danger, 11, tip), 0, i + 1);

            // The column: lit up to the current level, marker on the current one.
            var col = new Border
            {
                Margin = new Thickness(1, 0, 1, 0),
                CornerRadius = new CornerRadius(3),
                Background = marker ? new SolidColorBrush(blood)
                    : reached ? new SolidColorBrush(Color.FromArgb(110, blood.R, blood.G, blood.B))
                    : Art.Hex("#2A1E1E"),
                BorderBrush = marker ? Brushes.White : Brushes.Transparent,
                BorderThickness = new Thickness(marker ? 1.5 : 0),
                ToolTip = tip
            };
            if (marker)
                col.Effect = new System.Windows.Media.Effects.DropShadowEffect { Color = blood, BlurRadius = 10, ShadowDepth = 0, Opacity = 0.9 };
            Grid.SetRowSpan(col, 2);
            Cell(col, 1, i + 1);

            Brush fg = marker ? Brushes.White : reached ? Art.Hex("#E8D0C8") : Art.Hex("#8A7070");
            Cell(Glyph(lvl.Move.ToString(), fg, marker ? 12.5 : 11, tip), 1, i + 1);
            Cell(Glyph(lvl.Attack.ToString(), fg, marker ? 12.5 : 11, tip), 2, i + 1);
        }

        string she = g.Scenario.Killer.SheLower;
        BloodlustText.Text = $"{g.Bloodlust} / {g.BloodlustMax}";
        string next = g.Bloodlust < g.BloodlustMax && track[g.Bloodlust + 1].Effect != BloodlustEffect.None
            ? $" Next kill: {Bloodlust.Describe(track[g.Bloodlust + 1].Effect, g.Scenario)}"
            : g.Bloodlust >= g.BloodlustMax ? $" Each kill now heals {g.Scenario.Killer.Her} {Bloodlust.FinalEffectHeal}." : "";
        KillerStatsText.Text = g.InFinale
            ? $"{g.Scenario.FinaleName}: {she} moves {g.KillerMove} and attacks for {g.KillerAttack} (board +1 each)."
            : $"{g.Scenario.Killer.ShortName} moves {g.KillerMove} and attacks for {g.KillerAttack}." + next;
    }

    // ------------------------------------------------------------------ actions

    private static StackPanel Tile(string glyph, string name, string line1, string line2, Brush glyphBrush)
    {
        var panel = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
        panel.Children.Add(new TextBlock { Text = glyph, FontSize = 24, Foreground = glyphBrush, HorizontalAlignment = HorizontalAlignment.Center });
        panel.Children.Add(new TextBlock { Text = name, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center });
        panel.Children.Add(new TextBlock { Text = line1, FontSize = 11, Foreground = Art.Accent, HorizontalAlignment = HorizontalAlignment.Center });
        panel.Children.Add(new TextBlock { Text = line2, FontSize = 10.5, Foreground = Art.Dim, HorizontalAlignment = HorizontalAlignment.Center, TextAlignment = TextAlignment.Center, TextWrapping = TextWrapping.Wrap });
        return panel;
    }

    private void RenderActions()
    {
        var g = _game;
        string vehicle = g.HasVehicle ? $" · {g.Scenario.VehicleName}: up to {g.Scenario.VehicleRange} spaces" : "";
        WalkModeText.Text = g.MoveTokens > 0 ? $"{Plural(g.MoveTokens, "move", "moves")} ready{vehicle}" : "play Walk or Sprint";
        EscortModeText.Text = g.InFinale ? "no one left to save"
            : g.EscortTokens > 0 ? $"{Plural(g.EscortTokens, "escort", "escorts")} ready · up to {g.EscortCapacity} {g.Scenario.Victims}"
            : "play Lead or Shepherd";
        EscortMode.IsEnabled = !g.InFinale && !g.IsOver;
        WalkMode.IsEnabled = !g.IsOver;
        if (g.InFinale && EscortMode.IsChecked == true) WalkMode.IsChecked = true;
        // Playing an escort card switches to Escort; playing a move card switches back.
        if (g.EscortTokens > 0 && g.MoveTokens == 0 && EscortMode.IsEnabled) EscortMode.IsChecked = true;
        else if (g.MoveTokens > 0 && g.EscortTokens == 0) WalkMode.IsChecked = true;

        var tokens = new List<string>();
        if (g.Barricaded) tokens.Add("▦ Door barred");
        if (g.HoldingBreath) tokens.Add("◌ Holding breath");
        if (g.Repelled) tokens.Add("❄ Salted");
        if (g.FogThisTurn) tokens.Add("☁ Fog: −1 search die");
        if (g.SteelYourself) tokens.Add("✦ Steeled: +1 die on next roll");
        TokensText.Text = string.Join("    ", tokens);
        TokensText.Visibility = tokens.Count == 0 ? Visibility.Collapsed : Visibility.Visible;

        RenderPhaseTracker();

        SteelCheck.IsEnabled = g.Hope > 0 && !g.IsOver;
        if (g.Hope == 0) g.SteelYourself = false;
        SteelCheck.IsChecked = g.SteelYourself;

        bool planning = g.InPlanning;
        EndTurnTitle.Text = planning ? "END TURN" : "END ACTIONS";
        EndTurnSub.Text = planning ? $"press E · {g.Scenario.Killer.ShortName}'s turn next" : "press E · then Planning";
        EndTurnButton.IsEnabled = (g.CanEndActions || g.CanEndTurn) && !_busy;
    }

    /// <summary>Action › Planning › Killer, with the current step lit.</summary>
    private void RenderPhaseTracker()
    {
        var g = _game;
        int current = g.IsOver || g.AwaitingStart ? -1 : _stage != Stage.YourTurn ? 2 : g.InPlanning ? 1 : 0;
        string[] names = { "1 ACTION", "2 PLANNING", "3 " + g.Scenario.Killer.ShortName.ToUpperInvariant() };
        PhaseTracker.Children.Clear();
        for (int i = 0; i < names.Length; i++)
        {
            bool on = i == current;
            PhaseTracker.Children.Add(new Border
            {
                Margin = new Thickness(i == 0 ? 0 : 2, 0, i == 2 ? 0 : 2, 0),
                Padding = new Thickness(2, 4, 2, 4),
                CornerRadius = new CornerRadius(5),
                Background = on ? (i == 2 ? Art.Hex("#3A1C18") : Art.Hex("#2E3A2A")) : Art.Hex("#0E1C21"),
                BorderBrush = on ? (i == 2 ? Art.Danger : Art.Warm) : Art.Edge,
                BorderThickness = new Thickness(on ? 2 : 1),
                Child = new TextBlock
                {
                    Text = names[i], FontSize = 11, FontWeight = on ? FontWeights.Bold : FontWeights.Normal,
                    Foreground = on ? (i == 2 ? Art.Danger : Art.Warm) : Art.Dim,
                    HorizontalAlignment = HorizontalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis
                }
            });
        }
        PhaseText.Text = current switch
        {
            0 => $"Play cards from your hand: they're how you move, escort, search, fight and rest. Playing is free. You have {g.Time} Time for Planning.",
            1 => $"Buy cards from the tableau for next turn with your {g.Time} Time (unspent Time is lost). Cards you used this turn come back after it.",
            2 => $"{g.Scenario.Killer.Name}'s turn.",
            _ => ""
        };
    }

    // ------------------------------------------------------------------ dice tray

    /// <summary>How a Horror die is drawn: still tumbling, or landed on a blank, partial or success face.
    /// Converted = a partial turned into a success by discarding cards.</summary>
    private enum DieLook { Rolling, Blank, Partial, Success, Converted }

    private static DieLook LookFor(int value) => DiceRoll.Face(value) switch
    {
        DieFace.Success => DieLook.Success,
        DieFace.Partial => DieLook.Partial,
        _ => DieLook.Blank
    };

    /// <summary>
    /// A red Horror die like the board game's: 5-6 show a star (success); a 3 shows two cards and a 4 shows one card
    /// (partials: discard that many cards to make it a success); 1-2 are blank. The number sits small in the corner.
    /// <paramref name="chosen"/> marks a partial picked in the trade-in panel.
    /// </summary>
    private static Border Die(int value, DieLook look, double angle = 0, double size = 40, bool chosen = false)
    {
        double k = size / 40;
        var canvas = new Canvas { Width = 40, Height = 40 };
        var white = Brushes.White;
        bool star = look is DieLook.Success or DieLook.Converted;
        bool cards = look is DieLook.Partial || (look == DieLook.Rolling && value is 3 or 4);
        if (look == DieLook.Rolling && value >= 5) star = true;

        if (star)
        {
            var poly = new System.Windows.Shapes.Polygon { Fill = white };
            for (int i = 0; i < 10; i++)
            {
                double r = i % 2 == 0 ? 13 : 5.5, a = -Math.PI / 2 + i * Math.PI / 5;
                poly.Points.Add(new Point(20 + r * Math.Cos(a), 20 + r * Math.Sin(a)));
            }
            canvas.Children.Add(poly);
        }
        else if (cards)
        {
            // The trade-in icon: two cards on a 3, one card on a 4. That's how many you discard.
            var icon = DiceRoll.CardsFor(value) == 2
                ? new[] { (11.0, 11.0, -12.0), (19.0, 12.0, 10.0) }
                : new[] { (15.0, 11.5, 0.0) };
            foreach (var (x, y, rot) in icon)
            {
                var c = new System.Windows.Shapes.Rectangle
                {
                    Width = 10, Height = 15, RadiusX = 2, RadiusY = 2, Fill = white, Stroke = Art.Hex("#B8322A"), StrokeThickness = 1,
                    RenderTransform = new RotateTransform(rot, 5, 7.5)
                };
                Canvas.SetLeft(c, x);
                Canvas.SetTop(c, y);
                canvas.Children.Add(c);
            }
        }
        var number = new TextBlock { Text = value.ToString(), FontSize = 9, FontWeight = FontWeights.Bold, Foreground = Art.Hex("#FFE3DE") };
        Canvas.SetLeft(number, 30);
        Canvas.SetTop(number, 27);
        canvas.Children.Add(number);
        if (look == DieLook.Converted)
        {
            // A small cards badge: this success was bought with cards.
            var badge = new TextBlock { Text = DiceRoll.CardsFor(value) == 2 ? "▮▮" : "▮", FontSize = 7, Foreground = Art.Hex("#FFE3DE") };
            Canvas.SetLeft(badge, 3);
            Canvas.SetTop(badge, 29);
            canvas.Children.Add(badge);
        }

        Color top = look == DieLook.Blank ? Art.C("#7A2620") : Art.C("#D23B30");
        Color bottom = look == DieLook.Blank ? Art.C("#4E1814") : Art.C("#8E2018");
        Brush edge = look switch
        {
            DieLook.Success => Art.Accent,
            DieLook.Converted => Art.Warm,
            DieLook.Partial => Art.Hex("#F0B0A8"),
            _ => Art.Hex("#3A120F")
        };
        var die = new Border
        {
            Width = size, Height = size,
            Margin = new Thickness(0, 0, 8 * k, 8 * k),
            CornerRadius = new CornerRadius(8 * k),
            Background = new LinearGradientBrush(top, bottom, 90),
            BorderBrush = chosen ? Art.Warm : edge,
            BorderThickness = new Thickness(chosen || look is DieLook.Success or DieLook.Converted ? 2.5 : look == DieLook.Partial ? 1.8 : 1),
            Child = new Viewbox { Child = canvas },
            RenderTransformOrigin = new Point(0.5, 0.5),
            RenderTransform = new RotateTransform(angle),
            ToolTip = look switch
            {
                DieLook.Success => $"{value}: success",
                DieLook.Converted => $"{value}: partial, made a success by discarding {Plural(DiceRoll.CardsFor(value), "card", "cards")}",
                DieLook.Partial => $"{value}: partial. Discard {Plural(DiceRoll.CardsFor(value), "card", "cards")} to make it a success",
                DieLook.Blank => $"{value}: nothing",
                _ => null
            }
        };
        if (look is DieLook.Success or DieLook.Converted)
            die.Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = look == DieLook.Converted ? Art.C("#E8B634") : Art.C("#6CC3B0"),
                BlurRadius = 12,
                ShadowDepth = 0,
                Opacity = 0.9
            };
        if (look == DieLook.Blank) die.Opacity = 0.8;
        return die;
    }

    /// <summary>The look of each die in a roll; dice bought with cards show as converted.</summary>
    private static List<DieLook> Looks(DiceRoll roll) =>
        roll.Values.Select((v, i) => roll.IsConverted(i) ? DieLook.Converted : LookFor(v)).ToList();

    /// <summary>Tumble the dice for a moment, then land on the real result.</summary>
    private async Task RollDiceAsync(DiceRoll roll, bool enemy)
    {
        _trayRoll = roll;
        _trayEnemy = enemy;
        DiceTitle.Text = roll.Label;
        DiceTitle.Foreground = enemy ? Art.Danger : Art.Warm;
        DiceText.Text = "Rolling…";
        Sound.Play("dice", enemy ? 0.7 : 0.9);
        for (int frame = 0; frame < 8; frame++)
        {
            DicePanel.Children.Clear();
            for (int d = 0; d < roll.Values.Count; d++)
                DicePanel.Children.Add(Die(_uiRng.Next(1, 7), DieLook.Rolling, _uiRng.Next(-25, 26)));
            await Anim.Wait(55);
        }
        RenderDice();
        foreach (var child in DicePanel.Children.OfType<Border>())
        {
            var pop = new ScaleTransform(1.3, 1.3);
            var group = new TransformGroup();
            group.Children.Add(pop);
            child.RenderTransform = group;
            _ = Anim.To(pop, ScaleTransform.ScaleXProperty, 1, 260, Anim.Back);
            _ = Anim.To(pop, ScaleTransform.ScaleYProperty, 1, 260, Anim.Back);
        }
        await Anim.Wait(380);
    }

    private void RenderDice()
    {
        DicePanel.Children.Clear();
        var roll = _trayRoll;
        if (roll is null)
        {
            DiceTitle.Text = "Dice";
            DiceTitle.Foreground = Art.Warm;
            DiceText.Text = "Search and Fight roll Horror dice here.";
            DiceText.ToolTip = "★ 5-6 succeeds. A 3 (two cards) can be bought for 2 cards, a 4 (one card) for 1. 1-2 are blank.";
            return;
        }
        DiceTitle.Text = roll.Label;
        DiceTitle.Foreground = _trayEnemy ? Art.Danger : Art.Warm;
        var looks = Looks(roll);
        for (int i = 0; i < roll.Values.Count; i++)
            DicePanel.Children.Add(Die(roll.Values[i], looks[i]));
        int partialsLeft = roll.Partials - roll.Converted;
        DiceText.Text = $"{roll.Successes} success{(roll.Successes == 1 ? "" : "es")}"
            + (roll.Converted > 0 ? $" ({roll.Converted} bought with cards)" : "")
            + (_game.Pending is not null && ReferenceEquals(_game.Pending.Roll, roll) ? $" · {partialsLeft} partial: trade in cards?" : "");
        DiceText.ToolTip = "★ 5-6 succeeds. A 3 (two cards) can be bought for 2 cards, a 4 (one card) for 1. 1-2 are blank.";
    }

    // ------------------------------------------------------------------ cards

    private static (string Glyph, Color Band, string Kind) CardLook(CardEffect e) => e switch
    {
        CardEffect.Sprint => ("»", Art.C("#2E6B63"), "MOVE"),
        CardEffect.Shepherd => ("♟", Art.C("#2E6B63"), "RESCUE"),
        CardEffect.LanternSignal => ("✺", Art.C("#2E6B63"), "RESCUE"),
        CardEffect.Lure => ("♪", Art.C("#5A3E6B"), "TRICK"),
        CardEffect.WildSwing => ("⚔", Art.C("#7A2E25"), "FIGHT"),
        CardEffect.DesperatePlan => ("☠", Art.C("#7A2E25"), "FIGHT"),
        CardEffect.Rummage => ("⌕", Art.C("#7A5A1E"), "SEARCH"),
        CardEffect.BarTheDoor => ("▦", Art.C("#2E4A6B"), "DEFEND"),
        CardEffect.HoldBreath => ("◌", Art.C("#2E4A6B"), "DEFEND"),
        CardEffect.SteelNerves => ("✦", Art.C("#3E6B2E"), "SUPPORT"),
        CardEffect.PatchUp => ("✚", Art.C("#3E6B2E"), "SUPPORT"),
        CardEffect.Scramble => ("»", Art.C("#55606A"), "FREE"),
        CardEffect.Peek => ("⌕", Art.C("#55606A"), "FREE"),
        CardEffect.Holler => ("♟", Art.C("#55606A"), "FREE"),
        CardEffect.QuickStep => ("»", Art.C("#4A5A58"), "STARTER"),
        CardEffect.Guide => ("♟", Art.C("#4A5A58"), "STARTER"),
        CardEffect.Shove => ("↯", Art.C("#4A5A58"), "STARTER"),
        CardEffect.GlanceAround => ("⌕", Art.C("#4A5A58"), "STARTER"),
        CardEffect.CatchBreath => ("☾", Art.C("#4A5A58"), "STARTER"),
        _ => ("?", Art.C("#444444"), "")
    };

    private const double CardWidth = 98, CardHeight = 150;

    /// <summary>The printed face of a card: coloured header with kind and cost, glyph, name, rules text, optional footer.</summary>
    private static StackPanel CardFace(ActionCardDef card, string? footer, bool enabled)
    {
        var look = CardLook(card.Effect);
        var band = new SolidColorBrush(look.Band);

        var header = new Border
        {
            Background = band,
            CornerRadius = new CornerRadius(5, 5, 0, 0),
            Padding = new Thickness(5, 3, 5, 3),
            Child = new DockPanel
            {
                Children =
                {
                    new Border
                    {
                        MinWidth = 20, Height = 20,
                        Padding = new Thickness(card.Cost == 0 ? 5 : 0, 0, card.Cost == 0 ? 5 : 0, 0),
                        CornerRadius = new CornerRadius(10),
                        Background = Art.Hex("#0B1418"),
                        BorderBrush = Art.Accent,
                        BorderThickness = new Thickness(1),
                        ToolTip = card.Cost == 0 ? "Free" : $"Costs {card.Cost} time to buy",
                        Child = new TextBlock
                        {
                            Text = card.Cost == 0 ? "FREE" : card.Cost.ToString(),
                            Foreground = Art.Accent,
                            FontWeight = FontWeights.Bold,
                            FontSize = 11,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center
                        }
                    }.Also(b => DockPanel.SetDock(b, Dock.Right)),
                    new TextBlock { Text = look.Kind, FontSize = 9, FontWeight = FontWeights.Bold, Foreground = Art.Hex("#E6EFEC"), VerticalAlignment = VerticalAlignment.Center }
                }
            }
        };

        var body = new StackPanel { Margin = new Thickness(6, 4, 6, 3) };
        body.Children.Add(new DockPanel
        {
            Children =
            {
                new TextBlock { Text = look.Glyph, FontSize = 18, Foreground = band, Margin = new Thickness(0, -2, 4, 0), VerticalAlignment = VerticalAlignment.Center }
                    .Also(t => DockPanel.SetDock(t, Dock.Left)),
                new TextBlock { Text = card.Name, FontFamily = new FontFamily("Georgia"), FontWeight = FontWeights.Bold, FontSize = 12, TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center }
            }
        });
        body.Children.Add(new TextBlock { Text = card.Text, TextWrapping = TextWrapping.Wrap, FontSize = 10.5, Foreground = Art.Dim, Margin = new Thickness(0, 3, 0, 3), Height = 42 });
        if (footer is not null)
            body.Children.Add(new TextBlock { Text = footer, FontSize = 10.5, FontWeight = FontWeights.Bold, Foreground = enabled ? Art.Warm : Art.Dim, TextWrapping = TextWrapping.Wrap });

        var face = new StackPanel();
        face.Children.Add(header);
        face.Children.Add(body);
        return face;
    }

    /// <summary>A clickable card (market and starting picks). <paramref name="onClick"/> runs as a game action (animated) unless <paramref name="raw"/> is set.</summary>
    private Button CardButton(ActionCardDef card, string footer, bool enabled, bool inHand, Action onClick, bool raw = false, bool selected = false)
    {
        var button = new Button
        {
            Style = (Style)FindResource("ActionButton"),
            Width = CardWidth,
            Height = CardHeight,
            Padding = new Thickness(0),
            Margin = new Thickness(0, 0, 6, 0),
            VerticalContentAlignment = VerticalAlignment.Top,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            Background = inHand ? Art.Hex("#1E2E26") : Art.Hex("#16282F"),
            BorderBrush = selected ? Art.Accent : inHand && enabled ? Art.Warm : Art.Edge,
            BorderThickness = new Thickness(selected ? 3 : 1),
            Content = CardFace(card, footer, enabled),
            IsEnabled = enabled && !_busy
        };
        button.Click += (_, _) =>
        {
            if (raw) onClick();
            else Perform(onClick);
        };
        if (selected)
            button.Effect = new System.Windows.Media.Effects.DropShadowEffect { Color = Art.C("#6CC3B0"), BlurRadius = 16, ShadowDepth = 0, Opacity = 0.9 };
        return button;
    }

    /// <summary>A card in your hand, with Play and Discard (+1 Time) buttons along the bottom.</summary>
    private Border HandCard(int index)
    {
        var g = _game;
        var card = g.Hand[index];
        bool canPlay = g.CanPlay(index) && !_busy;
        bool canDiscard = g.CanDiscard(index) && !_busy;

        var play = new Button
        {
            Style = (Style)FindResource("ActionButton"),
            Content = "▶ Play",
            Padding = new Thickness(4, 3, 4, 3),
            Margin = new Thickness(0, 0, 3, 0),
            FontSize = 11.5,
            FontWeight = FontWeights.Bold,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            Background = Art.Hex("#2E3A2A"),
            BorderBrush = canPlay ? Art.Warm : Art.Edge,
            IsEnabled = canPlay,
            ToolTip = canPlay ? "Play this card (free)" : "Can't play this right now"
        };
        play.Click += (_, _) => Perform(() => g.Play(index));

        var discard = new Button
        {
            Style = (Style)FindResource("ActionButton"),
            Content = $"+{Game.DiscardRefund}◆",
            Width = 38,
            Padding = new Thickness(2, 3, 2, 3),
            FontSize = 11.5,
            FontWeight = FontWeights.Bold,
            Foreground = Art.Accent,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            IsEnabled = canDiscard,
            ToolTip = g.Time >= Game.MaxTime
                ? $"Discard for +{Game.DiscardRefund} Time (you're already at the {Game.MaxTime} Time limit)"
                : $"Discard this card for +{Game.DiscardRefund} Time"
        };
        discard.Click += (_, _) => Perform(() => g.Discard(index));

        var buttons = new DockPanel { Margin = new Thickness(5, 0, 5, 5), LastChildFill = true };
        DockPanel.SetDock(discard, Dock.Right);
        buttons.Children.Add(discard);
        buttons.Children.Add(play);

        var layout = new DockPanel();
        DockPanel.SetDock(buttons, Dock.Bottom);
        layout.Children.Add(buttons);
        layout.Children.Add(CardFace(card, null, canPlay));

        return new Border
        {
            Width = CardWidth,
            Height = CardHeight,
            Margin = new Thickness(0, 0, 6, 0),
            CornerRadius = new CornerRadius(6),
            Background = Art.Hex("#1E2E26"),
            BorderBrush = canPlay ? Art.Warm : Art.Edge,
            BorderThickness = new Thickness(1),
            Child = layout
        };
    }

    private void RenderCards()
    {
        var g = _game;
        RenderTableauSummary();

        HandTitle.Text = $"YOUR HAND ({g.Hand.Count}/{Game.HandLimit})";
        HandHint.Text = g.InPlanning ? "  bought cards wait here for next turn · discard for +1 Time" : "  play cards to act (free) · discard for +1 Time";
        HandPanel.Children.Clear();
        // Up to 4 cards sit side by side. Past that the hand fans out: each card overlaps the next, its left side
        // (with Play) still showing; hovering a card lifts it to the front so all of it, Discard included, is reachable.
        const double slot = CardWidth + 6;
        double step = g.Hand.Count <= 4 ? slot : 60;
        for (int i = 0; i < g.Hand.Count; i++)
        {
            var hc = HandCard(i);
            if (step < slot && i < g.Hand.Count - 1)
                hc.Margin = new Thickness(hc.Margin.Left, hc.Margin.Top, hc.Margin.Right - (slot - step), hc.Margin.Bottom);
            Panel.SetZIndex(hc, i);
            int z = i;
            hc.MouseEnter += (_, _) => Panel.SetZIndex(hc, 100);
            hc.MouseLeave += (_, _) => Panel.SetZIndex(hc, z);
            HandPanel.Children.Add(hc);
        }

        // The tableau summary is narrow; the hand gets the rest of the strip.
        double handNeed = g.Hand.Count == 0 ? 2 * slot : slot + (g.Hand.Count - 1) * step;
        MarketColumn.Width = new GridLength(230);
        HandColumn.Width = new GridLength(Math.Max(handNeed, 2 * slot), GridUnitType.Star);
        if (g.Hand.Count == 0)
            HandPanel.Children.Add(new TextBlock
            {
                Text = "Empty. Without cards you can't act: buy some in Planning (zero-cost basics are free).",
                Foreground = Art.Dim, Width = 220, TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center
            });

        RenderPlanning();
        RenderRollChoice();
    }

    // ------------------------------------------------------------------ the Action Tableau and Planning

    /// <summary>True when the player has tucked the Planning overlay away to look at the board.</summary>
    private bool _planningHidden;

    /// <summary>The narrow strip beside the hand: what's in the tableau, what's waiting to come back, and a way to open it.</summary>
    private void RenderTableauSummary()
    {
        var g = _game;
        MarketPanel.Children.Clear();
        MarketHint.Text = g.InPlanning ? "  buy now" : "  buy cards in Planning";
        var stack = new StackPanel { Width = 206 };
        int piles = g.Tableau.Distinct().Count();
        stack.Children.Add(new TextBlock
        {
            Text = $"{g.Tableau.Count} cards in {piles} piles.",
            Foreground = Art.TextColor, FontSize = 12, TextWrapping = TextWrapping.Wrap
        });
        var used = g.SpentThisTurn.GroupBy(c => c.Name).Select(x => x.Count() > 1 ? $"{x.Key} ×{x.Count()}" : x.Key).ToList();
        stack.Children.Add(new TextBlock
        {
            Text = used.Count == 0 ? "Nothing used yet this turn." : "Used this turn (back after Planning): " + string.Join(", ", used),
            Foreground = Art.Dim, FontSize = 11, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 3, 0, 6),
            MaxHeight = 64, TextTrimming = TextTrimming.CharacterEllipsis
        });
        var open = new Button
        {
            Style = (Style)FindResource("ActionButton"),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            FontWeight = FontWeights.Bold,
            Content = g.InPlanning ? "▴ Open the tableau to buy" : "▴ Look at the tableau",
            BorderBrush = g.InPlanning ? Art.Warm : Art.Edge,
            IsEnabled = !g.IsOver && !g.AwaitingStart && (!g.InPlanning || _planningHidden) && !_busy
        };
        open.Click += (_, _) => { _planningHidden = false; _peekTableau = !g.InPlanning; RenderPlanning(); };
        stack.Children.Add(open);
        MarketPanel.Children.Add(stack);
    }

    /// <summary>Looking at the tableau during the Action phase (read-only).</summary>
    private bool _peekTableau;

    /// <summary>The Planning overlay: every pile in the tableau, with cost, count left and Buy.</summary>
    private void RenderPlanning()
    {
        var g = _game;
        bool planning = g.InPlanning;
        if (!planning) _planningHidden = false;
        if (planning) _peekTableau = false;
        bool show = (planning && !_planningHidden || _peekTableau && g.InAction) && !MenuOpen && !BriefingOpen && !g.IsOver;
        if (!show)
        {
            if (!g.InAction) _peekTableau = false;
            PlanningOverlay.Visibility = Visibility.Collapsed;
            return;
        }
        PlanningOverlay.Visibility = Visibility.Visible;
        PlanningTitle.Text = planning ? "Planning: buy cards for next turn" : "The Action Tableau";
        PlanningSub.Text = planning
            ? $"You have {g.Time} Time  ·  hand {g.Hand.Count}/{Game.HandLimit}  ·  unspent Time is lost when your turn ends. "
              + "Cards used this turn are greyed out until it ends."
            : "You can buy these in Planning, after your actions. Cards you play now come back to the tableau when your turn ends.";
        PlanningHide.Content = planning ? "▾ See the board" : "▾ Close";
        PlanningEnd.Visibility = planning ? Visibility.Visible : Visibility.Collapsed;
        PlanningEnd.IsEnabled = g.CanEndTurn && !_busy;

        TableauPanel.Children.Clear();
        // One pile per card: the free basics first, then by cost.
        var kinds = ActionCards.Starters.Concat(ActionCards.All).ToList();
        foreach (var card in kinds.OrderBy(c => c.Cost).ThenBy(c => c.Name))
        {
            int left = g.TableauCount(card);
            int usedNow = g.SpentThisTurn.Count(c => ReferenceEquals(c, card));
            int held = g.Hand.Count(c => ReferenceEquals(c, card));
            string? reason = g.BuyBlockedReason(card);
            string footer = !planning ? (card.Cost == 0 ? "Free" : $"Costs {card.Cost} Time")
                : reason is null ? (card.Cost == 0 ? "Take (free)" : $"Buy: {card.Cost} Time")
                : char.ToUpperInvariant(reason[0]) + reason[1..];
            var c0 = card;
            var button = CardButton(card, footer, planning && reason is null, inHand: false, () => g.Buy(c0));
            button.Margin = new Thickness(0);
            if (left == 0) button.Opacity = 0.55;

            var pile = new StackPanel { Margin = new Thickness(4, 0, 4, 8) };
            // A little stack behind the card shows how many are left.
            var grid = new Grid { Width = CardWidth + 6, Height = CardHeight + 6 };
            for (int k = Math.Min(left, 3) - 1; k >= 1; k--)
                grid.Children.Add(new Border
                {
                    Width = CardWidth, Height = CardHeight, CornerRadius = new CornerRadius(6),
                    Background = Art.Hex("#10242A"), BorderBrush = Art.Edge, BorderThickness = new Thickness(1),
                    Margin = new Thickness(k * 3, k * 3, 0, 0), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top
                });
            button.HorizontalAlignment = HorizontalAlignment.Left;
            button.VerticalAlignment = VerticalAlignment.Top;
            grid.Children.Add(button);
            pile.Children.Add(grid);
            var count = new List<string> { left == 0 ? "none left" : $"{left} left" };
            if (usedNow > 0) count.Add($"{usedNow} used");
            if (held > 0) count.Add($"{held} in hand");
            pile.Children.Add(new TextBlock
            {
                Text = string.Join(" · ", count), FontSize = 11, HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = left == 0 ? Art.Dim : Art.Accent
            });
            TableauPanel.Children.Add(pile);
        }
    }

    private void PlanningHide_Click(object sender, RoutedEventArgs e)
    {
        if (_game.InPlanning) _planningHidden = true;
        _peekTableau = false;
        RenderPlanning();
        RenderCards();
    }

    // ------------------------------------------------------------------ partial dice: trade cards in

    private readonly HashSet<int> _convertPicks = new();
    private readonly HashSet<int> _convertDice = new();

    /// <summary>
    /// After a roll with partials, click the dice to convert (a 3 shows two cards, a 4 shows one), then pick exactly
    /// that many cards from your hand to discard. Nothing else can happen until you convert or keep the roll.
    /// </summary>
    private void RenderRollChoice()
    {
        var g = _game;
        if (g.Pending is not { } p || MenuOpen || _busy)
        {
            RollChoiceOverlay.Visibility = Visibility.Collapsed;
            if (g.Pending is null) { _convertPicks.Clear(); _convertDice.Clear(); }
            return;
        }
        RollChoiceOverlay.Visibility = Visibility.Visible;
        _convertPicks.RemoveWhere(i => i >= g.Hand.Count);
        _convertDice.RemoveWhere(i => i >= p.Roll.Values.Count || DiceRoll.CardsFor(p.Roll.Values[i]) == 0);

        int need = _convertDice.Sum(i => DiceRoll.CardsFor(p.Roll.Values[i]));
        // If a choice of dice now costs fewer cards than are picked, drop the extra picks.
        while (_convertPicks.Count > need) _convertPicks.Remove(_convertPicks.Max());

        RollChoiceDice.Children.Clear();
        for (int i = 0; i < p.Roll.Values.Count; i++)
        {
            int index = i;
            int value = p.Roll.Values[i];
            int cost = DiceRoll.CardsFor(value);
            bool chosen = _convertDice.Contains(index);
            var die = Die(value, chosen ? DieLook.Converted : LookFor(value), 0, 52, chosen);
            if (cost > 0)
            {
                int othersCost = need - (chosen ? cost : 0);
                bool canAdd = chosen || (_convertDice.Count < p.MaxConversions && othersCost + cost <= g.Hand.Count);
                die.Cursor = canAdd ? System.Windows.Input.Cursors.Hand : System.Windows.Input.Cursors.No;
                die.ToolTip = chosen ? $"Chosen: costs {Plural(cost, "card", "cards")}. Click to un-choose."
                    : canAdd ? $"Click to turn this {value} into a success for {Plural(cost, "card", "cards")}."
                    : $"Needs {Plural(cost, "card", "cards")}; you can't afford it with the dice already chosen.";
                if (!canAdd) die.Opacity = 0.6;
                die.MouseLeftButtonUp += (_, _) =>
                {
                    if (!_convertDice.Remove(index) && canAdd) _convertDice.Add(index);
                    RenderRollChoice();
                };
            }
            var stack = new StackPanel { Margin = new Thickness(4, 0, 4, 0) };
            stack.Children.Add(die);
            stack.Children.Add(new TextBlock
            {
                Text = cost == 0 ? (DiceRoll.Face(value) == DieFace.Success ? "success" : "blank") : chosen ? "✓ chosen" : $"{Plural(cost, "card", "cards")}",
                FontSize = 11, HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = chosen ? Art.Warm : cost > 0 ? Art.Hex("#F0B0A8") : Art.Dim
            });
            RollChoiceDice.Children.Add(stack);
        }

        int after = p.Roll.Successes + _convertDice.Count;
        RollChoiceSub.Text = $"{p.Roll.Label}: {Plural(p.Roll.Stars, "success", "successes")} and {Plural(p.Roll.Partials, "partial", "partials")}. "
            + $"Click a die showing cards to choose it (up to {p.MaxConversions}): a 3 costs 2 cards, a 4 costs 1. Then pick that many cards to discard.";

        RollChoiceHand.Children.Clear();
        for (int i = 0; i < g.Hand.Count; i++)
        {
            int index = i;
            bool selected = _convertPicks.Contains(index);
            bool full = _convertPicks.Count >= need;
            RollChoiceHand.Children.Add(CardButton(g.Hand[i],
                selected ? "✓ Discard" : need == 0 ? "Choose a die first" : full ? "Enough picked" : "Pick to discard",
                enabled: selected || !full, inHand: true,
                onClick: () =>
                {
                    if (!_convertPicks.Remove(index) && _convertPicks.Count < need) _convertPicks.Add(index);
                    RenderRollChoice();
                },
                raw: true, selected: selected));
        }

        string result = p.Purpose == "fight" ? Plural(after, "wound", "wounds") : $"{after} success{(after == 1 ? "" : "es")}";
        RollChoiceConvert.IsEnabled = need > 0 && _convertPicks.Count == need;
        RollChoiceConvert.Content = need == 0 ? "Choose a die to convert"
            : _convertPicks.Count < need ? $"Pick {need - _convertPicks.Count} more card{(need - _convertPicks.Count == 1 ? "" : "s")}"
            : $"Discard {need}: {result}";
        RollChoiceKeep.Content = p.Purpose == "fight"
            ? $"Keep the roll ({Plural(p.Roll.Successes, "wound", "wounds")})"
            : $"Keep the roll ({p.Roll.Successes} success{(p.Roll.Successes == 1 ? "" : "es")})";
    }

    private void RollChoiceConvert_Click(object sender, RoutedEventArgs e)
    {
        var dice = _convertDice.ToList();
        var picks = _convertPicks.ToList();
        _convertDice.Clear();
        _convertPicks.Clear();
        Perform(() => _game.ConvertPartials(dice, picks));
    }

    private void RollChoiceKeep_Click(object sender, RoutedEventArgs e)
    {
        _convertDice.Clear();
        _convertPicks.Clear();
        Perform(() => _game.ConvertPartials(Array.Empty<int>(), Array.Empty<int>()));
    }

    /// <summary>After the briefing: reveal the opening event and start round 1's Action phase.</summary>
    private async void BeginNight()
    {
        if (!_game.Begin()) return;
        Render();
        // The opening event card, revealed before the night begins.
        if (_game.LastEvent is { } opening)
        {
            SetBusy(true);
            try { await RevealEventCardAsync(opening.Card, opening); }
            finally { SetBusy(false); }
            Render();
        }
        Sound.Play("round", 0.7);
        await ShowBannerAsync("ROUND 1", $"Action phase: play cards to act. Lead the {_game.Scenario.Victims} to the {_game.Scenario.SanctuaryName}.", Art.Warm);
    }

    // ------------------------------------------------------------------ items

    private static string ItemGlyph(ItemKind k) => k switch
    {
        ItemKind.BoatHook => "⚓",
        ItemKind.GuttingKnife => "†",
        ItemKind.Lantern => "✹",
        ItemKind.Rope => "∞",
        ItemKind.FlareGun => "✷",
        ItemKind.Bandages => "✚",
        ItemKind.BlessedSalt => "❄",
        ItemKind.IronNails => "▦",
        ItemKind.Key => "⚿",
        ItemKind.Vehicle => "»",
        _ => "?"
    };

    /// <summary>A few words for an item tile; the full rules text is in its tooltip.</summary>
    private string ItemShort(ItemDef def) => def.Kind switch
    {
        ItemKind.BoatHook or ItemKind.GuttingKnife => "+1 fight die",
        ItemKind.Lantern => "+1 search die",
        ItemKind.Rope => "+1 escort",
        ItemKind.FlareGun => "2 wounds, near",
        ItemKind.Bandages => "heal 2",
        ItemKind.BlessedSalt => "1 wound + ward",
        ItemKind.IronNails => "bar the door",
        ItemKind.Key => "used at a locked door",
        ItemKind.Vehicle => $"walk up to {_game.Scenario.VehicleRange} spaces",
        _ => ""
    };

    /// <summary>
    /// Your items as tiles, two to a row: icon, name and what it does at a glance. One-use items are buttons:
    /// click to use. Full text in the tooltip. Stays readable however many you carry (shrinks only past eight).
    /// </summary>
    private void RenderItems()
    {
        var g = _game;
        ItemsPanel.Children.Clear();
        if (g.Inventory.Count == 0)
        {
            ItemsPanel.Columns = 1;
            ItemsPanel.Children.Add(new TextBlock { Text = "Nothing yet. Search places marked ✦ to find gear.", Foreground = Art.Dim, FontSize = 13, TextWrapping = TextWrapping.Wrap });
            return;
        }
        ItemsPanel.Columns = 2;
        for (int i = 0; i < g.Inventory.Count; i++)
        {
            int index = i;
            var def = Items.All[g.Inventory[i]];
            bool usable = def.Consumable && g.CanUse(index) && !_busy;
            string? blocked = def.Consumable && !usable ? g.UseBlockedReason(index) ?? "not right now" : null;
            Brush tint = def.Consumable ? Art.Warm : Art.Accent;

            var icon = new Border
            {
                Width = 32, Height = 32,
                CornerRadius = new CornerRadius(8),
                Background = def.Consumable ? Art.Hex("#3A2E14") : Art.Hex("#14302B"),
                BorderBrush = tint,
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Child = new TextBlock { Text = ItemGlyph(def.Kind), FontSize = 17, Foreground = tint, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center }
            };
            DockPanel.SetDock(icon, Dock.Left);
            var words = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            words.Children.Add(new TextBlock { Text = def.Name, FontWeight = FontWeights.Bold, FontSize = 13, Foreground = tint, TextTrimming = TextTrimming.CharacterEllipsis });
            words.Children.Add(new TextBlock
            {
                // Kept items work by themselves; one-use items say "click to use", or why not yet.
                Text = !def.Consumable ? $"always on: {ItemShort(def)}" : usable ? $"▶ Click: {ItemShort(def)}" : blocked!,
                FontSize = 11.5, Foreground = usable ? Art.Warm : blocked is not null ? Art.Hex("#C9897E") : Art.Accent,
                TextTrimming = TextTrimming.CharacterEllipsis
            });
            var face = new DockPanel();
            face.Children.Add(icon);
            face.Children.Add(words);

            string tip = $"{def.Name} ({(def.Consumable ? "one use" : "kept: works by itself, nothing to click")})\n{def.Text}"
                + (def.Consumable ? (usable ? "\n\nClick to use it." : $"\n\nNot yet: {blocked}.") : "");
            FrameworkElement tile;
            if (usable)
            {
                var button = new Button
                {
                    Style = (Style)FindResource("ActionButton"),
                    Padding = new Thickness(6, 4, 6, 4),
                    HorizontalContentAlignment = HorizontalAlignment.Stretch,
                    BorderBrush = Art.Warm,
                    Content = face
                };
                button.Click += (_, _) => Perform(() => g.Use(index));
                tile = button;
            }
            else
            {
                // Not a button, so it doesn't fade out: kept items, and one-use items waiting for their moment.
                tile = new Border
                {
                    CornerRadius = new CornerRadius(6),
                    Background = def.Consumable ? Art.Hex("#1E1E1C") : Art.Hex("#14262B"),
                    BorderBrush = def.Consumable ? Art.Hex("#4A3E2A") : Art.Edge,
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(6, 4, 6, 4),
                    Child = face
                };
            }
            tile.Height = 42;
            tile.Margin = new Thickness(index % 2 == 0 ? 0 : 3, 0, index % 2 == 0 ? 3 : 0, 5);
            tile.ToolTip = tip;
            ItemsPanel.Children.Add(tile);
        }
    }

    // ------------------------------------------------------------------ tide report + journal

    private void RenderTideReport()
    {
        TideLines.Children.Clear();
        var tide = _game.LastTide;
        if (tide is null)
        {
            TideTitle.Text = "Quiet… for now";
            TideFlavour.Text = $"Do what you can, then end your turn. {_game.Scenario.Killer.Name} moves when you do.";
            return;
        }
        TideTitle.Text = $"Last {_game.Scenario.DeckName.ToLowerInvariant()} (round {tide.Round}): {tide.CardName}";
        TideFlavour.Text = tide.Flavour;
        foreach (var line in tide.Lines)
            TideLines.Children.Add(new TextBlock { Text = "• " + line, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 1, 0, 1), FontSize = 12 });
    }

    private void RenderLog()
    {
        var log = _game.Log;
        for (; _logShown < log.Count; _logShown++)
        {
            var entry = log[_logShown];
            LogPanel.Children.Add(new TextBlock
            {
                Text = entry.Kind == LogKind.System ? entry.Text : $"R{entry.Round}  {entry.Text}",
                TextWrapping = TextWrapping.Wrap,
                FontSize = 12,
                Margin = new Thickness(0, entry.Kind == LogKind.System ? 6 : 1, 0, 1),
                FontWeight = entry.Kind == LogKind.System ? FontWeights.Bold : FontWeights.Normal,
                Foreground = entry.Kind switch
                {
                    LogKind.Good => Art.Accent,
                    LogKind.Bad => Art.Dim,
                    LogKind.Tide => Art.Danger,
                    LogKind.System => Art.Warm,
                    _ => Art.TextColor
                }
            });
        }
        LogScroll.ScrollToEnd();
    }

    // ------------------------------------------------------------------ game over

    private void RenderGameOver()
    {
        var g = _game;
        if (!g.IsOver)
        {
            GameOverOverlay.Visibility = Visibility.Collapsed;
            return;
        }
        // Only pop the overlay once per game; "Look at the board" can hide it.
        if (ReferenceEquals(GameOverOverlay.Tag, g)) return;
        GameOverOverlay.Tag = g;
        GameOverOverlay.Visibility = Visibility.Visible;
        GameOverOverlay.Opacity = 0;
        _ = Anim.To(GameOverOverlay, OpacityProperty, 1, 500);

        bool won = g.Phase == Phase.Won;
        Sound.Play(won ? "win" : "lose");
        GameOverPortrait.Child = won
            ? Art.Portrait(Art.Survivor(g.Scenario.Id), Art.Accent, Art.Hex("#1F3A40"))
            : Art.Portrait(Art.Killer(g.Scenario.Id), Art.Danger, Art.Hex("#10262A"));
        GameOverTitle.Text = won ? g.Scenario.WinText : g.Scenario.LoseText;
        GameOverTitle.Foreground = won ? Art.Accent : Art.Danger;
        var sc = g.Scenario;
        string saved = g.VillagersSaved == 1 ? $"1 {sc.Victim}" : $"{g.VillagersSaved} {sc.Victims}";
        GameOverText.Text = won
            ? $"{sc.Survivor.ShortName} defeated {sc.Killer.NameLower} in round {g.Round}. {saved} reached the {sc.SanctuaryName}; {g.VillagersLost} did not."
            : $"{sc.Survivor.ShortName} fell in round {g.Round}. {saved} reached the {sc.SanctuaryName} first.";
        GameOverRating.Text = (won, g.VillagersSaved) switch
        {
            (true, >= 7) => $"Legend of {g.Scenario.Place} ({g.Difficulty})",
            (true, >= 4) => $"Keeper of the Light ({g.Difficulty})",
            (true, _) => $"Survivor ({g.Difficulty})",
            (false, >= 4) => $"Remembered Kindly ({g.Difficulty})",
            _ => $"Lost at Sea ({g.Difficulty})"
        };
    }
}
