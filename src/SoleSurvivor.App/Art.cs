using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using SoleSurvivor.Core;
using WpfPath = System.Windows.Shapes.Path;
using Ellipse = System.Windows.Shapes.Ellipse;
using Rectangle = System.Windows.Shapes.Rectangle;
using Line = System.Windows.Shapes.Line;

namespace SoleSurvivor.App;

/// <summary>
/// All vector art. Shapes are written in path mini-language (same syntax as SVG paths)
/// so they stay compact and easy to tweak.
/// </summary>
internal static partial class Art
{
    // ------------------------------------------------------------------ palette

    public static readonly SolidColorBrush Accent = Hex("#6CC3B0");
    public static readonly SolidColorBrush Danger = Hex("#C8553D");
    public static readonly SolidColorBrush Warm = Hex("#E8C26A");
    public static readonly SolidColorBrush TextColor = Hex("#D9E4E1");
    public static readonly SolidColorBrush Dim = Hex("#8FA6A5");
    public static readonly SolidColorBrush Edge = Hex("#2A4750");
    public static readonly SolidColorBrush Empty = Hex("#22363D");

    public static SolidColorBrush Hex(string hex)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        brush.Freeze();
        return brush;
    }

    public static Color C(string hex) => (Color)ColorConverter.ConvertFromString(hex);

    public static WpfPath P(string data, string fill, string? stroke = null, double thickness = 1)
    {
        var geometry = Geometry.Parse(data);
        geometry.Freeze();
        return new WpfPath
        {
            Data = geometry,
            Fill = fill == "none" ? null : Hex(fill),
            Stroke = stroke is null ? null : Hex(stroke),
            StrokeThickness = thickness,
            StrokeLineJoin = PenLineJoin.Round,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round
        };
    }

    internal static Canvas Group(double width, double height, params UIElement[] parts)
    {
        var canvas = new Canvas { Width = width, Height = height };
        foreach (var part in parts) canvas.Children.Add(part);
        return canvas;
    }

    internal static Ellipse Glow(double cx, double cy, double radius, Color color, double alpha)
    {
        var brush = new RadialGradientBrush(Color.FromArgb((byte)(255 * alpha), color.R, color.G, color.B), Color.FromArgb(0, color.R, color.G, color.B));
        brush.Freeze();
        var e = new Ellipse { Width = radius * 2, Height = radius * 2, Fill = brush, IsHitTestVisible = false };
        Canvas.SetLeft(e, cx - radius);
        Canvas.SetTop(e, cy - radius);
        return e;
    }

    // ------------------------------------------------------------------ locations
    // Each icon is drawn in an 84 x 64 box. (42, 62) is the "ground" point that sits just above the location's name plaque.

    public const double IconWidth = 84, IconHeight = 64;

    private static Canvas GallowmouthIcon(int id) => id switch
    {
        0 => Lighthouse(),
        1 => CliffPath(),
        2 => Chapel(),
        3 => Inn(),
        4 => Cottages(),
        5 => Harbour(),
        6 => Boathouse(),
        7 => Cannery(),
        _ => new Canvas()
    };

    private static Canvas Lighthouse() => Group(IconWidth, IconHeight,
        P("M6,64 Q12,48 30,46 L56,46 Q74,48 80,64 Z", "#3A403C", "#272B28"),
        P("M33,50 L51,50 L47,6 L37,6 Z", "#E6E1D6", "#9C978C"),
        P("M34.3,38 L49.7,38 L49,30 L35,30 Z", "#B9473A"),
        P("M35.8,22 L48.2,22 L47.6,15 L36.4,15 Z", "#B9473A"),
        P("M40,50 L44,50 L44,43 Q42,41 40,43 Z", "#3B2B20"),
        Glow(42, -1, 22, C("#FFE08A"), 0.75),
        P("M34,4 L50,4 L50,7 L34,7 Z", "#2B2B2B"),
        P("M37,-6 L47,-6 L47,4 L37,4 Z", "#FFE08A", "#C9A14A"),
        P("M35,-6 L49,-6 L42,-14 Z", "#B9473A", "#7A2E25"));

    private static Canvas CliffPath() => Group(IconWidth, IconHeight,
        P("M2,62 L12,40 L24,44 L33,28 L46,35 L58,22 L70,33 L82,62 Z", "#4A504C", "#2C302D", 1.2),
        P("M24,44 L33,28 L38,40 Z M58,22 L70,33 L62,36 Z", "#5C635E"),
        P("M30,63 Q42,52 36,42 Q30,32 44,24", "none", "#8A7A5A", 3),
        P("M8,62 A8,5 0 1 1 24,62 Z", "#3F5A32"),
        P("M60,62 A9,6 0 1 1 78,62 Z", "#3F5A32"),
        P("M12,58 A1.5,1.5 0 1 1 15,58 A1.5,1.5 0 1 1 12,58 Z M18,57 A1.5,1.5 0 1 1 21,57 A1.5,1.5 0 1 1 18,57 Z M66,57 A1.5,1.5 0 1 1 69,57 A1.5,1.5 0 1 1 66,57 Z M72,59 A1.5,1.5 0 1 1 75,59 A1.5,1.5 0 1 1 72,59 Z", "#E3C24A"),
        P("M52,62 L52,44", "none", "#6B5236", 2),
        P("M46,44 L60,44 L58,50 L46,50 Z", "#8A6A40", "#5A4428"));

    private static Canvas Chapel() => Group(IconWidth, IconHeight,
        P("M4,62 L4,54 Q7,51 10,54 L10,62 Z M70,62 L70,55 Q73,52 76,55 L76,62 Z", "#6F6E68"),
        P("M18,62 L18,34 L50,34 L50,62 Z", "#8E8A80", "#5D5A53"),
        P("M14,36 L34,20 L54,36 Z", "#3E3A38", "#2A2726"),
        P("M50,62 L50,22 L64,22 L64,62 Z", "#9A968C", "#5D5A53"),
        P("M48,24 L57,2 L66,24 Z", "#3E3A38", "#2A2726"),
        P("M56,-7 L58,-7 L58,2 L56,2 Z M53.5,-4 L60.5,-4 L60.5,-2 L53.5,-2 Z", "#CFC8B8"),
        P("M54,31 L54,27 Q57,24 60,27 L60,31 Z", "#1F1F1F"),
        Glow(32, 47, 14, C("#E8C26A"), 0.45),
        P("M28,53 L28,45 Q32,39 36,45 L36,53 Z", "#E8C26A", "#8A6F35"),
        P("M40,62 L40,55 Q43,52 46,55 L46,62 Z", "#3B2B20"));

    private static Canvas Inn()
    {
        var c = Group(IconWidth, IconHeight,
            P("M54,24 L54,10 L60,10 L60,28 Z", "#4A3A30"),
            P("M12,62 L12,30 L66,30 L66,62 Z", "#6B5442", "#3F3127"),
            P("M12,40 L66,40 M12,52 L66,52", "none", "#56432F", 1),
            P("M6,32 L39,12 L72,32 Z", "#3A2D25", "#231B16"),
            Glow(23, 44, 12, C("#E8C26A"), 0.4),
            Glow(55, 44, 12, C("#E8C26A"), 0.4),
            P("M18,40 L28,40 L28,48 L18,48 Z M50,40 L60,40 L60,48 L50,48 Z", "#E8C26A", "#8A6F35"),
            P("M34,62 L34,46 L44,46 L44,62 Z", "#2B1F18"),
            P("M66,36 L77,36 M74,36 L74,38 M79,36 L79,38", "none", "#2B2B2B", 1.5),
            P("M70,38 L83,38 L83,49 L70,49 Z", "#8A6A40", "#5A4428"));
        var anchor = new TextBlock { Text = "⚓", FontSize = 9, Foreground = Hex("#2B1F18"), IsHitTestVisible = false };
        Canvas.SetLeft(anchor, 72);
        Canvas.SetTop(anchor, 37);
        c.Children.Add(anchor);
        return c;
    }

    private static Canvas Cottages() => Group(IconWidth, IconHeight,
        P("M2,62 L2,42 L24,42 L24,62 Z", "#7A6F60", "#4D463D"),
        P("M0,44 L13,32 L26,44 Z", "#4B3F36", "#2E2621"),
        P("M8,48 L14,48 L14,54 L8,54 Z", "#E8C26A"),
        P("M33,30 L33,22 L38,22 L38,28 Z", "#4B3F36"),
        P("M30,60 L30,36 L56,36 L56,60 Z", "#857A6A", "#4D463D"),
        P("M27,38 L43,24 L59,38 Z", "#3F352D", "#2E2621"),
        Glow(38.5, 45.5, 11, C("#E8C26A"), 0.4),
        P("M35,42 L42,42 L42,49 L35,49 Z", "#E8C26A", "#8A6F35"),
        P("M46,60 L46,50 L51,50 L51,60 Z", "#2B1F18"),
        P("M60,62 L60,44 L82,44 L82,62 Z", "#6F6656", "#4D463D"),
        P("M58,46 L71,34 L84,46 Z", "#4B3F36", "#2E2621"),
        P("M66,50 L72,50 L72,56 L66,56 Z", "#141414"),
        P("M74,62 L74,54 L78,54 L74,62 Z", "#2B1F18"));

    private static Canvas Harbour() => Group(IconWidth, IconHeight,
        P("M44,30 L78,30 Q72,41 50,41 Z", "#8B3A2E", "#5A251D"),
        P("M58,30 L60,30 L60,2 L58,2 Z", "#3A2A1A"),
        P("M61,4 L75,26 L61,26 Z", "#D8D0BC", "#9E9786"),
        P("M2,46 L80,46 L80,52 L2,52 Z", "#6A5236", "#46361F"),
        P("M2,49 L80,49", "none", "#54412A", 1),
        P("M6,52 L9,52 L9,63 L6,63 Z M38,52 L41,52 L41,63 L38,63 Z M72,52 L75,52 L75,63 L72,63 Z", "#4A3824"),
        P("M16,46 L16,40 L22,40 L22,46 Z", "#3A3A3A"),
        P("M30,46 L30,20", "none", "#2B2B2B", 2),
        Glow(30, 18, 12, C("#FFE08A"), 0.6),
        P("M27,15 L33,15 L33,21 L27,21 Z", "#FFE08A", "#8A6F35"));

    private static Canvas Boathouse() => Group(IconWidth, IconHeight,
        P("M8,62 L8,30 L76,30 L76,62 Z", "#5A4632", "#3A2D20"),
        P("M8,38 L76,38 M8,46 L76,46 M8,54 L76,54", "none", "#4A3828", 1),
        P("M2,32 L42,12 L82,32 Z", "#2F2620", "#1E1814"),
        P("M24,62 L24,38 L60,38 L60,62 Z", "#1C1612", "#3A2E24"),
        P("M42,38 L42,62", "none", "#3A2E24", 1.5),
        P("M58,64 Q71,48 84,64 Z", "#8B3A2E", "#5A251D"),
        P("M0,64 Q8,56 16,64 Z", "#6A6A6A"));

    private static Canvas Cannery() => Group(IconWidth, IconHeight,
        P("M4,62 L4,30 L64,30 L64,62 Z", "#6E6A63", "#46433E"),
        P("M4,30 L4,22 L16,30 L16,22 L28,30 L28,22 L40,30 L40,22 L52,30 L52,22 L64,30 Z", "#3C3A36", "#26241F"),
        P("M68,62 L68,8 L76,8 L76,62 Z", "#7A4A3A", "#4E2F25"),
        P("M66,8 L78,8 L78,4 L66,4 Z", "#4E2F25"),
        P("M10,40 L18,40 L18,48 L10,48 Z M38,40 L46,40 L46,48 L38,48 Z", "#1A1A1A"),
        Glow(28, 44, 10, C("#E8C26A"), 0.35),
        P("M24,40 L32,40 L32,48 L24,48 Z", "#E8C26A", "#8A6F35"),
        P("M50,62 L50,46 L60,46 L60,62 Z", "#2B2520"),
        P("M10,62 L10,54 Q14,52 18,54 L18,62 Z M20,62 L20,55 Q24,53 28,55 L28,62 Z", "#5A4632", "#3A2D20"));

    /// <summary>Smoke puffs that drift up out of the cannery chimney forever.</summary>
    public static void AddChimneySmoke(Canvas layer, double chimneyX, double chimneyY)
    {
        for (int i = 0; i < 3; i++)
        {
            var puff = new Ellipse { Width = 12, Height = 9, Fill = Hex("#9AA3A3"), Opacity = 0, IsHitTestVisible = false };
            var move = new TranslateTransform();
            puff.RenderTransform = move;
            Canvas.SetLeft(puff, chimneyX - 6);
            Canvas.SetTop(puff, chimneyY - 6);
            layer.Children.Add(puff);

            var begin = TimeSpan.FromSeconds(i * 1.1);
            var duration = new Duration(TimeSpan.FromSeconds(3.3));
            move.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(0, -34, duration) { BeginTime = begin, RepeatBehavior = RepeatBehavior.Forever });
            move.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(0, 14, duration) { BeginTime = begin, RepeatBehavior = RepeatBehavior.Forever });
            var fade = new DoubleAnimationUsingKeyFrames { BeginTime = begin, RepeatBehavior = RepeatBehavior.Forever, Duration = duration };
            fade.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromPercent(0)));
            fade.KeyFrames.Add(new LinearDoubleKeyFrame(0.35, KeyTime.FromPercent(0.2)));
            fade.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromPercent(1)));
            puff.BeginAnimation(UIElement.OpacityProperty, fade);
        }
    }

    // ------------------------------------------------------------------ scenery

    /// <summary>The coast: land, sea along the west and south, rocks, grass, fog and the lighthouse beam.</summary>
    public static void DrawScenery(Canvas scene, double width, double height, string scenarioId)
    {
        var theme = ThemeFor(scenarioId);
        if (!theme.HasSea)
        {
            DrawInlandScenery(scene, width, height, scenarioId, theme);
            return;
        }
        // Land.
        var land = new LinearGradientBrush(C("#1E2E26"), C("#16231D"), 90);
        land.Freeze();
        scene.Children.Add(new Rectangle { Width = width, Height = height, Fill = land });

        // Grass tufts and stones, scattered deterministically.
        var rng = new Random(7);
        var tufts = new System.Text.StringBuilder();
        var stones = new System.Text.StringBuilder();
        for (int i = 0; i < 260; i++)
        {
            double x = rng.NextDouble() * width, y = rng.NextDouble() * (height - 90);
            if (x < 60) continue;
            if (i % 9 == 0)
                stones.Append($"M{x:F0},{y:F0} a3,2 0 1 1 6,0 a3,2 0 1 1 -6,0 Z ");
            else
                tufts.Append($"M{x:F0},{y:F0} l-2,-5 M{x:F0},{y:F0} l1,-6 M{x:F0},{y:F0} l3,-4 ");
        }
        var tuftPath = P(tufts.ToString(), "none", "#2D4536", 1);
        tuftPath.Opacity = 0.8;
        scene.Children.Add(tuftPath);
        scene.Children.Add(P(stones.ToString(), "#2C3530"));

        // Fields, walls, heather, the stone circle's ground, the mill pond (see Scenery.cs).
        DrawSceneryGroup(scene, "ground", scenarioId);
        // Shingle beach, half of which the sea covers.
        DrawSceneryGroup(scene, "under_sea", scenarioId);

        // Sea: a strip down the west side and along the south.
        var sea = new LinearGradientBrush(C("#14414C"), C("#0A2229"), 90);
        sea.Freeze();
        var seaShape = P("M0,0 L34,0 Q52,60 30,140 Q14,220 34,300 Q56,380 40,440 Q60,512 160,520 Q300,532 420,526 Q560,518 680,524 Q760,528 800,520 L800,580 L0,580 Z", "#0A2229");
        seaShape.Fill = sea;
        scene.Children.Add(seaShape);
        // Surf line along the shore.
        var surf = P("M34,0 Q52,60 30,140 Q14,220 34,300 Q56,380 40,440 Q60,512 160,520 Q300,532 420,526 Q560,518 680,524 Q760,528 800,520", "none", "#9FD8CC", 2);
        surf.Opacity = 0.35;
        scene.Children.Add(surf);

        // Moving waves.
        for (int row = 0; row < 4; row++)
        {
            var wave = new System.Text.StringBuilder($"M-60,{548 + row * 9} ");
            for (int x = -60; x < width + 60; x += 30) wave.Append($"q7.5,-4 15,0 q7.5,4 15,0 ");
            var w = P(wave.ToString(), "none", "#6CC3B0", 1.3);
            w.Opacity = 0.18 + row * 0.06;
            var shift = new TranslateTransform();
            w.RenderTransform = shift;
            shift.BeginAnimation(TranslateTransform.XProperty,
                new DoubleAnimation(0, row % 2 == 0 ? 30 : -30, new Duration(TimeSpan.FromSeconds(2.6 + row * 0.7))) { RepeatBehavior = RepeatBehavior.Forever });
            scene.Children.Add(w);
        }
        for (int row = 0; row < 3; row++)
        {
            var wave = new System.Text.StringBuilder($"M{6 + row * 8},-40 ");
            for (int y = -40; y < 470; y += 30) wave.Append($"q-4,7.5 0,15 q4,7.5 0,15 ");
            var w = P(wave.ToString(), "none", "#6CC3B0", 1.2);
            w.Opacity = 0.15 + row * 0.05;
            var shift = new TranslateTransform();
            w.RenderTransform = shift;
            shift.BeginAnimation(TranslateTransform.YProperty,
                new DoubleAnimation(0, 30, new Duration(TimeSpan.FromSeconds(3 + row * 0.8))) { RepeatBehavior = RepeatBehavior.Forever });
            scene.Children.Add(w);
        }

        // Cliff edge, rocks with foam, the wreck.
        DrawSceneryGroup(scene, "over_sea", scenarioId);
        AddSeaLife(scene);
        // Woods, bushes, the dead tree, the standing stones.
        DrawSceneryGroup(scene, "top", scenarioId);
    }

    /// <summary>Snowfield or walled grounds: land, texture, then the scenario's baked scenery.</summary>
    private static void DrawInlandScenery(Canvas scene, double width, double height, string scenarioId, Theme theme)
    {
        var land = new LinearGradientBrush(C(theme.LandTop), C(theme.LandBottom), 90);
        land.Freeze();
        scene.Children.Add(new Rectangle { Width = width, Height = height, Fill = land });

        if (theme.Indoor)
        {
            // Floorboards: long planks with staggered joints, instead of grass and stones.
            var boards = new System.Text.StringBuilder();
            var joints = new System.Text.StringBuilder();
            var r = new Random(11);
            for (double y = 0; y < height; y += 12)
            {
                boards.Append($"M0,{y:F0} H{width:F0} ");
                for (double x = r.Next(0, 90); x < width; x += 70 + r.Next(0, 60))
                    joints.Append($"M{x:F0},{y:F0} v12 ");
            }
            scene.Children.Add(P(boards.ToString(), "none", theme.Tufts, 1).Also(e => e.Opacity = 0.7));
            scene.Children.Add(P(joints.ToString(), "none", theme.Stones, 1).Also(e => e.Opacity = 0.8));
            DrawSceneryGroup(scene, "ground", scenarioId);
            DrawSceneryGroup(scene, "top", scenarioId);
            return;
        }

        var rng = new Random(7);
        var tufts = new System.Text.StringBuilder();
        var stones = new System.Text.StringBuilder();
        for (int i = 0; i < 220; i++)
        {
            int x = rng.Next((int)width), y = rng.Next((int)height);
            if (i % 9 == 0) stones.Append($"M{x},{y} a3,2 0 1 1 6,0 a3,2 0 1 1 -6,0 Z ");
            else tufts.Append($"M{x},{y} l-2,-5 M{x},{y} l1,-6 M{x},{y} l3,-4 ");
        }
        var tuftPath = P(tufts.ToString(), "none", theme.Tufts, 1);
        tuftPath.Opacity = 0.7;
        scene.Children.Add(tuftPath);
        scene.Children.Add(P(stones.ToString(), theme.Stones));

        DrawSceneryGroup(scene, "ground", scenarioId);
        DrawSceneryGroup(scene, "top", scenarioId);
    }

    private static void DrawSceneryGroup(Canvas scene, string group, string scenarioId)
    {
        foreach (var layer in Scenery.For(scenarioId))
        {
            if (layer.Group != group) continue;
            var path = P(layer.Data, layer.Fill, layer.Stroke, layer.Thickness);
            path.Opacity = layer.Opacity;
            path.IsHitTestVisible = false;
            if (layer.Dashed)
            {
                // Dry-stone wall texture (units are multiples of the stroke thickness).
                path.StrokeDashArray = new DoubleCollection { 2, 1 };
                path.StrokeStartLineCap = PenLineCap.Flat;
                path.StrokeEndLineCap = PenLineCap.Flat;
            }
            scene.Children.Add(path);
        }
    }

    /// <summary>Boats rocking at anchor and moonlight glinting on the water.</summary>
    private static void AddSeaLife(Canvas scene)
    {
        AddBoat(scene, 232, 556, Group(30, 14,
            P("M3,10 Q15,17 27,10 L24,7 L6,7 Z", "#5A3A2A", "#2E1E15")), 15, 10, 2.6);
        AddBoat(scene, 530, 560, Group(36, 34,
            P("M2,28 Q18,36 34,28 L30,24 L6,24 Z", "#7A3A2E", "#3A1E15"),
            P("M17,24 L19,24 L19,4 L17,4 Z", "#3A2A1A"),
            P("M19,6 L28,21 L19,21 Z", "#B8B0A0", "#8A8474", 0.6)), 18, 28, 3.3);

        var glints = new (double X, double Y, double Period)[]
        {
            (110, 548, 2.1), (300, 568, 2.9), (440, 571, 2.4), (612, 546, 3.3), (705, 569, 2.7),
            (18, 150, 3.1), (12, 300, 2.5), (24, 470, 3.6), (770, 566, 2.2)
        };
        foreach (var (x, y, period) in glints)
        {
            var glint = P($"M{x - 5},{y} L{x + 5},{y} M{x - 2},{y + 3} L{x + 3},{y + 3}", "none", "#DDF2EC", 1.2);
            glint.IsHitTestVisible = false;
            glint.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0.05, 0.7, new Duration(TimeSpan.FromSeconds(period)))
            {
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            });
            scene.Children.Add(glint);
        }
    }

    private static void AddBoat(Canvas scene, double x, double y, Canvas boat, double anchorX, double anchorY, double period)
    {
        boat.IsHitTestVisible = false;
        var rock = new RotateTransform(0, anchorX, anchorY);
        var bob = new TranslateTransform();
        var group = new TransformGroup();
        group.Children.Add(rock);
        group.Children.Add(bob);
        boat.RenderTransform = group;
        Canvas.SetLeft(boat, x - anchorX);
        Canvas.SetTop(boat, y - anchorY);
        var ease = new SineEase { EasingMode = EasingMode.EaseInOut };
        rock.BeginAnimation(RotateTransform.AngleProperty, new DoubleAnimation(-4, 4, new Duration(TimeSpan.FromSeconds(period)))
        { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever, EasingFunction = ease });
        bob.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(-1, 1.5, new Duration(TimeSpan.FromSeconds(period * 0.7)))
        { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever, EasingFunction = ease });
        scene.Children.Add(boat);
    }

    /// <summary>Warm light spilling onto the ground around each building.</summary>
    public static void AddLightPools(Canvas layer)
    {
        foreach (var loc in Board.Locations)
            layer.Children.Add(Glow(loc.X, loc.Y - 8, loc.IsSanctuary ? 105 : 82, C("#E8C26A"), loc.IsSanctuary ? 0.2 : 0.12));
    }

    /// <summary>An eerie glow over the standing stones. The holder's opacity is driven by Dread.</summary>
    public static Canvas AddStoneGlow(Canvas layer)
    {
        var holder = new Canvas { IsHitTestVisible = false };
        var glow = Glow(Scenery.StoneCircleX, Scenery.StoneCircleY - 6, 48, C("#7CF5C8"), 0.45);
        glow.RenderTransform = new ScaleTransform(1, 0.6, 48, 48);
        glow.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0.35, 1, new Duration(TimeSpan.FromSeconds(2.8)))
        {
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever,
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
        });
        holder.Children.Add(glow);
        layer.Children.Add(holder);
        return holder;
    }

    /// <summary>Darkened edges, like lamplight fading into the night.</summary>
    public static void AddVignette(Canvas layer, double width, double height)
    {
        var brush = new RadialGradientBrush { RadiusX = 0.75, RadiusY = 0.75 };
        brush.GradientStops.Add(new GradientStop(Color.FromArgb(0, 0, 0, 0), 0.55));
        brush.GradientStops.Add(new GradientStop(Color.FromArgb(140, 0, 0, 0), 1));
        brush.Freeze();
        layer.Children.Add(new Rectangle { Width = width, Height = height, Fill = brush, IsHitTestVisible = false });
    }

    /// <summary>Slow drifting fog banks over the whole board.</summary>
    public static void AddFog(Canvas layer, double width, double height)
    {
        var rng = new Random(11);
        for (int i = 0; i < 5; i++)
        {
            double w = 260 + rng.Next(160), h = 90 + rng.Next(60);
            var brush = new RadialGradientBrush(Color.FromArgb(34, 200, 225, 220), Color.FromArgb(0, 200, 225, 220));
            brush.Freeze();
            var bank = new Ellipse { Width = w, Height = h, Fill = brush, IsHitTestVisible = false };
            Canvas.SetLeft(bank, rng.NextDouble() * (width - w));
            Canvas.SetTop(bank, rng.NextDouble() * (height - h));
            var drift = new TranslateTransform();
            bank.RenderTransform = drift;
            drift.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(-60, 60, new Duration(TimeSpan.FromSeconds(14 + i * 3)))
            {
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            });
            layer.Children.Add(bank);
        }
    }

    /// <summary>The lighthouse beam sweeping round. Centred on the lamp.</summary>
    public static void AddBeam(Canvas layer, double lampX, double lampY)
    {
        var brush = new LinearGradientBrush(Color.FromArgb(70, 255, 224, 138), Color.FromArgb(0, 255, 224, 138), 0);
        brush.Freeze();
        var beam = new WpfPath
        {
            Data = Geometry.Parse("M0,0 L520,-55 L520,55 Z"),
            Fill = brush,
            IsHitTestVisible = false
        };
        var spin = new RotateTransform(0, 0, 0);
        beam.RenderTransform = spin;
        Canvas.SetLeft(beam, lampX);
        Canvas.SetTop(beam, lampY);
        spin.BeginAnimation(RotateTransform.AngleProperty, new DoubleAnimation(0, 360, new Duration(TimeSpan.FromSeconds(9))) { RepeatBehavior = RepeatBehavior.Forever });
        layer.Children.Add(beam);
    }

    /// <summary>Dirt track between two locations.</summary>
    public static IEnumerable<UIElement> Road(double x1, double y1, double x2, double y2, Theme theme)
    {
        yield return new Line { X1 = x1, Y1 = y1, X2 = x2, Y2 = y2, Stroke = Hex(theme.RoadOuter), StrokeThickness = 11, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round };
        yield return new Line { X1 = x1, Y1 = y1, X2 = x2, Y2 = y2, Stroke = Hex(theme.RoadInner), StrokeThickness = 7, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round };
        yield return new Line { X1 = x1, Y1 = y1, X2 = x2, Y2 = y2, Stroke = Hex(theme.RoadDash), StrokeThickness = 1.5, StrokeDashArray = new DoubleCollection { 2, 5 } };
    }

    // ------------------------------------------------------------------ characters

    /// <summary>Parts of a character token the animations need to reach.</summary>
    public sealed class Figure
    {
        public required Canvas Root { get; init; }          // positioned via Position
        public required Canvas Body { get; init; }          // the drawing; flickers, sinks, falls
        public required TranslateTransform Position { get; init; }
        public required ScaleTransform Facing { get; init; }  // ScaleX -1 to face left
        public required TranslateTransform Bob { get; init; }
        public required RotateTransform Tilt { get; init; }
        public RotateTransform? LegLeft { get; init; }
        public RotateTransform? LegRight { get; init; }
        public double FootX { get; init; }
        public double FootY { get; init; }
    }

    internal static Figure Wrap(Canvas body, double width, double height, double footX, double footY, RotateTransform? legL = null, RotateTransform? legR = null)
    {
        var facing = new ScaleTransform(1, 1);
        var bob = new TranslateTransform();
        var tilt = new RotateTransform();
        var group = new TransformGroup();
        group.Children.Add(facing);
        group.Children.Add(tilt);
        group.Children.Add(bob);
        body.RenderTransform = group;
        body.RenderTransformOrigin = new Point(footX / width, footY / height);
        body.Width = width;
        body.Height = height;

        var position = new TranslateTransform();
        var root = new Canvas { Width = width, Height = height, RenderTransform = position, IsHitTestVisible = false };
        root.Children.Add(body);
        return new Figure { Root = root, Body = body, Position = position, Facing = facing, Bob = bob, Tilt = tilt, LegLeft = legL, LegRight = legR, FootX = footX, FootY = footY };
    }

    /// <summary>Wren: yellow oilskins, sou'wester, storm lantern. 32 x 48, feet at (16, 48).</summary>
    private static Figure Wren()
    {
        var legL = new RotateTransform(0, 13, 36);
        var legR = new RotateTransform(0, 19, 36);
        var leftLeg = P("M11,36 L15,36 L15,47 L11,47 Z", "#2B2B2B");
        leftLeg.RenderTransform = legL;
        var rightLeg = P("M17,36 L21,36 L21,47 L17,47 Z", "#363636");
        rightLeg.RenderTransform = legR;

        var body = Group(32, 48,
            P("M5,47 A11,2.5 0 1 1 27,47 A11,2.5 0 1 1 5,47 Z", "#000000").Also(e => e.Opacity = 0.45),
            Glow(28, 33, 15, C("#FFD97A"), 0.55),
            rightLeg,
            leftLeg,
            P("M9,17 L23,17 L25.5,38 L6.5,38 Z", "#E8B634", "#9C7A1F"),
            P("M16,18 L16,37", "none", "#9C7A1F", 0.8),
            P("M23,22 L28,30", "none", "#C9981F", 2.6),
            P("M25.5,29 L30.5,29 L30.5,36 L25.5,36 Z", "#FFE08A", "#6B5236"),
            P("M11.5,11.5 A4.5,4.5 0 1 1 20.5,11.5 A4.5,4.5 0 1 1 11.5,11.5 Z", "#E9C9A8", "#B8957A"),
            P("M6,9 Q16,-2 26,9 L28,10.5 L4,10.5 Z", "#E8B634", "#9C7A1F"),
            P("M14,12.8 L15,12.8 M17.5,12.8 L18.5,12.8", "none", "#2B1F18", 1.2));
        return Wrap(body, 32, 48, 16, 47, legL, legR);
    }

    /// <summary>The Tidewife: drowned, pale, hair like kelp, eyes like sea-glass. 36 x 60, feet at (18, 59).</summary>
    private static Figure TidewifeFigure()
    {
        var dress = new LinearGradientBrush(Color.FromArgb(240, 169, 194, 188), Color.FromArgb(60, 120, 160, 150), 90);
        dress.Freeze();
        var gown = P("M10,20 L26,20 L32,55 L28,51 L25,58 L21,52 L17,59 L13,52 L9,58 L4,55 Z", "#A9C2BC", "#6E8F88", 0.8);
        gown.Fill = dress;

        var eyes = P("M14.2,12 A1.2,1 0 1 1 16.6,12 A1.2,1 0 1 1 14.2,12 Z M19.4,12 A1.2,1 0 1 1 21.8,12 A1.2,1 0 1 1 19.4,12 Z", "#8FF5E0");
        eyes.Effect = new DropShadowEffect { Color = C("#8FF5E0"), BlurRadius = 8, ShadowDepth = 0, Opacity = 1 };

        var body = Group(36, 60,
            P("M2,59 A16,3.5 0 1 1 34,59 A16,3.5 0 1 1 2,59 Z", "#2D7A7A").Also(e => e.Opacity = 0.45),
            Glow(18, 30, 26, C("#6CC3B0"), 0.18),
            gown,
            P("M10,22 Q5,34 7,44 M26,22 Q31,34 29,44", "none", "#BFD6D0", 2.4),
            P("M12,7 A6,7 0 1 1 24,7 A6,7 0 1 1 12,7 Z", "#CFE3DE", "#8FB1AA"),
            P("M11,8 Q8,24 10,38 L13.5,36 Q12,22 15,10 Z M25,8 Q28,24 26,38 L22.5,36 Q24,22 21,10 Z M11,9 Q18,-1 25,9 Q18,4 11,9 Z", "#1C2A2A"),
            P("M16,16.6 Q18,15.4 20,16.6", "none", "#4A5E5A", 0.8),
            eyes);
        return Wrap(body, 36, 60, 18, 59);
    }

    private static readonly string[] VillagerCoats = ["#7B8FA6", "#A66B5B", "#6E8F5E", "#9C8456", "#8A6E9C", "#5E8C8C"];

    /// <summary>A villager. 10 x 18, feet at (5, 18).</summary>
    public static Canvas Villager(int seed, string? coatOverride = null)
    {
        string coat = coatOverride ?? VillagerCoats[Math.Abs(seed) % VillagerCoats.Length];
        return Group(10, 18,
            P("M1,17.5 A4,1.2 0 1 1 9,17.5 A4,1.2 0 1 1 1,17.5 Z", "#000000").Also(e => e.Opacity = 0.4),
            P("M3,13 L4.5,13 L4.5,17.5 L3,17.5 Z M5.5,13 L7,13 L7,17.5 L5.5,17.5 Z", "#2B2B2B"),
            P("M2,7 L8,7 L9,14 L1,14 Z", coat, "#2A2A2A", 0.6),
            P("M2.8,3.6 A2.2,2.4 0 1 1 7.2,3.6 A2.2,2.4 0 1 1 2.8,3.6 Z", "#E2C3A0", "#9C7E63", 0.5));
    }

    /// <summary>A portrait for the sidebar: the figure scaled up inside a round frame.</summary>
    public static Border Portrait(Figure figure, Brush ring, Brush background)
    {
        return new Border
        {
            Width = 64,
            Height = 64,
            CornerRadius = new CornerRadius(32),
            Background = background,
            BorderBrush = ring,
            BorderThickness = new Thickness(2.5),
            ClipToBounds = true,
            Child = new Viewbox { Child = figure.Root, Margin = new Thickness(6, 4, 6, 2) }
        };
    }
}

internal static class FluentExtensions
{
    public static T Also<T>(this T value, Action<T> action)
    {
        action(value);
        return value;
    }
}
