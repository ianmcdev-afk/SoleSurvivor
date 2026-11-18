using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using SoleSurvivor.Core;
using Ellipse = System.Windows.Shapes.Ellipse;
using Line = System.Windows.Shapes.Line;

namespace SoleSurvivor.App;

/// <summary>The board: static scenery, per-location info, character tokens and their animations.</summary>
public partial class MainWindow
{
    private Art.Figure _wren = null!;
    private Art.Figure _tidewife = null!;
    private readonly Dictionary<int, List<FrameworkElement>> _villagerFigures = new();
    private Canvas? _stoneGlow;
    private Canvas? _weatherLayer;
    private string? _sceneId;
    private Theme _theme = Art.ThemeFor("gallowmouth");

    /// <summary>Colour of the killer's ripples and footprints in this scenario.</summary>
    private Color TrailColor => _theme.Trail;

    // ------------------------------------------------------------------ geometry

    private static Point Spot(int id) => new(Board.Locations[id].X, Board.Locations[id].Y);

    /// <summary>Where Wren's feet go at a location (left of the building).</summary>
    private static Point WrenFeet(int id) => id == Board.Sanctuary && Scenarios.Current.Id == "gallowmouth"
        ? new(Board.Locations[id].X - 60, Board.Locations[id].Y + 14)   // on the rock beside the lighthouse
        : new(Board.Locations[id].X - 78, Board.Locations[id].Y + 40);

    /// <summary>Where the Tidewife's feet go at a location (right of the building).</summary>
    private static Point HerFeet(int id) => new(Board.Locations[id].X + 80, Board.Locations[id].Y + 40);

    /// <summary>Feet position of the n-th villager figure at a location.</summary>
    private static Point VillagerFeet(int id, int index, int count)
    {
        int shown = Math.Min(count, 10);
        double start = Board.Locations[id].X - (shown - 1) * 7;
        return new Point(start + index * 14, Board.Locations[id].Y + 66);
    }

    private static void Place(Art.Figure f, Point feet)
    {
        Anim.Set(f.Position, TranslateTransform.XProperty, feet.X - f.FootX);
        Anim.Set(f.Position, TranslateTransform.YProperty, feet.Y - f.FootY);
    }

    private static Point FeetOf(Art.Figure f) => new(f.Position.X + f.FootX, f.Position.Y + f.FootY);

    // ------------------------------------------------------------------ one-off scene

    /// <summary>Draw the current scenario's board. Cheap to skip when the scenario hasn't changed.</summary>
    private void BuildScene()
    {
        string id = Scenarios.Current.Id;
        if (_sceneId == id) return;
        _sceneId = id;
        _theme = Art.ThemeFor(id);
        Sound.SetAmbient(id);
        const double w = Board.Width, h = Board.Height;

        foreach (var layer in new[] { SceneLayer, AmbientLayer, RouteLayer, IconLayer, InfoLayer, TokenLayer, FogLayer, FxLayer })
            layer.Children.Clear();
        BoardFrame.Background = Art.Hex(_theme.Board);

        Art.DrawScenery(SceneLayer, w, h, id);

        // Roads under everything else.
        foreach (var (a, b) in Board.Edges)
            foreach (var part in Art.Road(Board.Locations[a].X, Board.Locations[a].Y, Board.Locations[b].X, Board.Locations[b].Y, _theme))
                SceneLayer.Children.Add(part);

        Art.AddLightPools(AmbientLayer);
        _stoneGlow = null;
        if (id == "gallowmouth")
        {
            _stoneGlow = Art.AddStoneGlow(AmbientLayer);
            var lamp = Board.Locations[Board.Sanctuary];
            Art.AddBeam(AmbientLayer, lamp.X, lamp.Y - 47);
        }
        else if (id == "whitepine")
        {
            Art.AddAurora(AmbientLayer, w);
        }

        foreach (var loc in Board.Locations)
        {
            var icon = Art.LocationIcon(id, loc.Id);
            Canvas.SetLeft(icon, loc.X - Art.IconWidth / 2);
            Canvas.SetTop(icon, loc.Y - 46);
            IconLayer.Children.Add(icon);
        }
        // Chimney smoke: the cannery, the ranger station's stove, the boiler house.
        (int loc, double dx, double dy)? chimney = id switch
        {
            "gallowmouth" => (7, 72, 2),
            "whitepine" => (0, 57, 10),
            "stagnes" => (6, 67, -2),
            _ => null
        };
        if (chimney is { } c)
        {
            var at = Board.Locations[c.loc];
            Art.AddChimneySmoke(IconLayer, at.X - 42 + c.dx, at.Y - 46 + c.dy);
        }

        Art.AddFog(FogLayer, w, h);
        _weatherLayer = new Canvas { IsHitTestVisible = false };
        if (_theme.Weather == Weather.Snow) Art.AddSnow(_weatherLayer, w, h);
        if (_theme.Weather == Weather.Rain) Art.AddRain(_weatherLayer, w, h);
        FogLayer.Children.Add(_weatherLayer);
        Art.AddVignette(FogLayer, w, h);
        ApplyAmbientSetting();

        _wren = Art.Survivor(id);
        _tidewife = Art.Killer(id);
        TokenLayer.Children.Add(_tidewife.Root);
        TokenLayer.Children.Add(_wren.Root);
        ResetFigures();
    }

    /// <summary>Weather, fog and ambient motion follow the Options setting.</summary>
    private void ApplyAmbientSetting()
    {
        var v = _settings.AmbientEffects ? Visibility.Visible : Visibility.Collapsed;
        AmbientLayer.Visibility = v;
        FogLayer.Visibility = v;
    }

    /// <summary>Put both figures back to their idle state (after a death or banishing).</summary>
    private void ResetFigures()
    {
        foreach (var f in new[] { _wren, _tidewife })
        {
            Anim.Set(f.Body, OpacityProperty, 1);
            Anim.Set(f.Root, OpacityProperty, 1);
            Anim.Set(f.Tilt, RotateTransform.AngleProperty, 0);
            Anim.Set(f.Facing, ScaleTransform.ScaleYProperty, 1);
            Anim.Set(f.Bob, TranslateTransform.YProperty, 0);
        }
        StartIdle(_wren);
        StartIdle(_tidewife);
    }

    private void StartIdle(Art.Figure f)
    {
        if (f == _tidewife)
        {
            Anim.Loop(f.Bob, TranslateTransform.YProperty, 0, -3, 1700);
            Anim.Loop(f.Tilt, RotateTransform.AngleProperty, -2.5, 2.5, 2300);
            Anim.Loop(f.Body, OpacityProperty, 0.78, 1, 900);
        }
        else
        {
            Anim.Loop(f.Bob, TranslateTransform.YProperty, 0, -1.2, 1300);
            Anim.Set(f.Tilt, RotateTransform.AngleProperty, 0);
        }
        if (f.LegLeft is not null) Anim.Set(f.LegLeft, RotateTransform.AngleProperty, 0);
        if (f.LegRight is not null) Anim.Set(f.LegRight, RotateTransform.AngleProperty, 0);
    }

    private static void Face(Art.Figure f, double fromX, double toX)
    {
        if (Math.Abs(toX - fromX) > 1) f.Facing.ScaleX = toX < fromX ? -1 : 1;
    }

    // ------------------------------------------------------------------ per-render board state

    private void RenderBoard()
    {
        RouteLayer.Children.Clear();
        InfoLayer.Children.Clear();
        _villagerFigures.Clear();

        // Animated "marching" route to every place you can go right now.
        foreach (int n in Board.Neighbours(_game.SurvivorLocation))
        {
            if (!IsReachable(n)) continue;
            var a = Spot(_game.SurvivorLocation);
            var b = Spot(n);
            var line = new Line
            {
                X1 = a.X, Y1 = a.Y, X2 = b.X, Y2 = b.Y,
                Stroke = Art.Warm,
                StrokeThickness = 3,
                StrokeDashArray = new DoubleCollection { 1.5, 2 },
                StrokeDashCap = PenLineCap.Round,
                Opacity = 0.9
            };
            line.BeginAnimation(System.Windows.Shapes.Shape.StrokeDashOffsetProperty,
                new DoubleAnimation(0, -7, new Duration(TimeSpan.FromMilliseconds(700))) { RepeatBehavior = RepeatBehavior.Forever });
            RouteLayer.Children.Add(line);
        }

        foreach (var loc in Board.Locations)
            BuildLocationInfo(loc);

        Anim.Set(FinaleTint, OpacityProperty, _game.InFinale ? 1 : 0);
        // The standing stones wake up as the Dread rises.
        if (_stoneGlow is not null)
            _stoneGlow.Opacity = _game.InFinale ? 1 : 0.15 + 0.85 * _game.Dread / Game.MaxDread;

        if (!_busy)
        {
            Place(_wren, WrenFeet(_game.SurvivorLocation));
            Place(_tidewife, HerFeet(_game.TidewifeLocation));
            _wren.Facing.ScaleX = 1;
            _tidewife.Facing.ScaleX = -1;
        }
    }

    private void BuildLocationInfo(LocationDef loc)
    {
        int id = loc.Id;
        bool reachable = IsReachable(id);
        bool wrenHere = _game.SurvivorLocation == id;
        bool herHere = _game.TidewifeLocation == id;

        if (reachable)
        {
            var ring = new Ellipse
            {
                Width = 140, Height = 50,
                Stroke = Art.Warm, StrokeThickness = 2.5,
                Fill = new SolidColorBrush(Color.FromArgb(28, 232, 194, 106)),
                IsHitTestVisible = false
            };
            Canvas.SetLeft(ring, loc.X - 70);
            Canvas.SetTop(ring, loc.Y - 4);
            Anim.Loop(ring, OpacityProperty, 0.35, 1, 650);
            InfoLayer.Children.Add(ring);
        }

        // Name plaque.
        string label = loc.IsSanctuary ? $"{loc.Name}   ★ {_game.VillagersSaved}" : loc.Name;
        var plaque = new Border
        {
            Width = 116,
            CornerRadius = new CornerRadius(5),
            Background = new SolidColorBrush(Color.FromArgb(232, 14, 28, 33)),
            BorderBrush = wrenHere ? Art.Accent : herHere ? Art.Danger : reachable ? Art.Warm : Art.Edge,
            BorderThickness = new Thickness(wrenHere || herHere || reachable ? 1.6 : 1),
            Padding = new Thickness(4, 1, 4, 2),
            IsHitTestVisible = false,
            Child = new TextBlock
            {
                Text = label,
                FontFamily = new FontFamily("Georgia"),
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                Foreground = loc.IsSanctuary ? Art.Warm : Art.TextColor,
                HorizontalAlignment = HorizontalAlignment.Center
            }
        };
        Canvas.SetLeft(plaque, loc.X - 58);
        Canvas.SetTop(plaque, loc.Y + 18);
        InfoLayer.Children.Add(plaque);

        // Villagers waiting here.
        int count = _game.Villagers[id];
        var figures = new List<FrameworkElement>();
        for (int i = 0; i < Math.Min(count, 10); i++)
        {
            // The special victim from an event is the first figure, in blue (Companion) or orange (Wounded).
            bool special = i == 0 && _game.SpecialVictimAt == id;
            string? coat = special ? (_game.SpecialVictimKind == EventKind.Wounded ? "#E08A2E" : "#3C6FD6") : null;
            var v = Art.Villager(id * 3 + i, coat);
            v.IsHitTestVisible = false;
            v.LayoutTransform = new ScaleTransform(1.4, 1.4);
            var feet = VillagerFeet(id, i, count);
            Canvas.SetLeft(v, feet.X - 7);
            Canvas.SetTop(v, feet.Y - 25);
            if (special)
                v.Effect = new System.Windows.Media.Effects.DropShadowEffect { Color = Art.C(coat!), BlurRadius = 12, ShadowDepth = 0, Opacity = 1 };
            InfoLayer.Children.Add(v);
            figures.Add(v);
        }
        if (count > 10)
        {
            var more = new TextBlock { Text = $"+{count - 10}", Foreground = Art.Warm, FontWeight = FontWeights.Bold, FontSize = 11, IsHitTestVisible = false };
            Canvas.SetLeft(more, VillagerFeet(id, 9, count).X + 10);
            Canvas.SetTop(more, loc.Y + 50);
            InfoLayer.Children.Add(more);
        }
        _villagerFigures[id] = figures;

        // A trap set by an event.
        if (_game.ActiveEvents.Any(e => e.Card.Kind == EventKind.Trap && e.Location == id))
        {
            var trap = new TextBlock
            {
                Text = "⊗", FontSize = 20, FontWeight = FontWeights.Bold, Foreground = Art.Accent, IsHitTestVisible = false,
                ToolTip = "Trap: 2 wounds to the killer when they end a move here"
            };
            trap.Effect = new System.Windows.Media.Effects.DropShadowEffect { Color = Colors.Black, BlurRadius = 6, ShadowDepth = 0, Opacity = 1 };
            Canvas.SetLeft(trap, loc.X - 60);
            Canvas.SetTop(trap, loc.Y - 12);
            InfoLayer.Children.Add(trap);
        }

        // Things left to find.
        int stash = _game.StashCount(id);
        if (stash > 0)
        {
            var badge = new Border
            {
                Width = 30, Height = 20,
                CornerRadius = new CornerRadius(10),
                Background = Art.Hex("#3A2E14"),
                BorderBrush = Art.Warm,
                BorderThickness = new Thickness(1),
                IsHitTestVisible = false,
                Child = new TextBlock
                {
                    Text = $"✦{stash}",
                    Foreground = Art.Warm,
                    FontSize = 11,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
            Canvas.SetLeft(badge, loc.X + 30);
            Canvas.SetTop(badge, loc.Y - 54);
            InfoLayer.Children.Add(badge);
        }

        // Click target covering the building, plaque and villagers.
        string action = !reachable ? ""
            : EscortMode.IsChecked == true && _game.CanEscort(id)
                ? $"\n\nClick: escort up to {_game.EscortCapacity} {_game.Scenario.Victims} here."
                : "\n\nClick: walk here.";
        var hit = new Border
        {
            Width = 130, Height = 124,
            Background = Brushes.Transparent,
            Cursor = reachable ? System.Windows.Input.Cursors.Hand : System.Windows.Input.Cursors.Arrow,
            ToolTip = $"{loc.Name}\n{loc.Flavour}\n\n{char.ToUpper(_game.Scenario.Victims[0]) + _game.Scenario.Victims[1..]}: {count}   Things to find: {stash}" + action
        };
        hit.MouseLeftButtonUp += (_, _) => OnLocationClicked(id);
        Canvas.SetLeft(hit, loc.X - 65);
        Canvas.SetTop(hit, loc.Y - 52);
        InfoLayer.Children.Add(hit);
    }

    private void HideVillagerFigures(int location, int count)
    {
        if (!_villagerFigures.TryGetValue(location, out var list)) return;
        for (int i = 0; i < count && list.Count > 0; i++)
        {
            var v = list[^1];
            list.RemoveAt(list.Count - 1);
            v.Visibility = Visibility.Hidden;
        }
    }

    private async Task FadeVillagerFiguresAsync(int location, int count)
    {
        if (!_villagerFigures.TryGetValue(location, out var list)) return;
        var tasks = new List<Task>();
        for (int i = 0; i < count && list.Count > 0; i++)
        {
            var v = list[^1];
            list.RemoveAt(list.Count - 1);
            var sink = new TranslateTransform();
            v.RenderTransform = sink;
            tasks.Add(Anim.To(v, OpacityProperty, 0, 700));
            tasks.Add(Anim.To(sink, TranslateTransform.YProperty, 10, 700, Anim.EaseOut));
        }
        await Task.WhenAll(tasks);
    }

    // ------------------------------------------------------------------ movement animations

    private void StartWalkCycle(Art.Figure f, double stepMs)
    {
        Anim.Loop(f.Bob, TranslateTransform.YProperty, 0, -3, stepMs / 2);
        Anim.Loop(f.Tilt, RotateTransform.AngleProperty, -3, 3, stepMs);
        if (f.LegLeft is not null) Anim.Loop(f.LegLeft, RotateTransform.AngleProperty, -24, 24, stepMs);
        if (f.LegRight is not null) Anim.Loop(f.LegRight, RotateTransform.AngleProperty, 24, -24, stepMs);
    }

    private async Task WalkWrenAsync(int from, int to, int followers)
    {
        var start = WrenFeet(from);
        var end = WrenFeet(to);
        Place(_wren, start);
        Face(_wren, start.X, end.X);

        double distance = (end - start).Length;
        double ms = Math.Clamp(distance * 3.4, 420, 1000);
        StartWalkCycle(_wren, 300);
        _ = Sound.StepsAsync(ms * Anim.Speed, followers > 0 ? 4 : 3);

        var tasks = new List<Task>
        {
            Anim.To(_wren.Position, TranslateTransform.XProperty, end.X - _wren.FootX, ms, Anim.EaseInOut),
            Anim.To(_wren.Position, TranslateTransform.YProperty, end.Y - _wren.FootY, ms, Anim.EaseInOut)
        };
        // Villagers trail behind her.
        var dir = end - start;
        dir.Normalize();
        for (int i = 0; i < followers; i++)
        {
            var back = dir * (22 + i * 13);
            tasks.Add(WalkVillagerAsync(start - back + new Vector(0, 2), end - back + new Vector(0, 2), ms, 60 + i * 70, id: i));
        }
        await Task.WhenAll(tasks);
        StartIdle(_wren);
    }

    private async Task WalkVillagerAsync(Point fromFeet, Point toFeet, double ms, double delayMs, int id)
    {
        var v = Art.Villager(id + 17);
        v.LayoutTransform = new ScaleTransform(1.4, 1.4);
        var move = new TranslateTransform(fromFeet.X - 7, fromFeet.Y - 25);
        var bob = new TranslateTransform();
        var group = new TransformGroup();
        group.Children.Add(bob);
        group.Children.Add(move);
        v.RenderTransform = group;
        FxLayer.Children.Add(v);
        Anim.Loop(bob, TranslateTransform.YProperty, 0, -2, 130);
        await Anim.Wait(delayMs);
        await Task.WhenAll(
            Anim.To(move, TranslateTransform.XProperty, toFeet.X - 7, ms, Anim.EaseInOut),
            Anim.To(move, TranslateTransform.YProperty, toFeet.Y - 25, ms, Anim.EaseInOut));
        FxLayer.Children.Remove(v);
    }

    /// <summary>Villagers walking from one location to another on their own (Lantern Signal).</summary>
    private Task VillagersWalkAsync(int from, int to, int count)
    {
        var tasks = new List<Task>();
        for (int i = 0; i < count; i++)
        {
            var a = VillagerFeet(from, i, count);
            var b = Board.Locations[to].IsSanctuary ? new Point(Board.Locations[to].X, Board.Locations[to].Y + 8) : VillagerFeet(to, i, count);
            tasks.Add(WalkVillagerAsync(a, b, 750, i * 80, from * 5 + i));
        }
        return Task.WhenAll(tasks);
    }

    private async Task GlideAsync(int from, int to)
    {
        var start = HerFeet(from);
        var end = HerFeet(to);
        Place(_tidewife, start);
        Face(_tidewife, start.X, end.X);
        Ripple(start, TrailColor);

        double distance = (end - start).Length;
        double ms = Math.Clamp(distance * 4.4, 650, 1250);
        Sound.Play("killer_move", 0.7);
        Anim.Set(_tidewife.Tilt, RotateTransform.AngleProperty, 0);
        _ = Anim.To(_tidewife.Tilt, RotateTransform.AngleProperty, _tidewife.Facing.ScaleX * 7, 250);

        // Wet footprints left behind.
        for (int i = 1; i <= 3; i++)
        {
            var p = start + (end - start) * (i / 4.0);
            _ = DelayedPuddleAsync(p, ms * i / 4.0);
        }

        await Task.WhenAll(
            Anim.To(_tidewife.Position, TranslateTransform.XProperty, end.X - _tidewife.FootX, ms, Anim.EaseInOut),
            Anim.To(_tidewife.Position, TranslateTransform.YProperty, end.Y - _tidewife.FootY, ms, Anim.EaseInOut));
        Ripple(end, TrailColor);
        StartIdle(_tidewife);
    }

    private async Task SinkRiseAsync(int from, int to)
    {
        var f = _tidewife;
        Place(f, HerFeet(from));
        Sound.Play("killer_move", 0.8);
        Ripple(HerFeet(from), TrailColor);
        f.Body.BeginAnimation(OpacityProperty, null);
        await Task.WhenAll(
            Anim.To(f.Facing, ScaleTransform.ScaleYProperty, 0.1, 450, Anim.EaseInOut),
            Anim.To(f.Body, OpacityProperty, 0, 450));
        Place(f, HerFeet(to));
        Ripple(HerFeet(to), TrailColor);
        await Anim.Wait(250);
        Ripple(HerFeet(to), TrailColor);
        await Task.WhenAll(
            Anim.To(f.Facing, ScaleTransform.ScaleYProperty, 1, 550, Anim.Back),
            Anim.To(f.Body, OpacityProperty, 1, 450));
        StartIdle(f);
    }

    private async Task LungeAsync(double fraction = 0.55)
    {
        var her = FeetOf(_tidewife);
        var wren = FeetOf(_wren);
        double baseX = _tidewife.Position.X;
        double dx = (wren.X - her.X) * fraction;
        Face(_tidewife, her.X, wren.X);
        await Anim.To(_tidewife.Position, TranslateTransform.XProperty, baseX + dx, 170, Anim.EaseOut);
        await Anim.To(_tidewife.Position, TranslateTransform.XProperty, baseX, 320, Anim.EaseInOut);
    }

    private static async Task ShakeAsync(Art.Figure f)
    {
        double x = f.Position.X;
        foreach (double d in new[] { 6.0, -6.0, 4.0, -3.0, 0.0 })
            await Anim.To(f.Position, TranslateTransform.XProperty, x + d, 45);
    }

    private async Task FlashAsync(Art.Figure f)
    {
        f.Body.BeginAnimation(OpacityProperty, null);
        for (int i = 0; i < 2; i++)
        {
            await Anim.To(f.Body, OpacityProperty, 0.15, 80);
            await Anim.To(f.Body, OpacityProperty, 1, 110);
        }
        if (f == _tidewife) StartIdle(f);
    }

    private async Task WrenFallsAsync()
    {
        _wren.Bob.BeginAnimation(TranslateTransform.YProperty, null);
        await Task.WhenAll(
            Anim.To(_wren.Tilt, RotateTransform.AngleProperty, 84 * _wren.Facing.ScaleX, 700, Anim.EaseOut),
            Anim.To(_wren.Body, OpacityProperty, 0.45, 900));
        Ripple(FeetOf(_wren), TrailColor);
    }

    private async Task TidewifeBanishedAsync()
    {
        var f = _tidewife;
        f.Body.BeginAnimation(OpacityProperty, null);
        f.Tilt.BeginAnimation(RotateTransform.AngleProperty, null);
        var feet = FeetOf(f);
        Sound.Play("hit");
        Float(feet + new Vector(0, -80), "BANISHED", Art.Accent, 22);
        for (int i = 0; i < 3; i++)
        {
            Ripple(feet, TrailColor, 1.4 + i * 0.6);
            await Anim.Wait(160);
        }
        await Task.WhenAll(
            Anim.To(f.Facing, ScaleTransform.ScaleYProperty, 0, 1300, Anim.EaseInOut),
            Anim.To(f.Body, OpacityProperty, 0, 1300));
    }

    // ------------------------------------------------------------------ effects

    private void Ripple(Point center, Color color, double size = 1.6)
    {
        var scale = new ScaleTransform(0.2, 0.2);
        var ring = new Ellipse
        {
            Width = 60, Height = 20,
            Stroke = new SolidColorBrush(color),
            StrokeThickness = 2,
            RenderTransform = scale,
            RenderTransformOrigin = new Point(0.5, 0.5),
            IsHitTestVisible = false
        };
        Canvas.SetLeft(ring, center.X - 30);
        Canvas.SetTop(ring, center.Y - 10);
        FxLayer.Children.Add(ring);
        _ = Anim.To(scale, ScaleTransform.ScaleXProperty, size, 750, Anim.EaseOut);
        _ = Anim.To(scale, ScaleTransform.ScaleYProperty, size, 750, Anim.EaseOut);
        Anim.To(ring, OpacityProperty, 0, 750, null, 0.9).ContinueWith(
            _ => FxLayer.Children.Remove(ring), TaskScheduler.FromCurrentSynchronizationContext());
    }

    private async Task DelayedPuddleAsync(Point p, double delayMs)
    {
        await Anim.Wait(delayMs);
        var puddle = new Ellipse
        {
            Width = 14, Height = 5,
            Fill = new SolidColorBrush(Color.FromArgb(150, TrailColor.R, TrailColor.G, TrailColor.B)),
            IsHitTestVisible = false
        };
        Canvas.SetLeft(puddle, p.X - 7);
        Canvas.SetTop(puddle, p.Y - 2);
        FxLayer.Children.Add(puddle);
        await Anim.To(puddle, OpacityProperty, 0, 1800);
        FxLayer.Children.Remove(puddle);
    }

    /// <summary>Text that rises and fades: damage numbers, "+1 saved", etc.</summary>
    private void Float(Point at, string text, Brush brush, double size = 16)
    {
        var lift = new TranslateTransform();
        var label = new TextBlock
        {
            Text = text,
            Width = 220,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            FontFamily = new FontFamily("Georgia"),
            FontWeight = FontWeights.Bold,
            FontSize = size,
            Foreground = brush,
            RenderTransform = lift,
            IsHitTestVisible = false,
            Effect = new DropShadowEffect { Color = Colors.Black, BlurRadius = 6, ShadowDepth = 0, Opacity = 1 }
        };
        Canvas.SetLeft(label, at.X - 110);
        Canvas.SetTop(label, at.Y - size);
        FxLayer.Children.Add(label);

        _ = Anim.To(lift, TranslateTransform.YProperty, -38, 1500, Anim.EaseOut);
        var fade = new DoubleAnimation(1, 0, new Duration(TimeSpan.FromMilliseconds(700 * Anim.Speed)))
        {
            BeginTime = TimeSpan.FromMilliseconds(900 * Anim.Speed)
        };
        fade.Completed += (_, _) => FxLayer.Children.Remove(label);
        label.BeginAnimation(OpacityProperty, fade);
    }

    private async Task PulseAsync(UIElement overlay, double peak, double upMs, double downMs)
    {
        await Anim.To(overlay, OpacityProperty, peak, upMs, Anim.EaseOut);
        await Anim.To(overlay, OpacityProperty, 0, downMs, Anim.EaseInOut);
    }

    // ------------------------------------------------------------------ after a player action

    private async Task AnimateActionAsync(Snapshot before)
    {
        var g = _game;

        // Dice first: they decide everything else. Converting partials re-uses the same dice, so no tumble then.
        bool converted = g.LastRoll is not null && before.Roll is not null && !ReferenceEquals(g.LastRoll, before.Roll)
                         && ReferenceEquals(g.LastRoll.Values, before.Roll.Values);
        if (converted)
        {
            _trayRoll = g.LastRoll;
            RenderDice();
            if (g.LastRoll!.Converted > 0)
            {
                Sound.Play("card_discard");
                Float(FeetOf(_wren) + new Vector(0, -60), $"+{g.LastRoll.Converted} ★ for {g.LastRoll.Converted * Game.CardsPerConversion} cards", Art.Warm, 16);
                await Anim.Wait(450);
            }
        }
        else if (g.LastRoll is not null && !ReferenceEquals(g.LastRoll, before.Roll))
            await RollDiceAsync(g.LastRoll, enemy: false);

        // Partials to trade in: the choice panel takes over; the result plays out after the choice.
        if (g.Pending is not null)
        {
            Sound.Play("card_play", 0.6);
            return;
        }

        if (g.SurvivorLocation != before.Survivor)
        {
            int from = before.Survivor;
            int followers = Math.Max(0, before.Villagers[from] - g.Villagers[from]);
            HideVillagerFigures(from, followers);
            await WalkWrenAsync(from, g.SurvivorLocation, followers);
        }
        else
        {
            var walks = new List<Task>();
            foreach (int n in Board.Neighbours(g.SurvivorLocation))
            {
                int moved = before.Villagers[n] - g.Villagers[n];
                if (moved <= 0) continue;
                HideVillagerFigures(n, moved);
                walks.Add(VillagersWalkAsync(n, g.SurvivorLocation, moved));
            }
            await Task.WhenAll(walks);
        }

        if (g.TidewifeLocation != before.Tidewife)
            await GlideAsync(before.Tidewife, g.TidewifeLocation);

        // Card and item sounds.
        if (g.Hand.Count > before.HandCount && g.Market.Count < before.MarketCount) Sound.Play("card_buy");
        else if (g.Hand.Count < before.HandCount && g.Time > before.Time) Sound.Play("card_discard");
        else if (g.Hand.Count < before.HandCount) Sound.Play("card_play");
        bool searched = g.LastRoll is not null && !ReferenceEquals(g.LastRoll, before.Roll)
                        && (g.LastRoll.Label.StartsWith("Search") || g.LastRoll.Label.StartsWith("Rummage") || g.LastRoll.Label.StartsWith("Glance") || g.LastRoll.Label.StartsWith("Peek"));
        if (searched) Sound.Play(g.Inventory.Count > before.InventoryCount ? "found" : "nothing");

        int wounds = before.TidewifeHealth - g.TidewifeHealth;
        if (wounds > 0)
        {
            Sound.Play("hit");
            var her = FeetOf(_tidewife);
            Float(her + new Vector(0, -70), $"−{wounds}", Art.Accent, 24);
            Ripple(her, Art.C("#E8F5F2"));
            await FlashAsync(_tidewife);
        }
        else if (g.LastRoll is not null && !ReferenceEquals(g.LastRoll, before.Roll) && g.TidewifeHere
                 && g.LastRoll.Successes == 0 && !searched)
        {
            Sound.Play("miss");
            Float(FeetOf(_tidewife) + new Vector(0, -70), "Miss", Art.Dim, 18);
        }

        // Discarded a card for Time.
        int gained = g.Time - before.Time;
        if (gained > 0 && g.Hand.Count < before.HandCount)
            Float(FeetOf(_wren) + new Vector(0, -60), $"+{gained} Time", Art.Accent, 17);

        int healed = g.Health - before.Health;
        if (healed > 0)
        {
            Sound.Play("heal", 0.8);
            Float(FeetOf(_wren) + new Vector(0, -60), $"+{healed} ♥", Art.Accent, 18);
        }

        int saved = g.VillagersSaved - before.Saved;
        if (saved > 0)
        {
            Sound.Play("saved");
            var lh = Spot(Board.Sanctuary);
            Float(lh + new Vector(0, -40), $"+{saved} saved ★", Art.Warm, 18);
            Ripple(lh + new Vector(0, 20), Art.C("#E8C26A"), 2.2);
            await Anim.Wait(350);
        }

        if (g.Phase == Phase.Won)
            await TidewifeBanishedAsync();
    }
}
