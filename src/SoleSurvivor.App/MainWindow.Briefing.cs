using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using SoleSurvivor.Core;
using Ellipse = System.Windows.Shapes.Ellipse;
using Line = System.Windows.Shapes.Line;
using Polyline = System.Windows.Shapes.Polyline;

namespace SoleSurvivor.App;

/// <summary>
/// The objective briefing: what tonight is about, how you win, and a map that makes the safe place unmistakable,
/// with every group of people to rescue and the route home from each. Shown when a night begins and from the header.
/// </summary>
public partial class MainWindow
{
    private bool BriefingOpen => BriefingOverlay.Visibility == Visibility.Visible;

    private void Objective_Click(object sender, RoutedEventArgs e)
    {
        if (!_busy && !MenuOpen) ShowBriefing();
    }

    private void BriefContinue_Click(object sender, RoutedEventArgs e) => HideBriefing();

    private void HideBriefing()
    {
        BriefingOverlay.Visibility = Visibility.Collapsed;
        Render();
        if (_game.AwaitingStart) BeginNight();
    }

    private void ShowBriefing()
    {
        var g = _game;
        var sc = g.Scenario;
        string victims = sc.Victims, Victims = char.ToUpperInvariant(victims[0]) + victims[1..];
        string safe = sc.SanctuaryName;

        BriefTitle.Text = sc.Title;
        BriefTagline.Text = sc.Tagline;
        BriefIntro.Text = sc.Blurb;

        BriefGoals.Children.Clear();
        int out_ = g.VillagersRemaining;
        AddGoal("★", Art.Accent, $"Rescue the {victims}",
            $"Lead them to the {safe}, where they are safe. {(g.Round == 1 && g.AwaitingStart ? $"There are {out_} out there tonight" : $"{out_} still out there, {g.VillagersSaved} saved so far")}. "
            + $"Play Walk to reach them, then Lead (or Shepherd) to escort up to {g.EscortCapacity} at a time. Each one saved gives you 1 Hope.");
        AddGoal("⚔", Art.Danger, $"Defeat {sc.Killer.NameLower}",
            $"This is how you win: wound {sc.Killer.Her} until {sc.Killer.Her} health reaches 0 ({g.TidewifeHealth} now). "
            + $"Find weapons first. Every {sc.Victim} {sc.Killer.SheLower} kills makes {sc.Killer.Her} stronger, so rescues matter.");
        AddGoal("♥", Art.Warm, "Stay alive",
            $"If your health reaches 0, the night is lost. When the {sc.DeckName.ToLowerInvariant()}s run out or Dread reaches {Game.MaxDread}, "
            + $"the {sc.FinaleName} begins: anyone not yet at the {safe} is lost, and {sc.Killer.SheLower} hunts only you.");

        var notes = new List<string>();
        if (sc.LockedEdges.Length > 0)
            notes.Add($"Padlocks mark locked doors. Nobody gets through, {sc.Killer.NameLower} included, until you walk through carrying a {sc.KeyName} (or the {sc.FinaleName} begins).");
        if (sc.HasSwarm)
            notes.Add($"{sc.Killer.Name} isn't alone. On each {sc.RoundWord} where {sc.Killer.SheLower} attacks, a {sc.SwarmName} crawls out where {sc.Killer.SheLower} stands (at most {sc.SwarmMax}). "
                + $"Every {sc.RoundWord} they all creep one step towards you. One on its own only hisses; two or three nip you for 1, four or more for 2. "
                + $"Attack cards kill one per success, and they shield {sc.Killer.Her}: in {sc.Killer.Her} space, the {sc.SwarmNames} die first.");
        if (sc.VehicleName is not null)
            notes.Add($"Find the {sc.VehicleName} to move up to {sc.VehicleRange} spaces at a time.");
        BriefNote.Text = string.Join(" ", notes);
        BriefNote.Visibility = notes.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

        BriefContinue.Content = g.AwaitingStart ? "Got it: begin the night" : "Back to the night";
        BriefMapTitle.Text = $"Bring the {victims} to the {safe}";
        DrawBriefingMap();

        BriefLegend.Children.Clear();
        AddLegend(Art.Accent, "★", $"The {safe}: safe");
        AddLegend(Art.Hex("#7FD8C4"), "♟", $"{Victims} to rescue");
        AddLegend(Art.Hex("#7FD8C4"), "┅", "Route to safety");
        AddLegend(Art.Warm, "●", "You");
        AddLegend(Art.Danger, "☠", sc.Killer.Name);
        if (sc.HasSwarm) AddLegend(Art.Hex("#BFD8FF"), "●●", char.ToUpperInvariant(sc.SwarmNames[0]) + sc.SwarmNames[1..]);
        if (g.LockedDoors.Any()) AddLegend(Art.Warm, "⚿", "Locked door");

        BriefingOverlay.Visibility = Visibility.Visible;
        PlanningOverlay.Visibility = Visibility.Collapsed;
        BriefingOverlay.Opacity = 0;
        _ = Anim.To(BriefingOverlay, OpacityProperty, 1, 260);
    }

    private void AddGoal(string glyph, Brush colour, string title, string text)
    {
        var row = new DockPanel { Margin = new Thickness(0, 0, 0, 10) };
        var icon = new Border
        {
            Width = 34, Height = 34, CornerRadius = new CornerRadius(17), Margin = new Thickness(0, 0, 12, 0),
            BorderBrush = colour, BorderThickness = new Thickness(2), Background = Art.Hex("#0E1C21"),
            VerticalAlignment = VerticalAlignment.Top,
            Child = new TextBlock { Text = glyph, FontSize = 16, Foreground = colour, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center }
        };
        DockPanel.SetDock(icon, Dock.Left);
        row.Children.Add(icon);
        var body = new StackPanel();
        body.Children.Add(new TextBlock { Text = title, FontWeight = FontWeights.Bold, FontSize = 15, Foreground = colour });
        body.Children.Add(new TextBlock { Text = text, FontSize = 12.5, TextWrapping = TextWrapping.Wrap, Foreground = Art.TextColor });
        row.Children.Add(body);
        BriefGoals.Children.Add(row);
    }

    private void AddLegend(Brush colour, string glyph, string text)
    {
        var p = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 18, 0) };
        p.Children.Add(new TextBlock { Text = glyph, Foreground = colour, FontWeight = FontWeights.Bold, FontSize = 14, Margin = new Thickness(0, 0, 6, 0) });
        p.Children.Add(new TextBlock { Text = text, Foreground = Art.Dim, FontSize = 12, VerticalAlignment = VerticalAlignment.Center });
        BriefLegend.Children.Add(p);
    }

    /// <summary>A clean map of tonight's board: roads, places, the safe place ringed in green, people to rescue, and the way home.</summary>
    private void DrawBriefingMap()
    {
        var g = _game;
        var sc = g.Scenario;
        var theme = Art.ThemeFor(sc.Id);
        var map = BriefMap;
        map.Children.Clear();

        map.Children.Add(new System.Windows.Shapes.Rectangle
        {
            Width = Board.Width, Height = Board.Height,
            Fill = new LinearGradientBrush(Art.C(theme.LandTop), Art.C(theme.LandBottom), 90)
        });
        map.Children.Add(new System.Windows.Shapes.Rectangle { Width = Board.Width, Height = Board.Height, Fill = Art.Hex("#000000"), Opacity = 0.35 });

        foreach (var (a, b) in Board.Edges)
            foreach (var part in Art.Road(Board.Locations[a].X, Board.Locations[a].Y, Board.Locations[b].X, Board.Locations[b].Y, theme))
                map.Children.Add(part);

        // The way home from every group of people, along roads anyone can use right now.
        var safe = Board.Locations[Board.Sanctuary];
        for (int i = 0; i < Board.Count; i++)
        {
            if (g.Villagers[i] == 0) continue;
            var path = Board.Path(i, Board.Sanctuary, null, (x, y) => !g.IsLocked(x, y));
            if (path is null) continue;
            var pts = new PointCollection { new(Board.Locations[i].X, Board.Locations[i].Y) };
            foreach (int stop in path) pts.Add(new Point(Board.Locations[stop].X, Board.Locations[stop].Y));
            var route = new Polyline
            {
                Points = pts, Stroke = Art.Hex("#7FD8C4"), StrokeThickness = 5, Opacity = 0.85,
                StrokeDashArray = new DoubleCollection { 1.2, 1.6 }, StrokeDashCap = PenLineCap.Round, StrokeLineJoin = PenLineJoin.Round
            };
            route.BeginAnimation(System.Windows.Shapes.Shape.StrokeDashOffsetProperty,
                new DoubleAnimation(0, -5.6, new Duration(TimeSpan.FromMilliseconds(600))) { RepeatBehavior = RepeatBehavior.Forever });
            map.Children.Add(route);
        }

        foreach (var (a, b) in g.LockedDoors)
        {
            var mid = new Point((Board.Locations[a].X + Board.Locations[b].X) / 2, (Board.Locations[a].Y + Board.Locations[b].Y) / 2);
            var padlock = Art.Padlock(true);
            Canvas.SetLeft(padlock, mid.X - 12);
            Canvas.SetTop(padlock, mid.Y - 12);
            map.Children.Add(padlock);
        }

        // The safe place: a big pulsing green ring and a banner you can't miss.
        var glow = Art.Glow(safe.X, safe.Y - 10, 130, Art.C("#7FD8C4"), 0.35);
        map.Children.Add(glow);
        var ring = new Ellipse { Width = 210, Height = 150, Stroke = Art.Accent, StrokeThickness = 4, Fill = new SolidColorBrush(Color.FromArgb(30, 127, 216, 196)) };
        Canvas.SetLeft(ring, safe.X - 105);
        Canvas.SetTop(ring, safe.Y - 90);
        Anim.Loop(ring, OpacityProperty, 0.45, 1, 800);
        map.Children.Add(ring);

        foreach (var loc in Board.Locations)
        {
            var icon = Art.LocationIcon(sc.Id, loc.Id);
            if (!loc.IsSanctuary) icon.Opacity = 0.85;
            Canvas.SetLeft(icon, loc.X - Art.IconWidth / 2);
            Canvas.SetTop(icon, loc.Y - 46);
            map.Children.Add(icon);

            var plaque = new Border
            {
                Width = 124, CornerRadius = new CornerRadius(5),
                Background = new SolidColorBrush(Color.FromArgb(235, 14, 28, 33)),
                BorderBrush = loc.IsSanctuary ? Art.Accent : Art.Edge, BorderThickness = new Thickness(loc.IsSanctuary ? 2 : 1),
                Child = new TextBlock
                {
                    Text = loc.Name, FontFamily = new FontFamily("Georgia"), FontWeight = FontWeights.Bold, FontSize = 12.5,
                    Foreground = loc.IsSanctuary ? Art.Accent : Art.TextColor, HorizontalAlignment = HorizontalAlignment.Center
                }
            };
            Canvas.SetLeft(plaque, loc.X - 62);
            Canvas.SetTop(plaque, loc.Y + 18);
            map.Children.Add(plaque);

            // People waiting here.
            int count = g.Villagers[loc.Id];
            if (count > 0)
            {
                var badge = Badge($"♟ {count}", Art.Hex("#7FD8C4"), Art.Hex("#0E2A26"));
                badge.ToolTip = $"{count} {(count == 1 ? sc.Victim : sc.Victims)} waiting at the {loc.Name}";
                Canvas.SetLeft(badge, loc.X + 26);
                Canvas.SetTop(badge, loc.Y - 58);
                map.Children.Add(badge);
            }
        }

        var safeBanner = Badge($"★  SAFE: bring the {sc.Victims} here", Art.Hex("#0B1418"), Art.Accent, 15);
        safeBanner.Effect = new DropShadowEffect { Color = Art.C("#7FD8C4"), BlurRadius = 14, ShadowDepth = 0, Opacity = 0.9 };
        safeBanner.Loaded += (_, _) =>
        {
            Canvas.SetLeft(safeBanner, Math.Clamp(safe.X - safeBanner.ActualWidth / 2, 6, Board.Width - safeBanner.ActualWidth - 6));
        };
        Canvas.SetLeft(safeBanner, safe.X - 140);
        Canvas.SetTop(safeBanner, Math.Min(safe.Y + 44, Board.Height - 34));
        map.Children.Add(safeBanner);

        // Where you are and where the killer is.
        var you = Badge("● You", Art.Hex("#0B1418"), Art.Warm);
        Canvas.SetLeft(you, Board.Locations[g.SurvivorLocation].X - 100);
        Canvas.SetTop(you, Board.Locations[g.SurvivorLocation].Y - 64);
        map.Children.Add(you);
        var killer = Badge($"☠ {sc.Killer.ShortName}", Art.Hex("#FFFFFF"), Art.Hex("#7A1E18"));
        Canvas.SetLeft(killer, Board.Locations[g.TidewifeLocation].X - 104);
        Canvas.SetTop(killer, Board.Locations[g.TidewifeLocation].Y - 64 + (g.TidewifeLocation == g.SurvivorLocation ? -26 : 0));
        map.Children.Add(killer);
    }

    private static Border Badge(string text, Brush foreground, Brush background, double size = 13) => new()
    {
        CornerRadius = new CornerRadius(8),
        Background = background,
        BorderBrush = foreground,
        BorderThickness = new Thickness(1.2),
        Padding = new Thickness(7, 2, 7, 3),
        Child = new TextBlock { Text = text, Foreground = foreground, FontWeight = FontWeights.Bold, FontSize = size }
    };
}
