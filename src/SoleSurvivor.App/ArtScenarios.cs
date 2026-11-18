using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using SoleSurvivor.Core;
using Ellipse = System.Windows.Shapes.Ellipse;
using Rectangle = System.Windows.Shapes.Rectangle;
using Line = System.Windows.Shapes.Line;

namespace SoleSurvivor.App;

/// <summary>Moving ambience over the board. Motes: dream-dust drifting up. Static: TV interference. Embers: candlelit dust.</summary>
public enum Weather { Fog, Snow, Rain, Motes, Static, Embers }

/// <summary>Colours and ambience for one scenario's board.</summary>
/// <param name="Trail">Killer footprints and ripples.</param>
public sealed record Theme(
    string LandTop, string LandBottom, string Tufts, string Stones,
    string RoadOuter, string RoadInner, string RoadDash,
    Color Trail, Color KillerGlow, Color Accent,
    bool HasSea, Weather Weather, string Board, bool Indoor = false);

/// <summary>Art for every scenario: themes, location icons, figures, weather.</summary>
internal static partial class Art
{
    public static Theme ThemeFor(string scenarioId) => scenarioId switch
    {
        "whitepine" => new("#7C8A99", "#5E6B7A", "#9AA8B6", "#6A7480",
                           "#55606C", "#A7B4C0", "#8796A4",
                           C("#CFE8FF"), C("#BFE6FF"), C("#9FD0F0"), false, Weather.Snow, "#3E4A57"),
        "larkspur" => new("#2A2440", "#1C1830", "#3A3358", "#322C48",
                          "#24202E", "#3E3A4A", "#C9B86A",
                          C("#C8A8FF"), C("#E8D8FF"), C("#C8A8FF"), false, Weather.Motes, "#16122A"),
        "kestrel" => new("#3A3024", "#251E17", "#4A3E2A", "#4A4032",
                         "#1E1E20", "#34343A", "#E8C26A",
                         C("#9FE8FF"), C("#BFF4FF"), C("#9FE8FF"), false, Weather.Static, "#1A1712"),
        "ashgrove" => new("#3A2620", "#26170F", "#4A3024", "#2E1C14",
                          "#3A1418", "#6A1E24", "#C9A04A",
                          C("#E8B060"), C("#F0C070"), C("#E8B060"), false, Weather.Embers, "#1A0F0C", Indoor: true),
        "stagnes" => new("#1D2622", "#151C19", "#2A3A30", "#2C3330",
                         "#2E2B26", "#6E6A60", "#5A574F",
                         C("#B9C9A0"), C("#C8553D"), C("#B9C9A0"), false, Weather.Rain, "#141A18"),
        _ => new("#1E2E26", "#16231D", "#2D4536", "#2C3530",
                 "#2A241A", "#4E4231", "#3A3125",
                 C("#6CC3B0"), C("#6CC3B0"), C("#6CC3B0"), true, Weather.Fog, "#0A2229"),
    };

    // ------------------------------------------------------------------ dispatch

    public static Canvas LocationIcon(string scenarioId, int id)
    {
        var icon = scenarioId switch
        {
            "whitepine" => id switch
            {
                0 => RangerStation(), 1 => Trailhead(), 2 => SkiLodge(), 3 => Cabins(),
                4 => Sawmill(), 5 => FireTower(), 6 => FrozenLake(), 7 => OldMine(), _ => new Canvas()
            },
            "stagnes" => id switch
            {
                0 => Gatehouse(), 1 => Courtyard(), 2 => GothicChapel(), 3 => Ward(false),
                4 => Ward(true), 5 => RecordsOffice(), 6 => BoilerHouse(), 7 => Morgue(), _ => new Canvas()
            },
            "larkspur" or "kestrel" or "ashgrove" => ExtraIcon(scenarioId, id),
            _ => GallowmouthIcon(id)
        };
        icon.Width = IconWidth;
        icon.Height = IconHeight;
        icon.IsHitTestVisible = false;
        return icon;
    }

    public static Figure Survivor(string scenarioId) => scenarioId switch
    {
        "whitepine" => Dana(),
        "stagnes" => Eli(),
        "larkspur" => Maya(),
        "kestrel" => Ruth(),
        "ashgrove" => Clara(),
        _ => Wren()
    };

    public static Figure Killer(string scenarioId) => scenarioId switch
    {
        "whitepine" => HollowStag(),
        "stagnes" => Matron(),
        "larkspur" => LullabyMan(),
        "kestrel" => Presenter(),
        "ashgrove" => Curator(),
        _ => TidewifeFigure()
    };

    // ================================================================== Whitepine icons

    private const string Snow = "#EEF4F8";
    private const string SnowShade = "#D7E2EA";

    private static Canvas RangerStation() => Group(IconWidth, IconHeight,
        P("M0,64 Q8,56 42,56 Q76,56 84,64 Z", SnowShade),
        P("M12,62 L12,34 L64,34 L64,62 Z", "#6B4A32", "#3E2A1C"),
        P("M12,40 L64,40 M12,46 L64,46 M12,52 L64,52 M12,58 L64,58", "none", "#563A26", 1),
        P("M54,24 L54,14 L60,14 L60,28 Z", "#4A3A30"),
        P("M53,14 L61,14 L61,11 L53,11 Z", Snow),
        P("M6,36 L38,16 L70,36 Z", "#3A2A20", "#231B16"),
        P("M8,34 L38,15 L68,34 L62,33 L38,20 L14,33 Z", Snow),
        P("M24,26 L24,8 M20,11 L28,11 M21,15 L27,15", "none", "#9AA6AE", 1.2),
        Glow(23, 46, 12, C("#E8C26A"), 0.45),
        Glow(53, 46, 12, C("#E8C26A"), 0.45),
        P("M18,42 L28,42 L28,50 L18,50 Z M48,42 L58,42 L58,50 L48,50 Z", "#E8C26A", "#8A6F35"),
        P("M33,62 L33,46 L43,46 L43,62 Z", "#2B1F18"),
        P("M76,62 L76,20", "none", "#8A8A8A", 1.5),
        P("M76,20 L86,23 L76,26 Z", "#3E7B5A"));

    private static Canvas Trailhead() => Group(IconWidth, IconHeight,
        P("M0,64 Q20,52 42,54 Q66,52 84,64 Z", SnowShade),
        P("M24,62 L24,30 L28,30 L28,62 Z M56,62 L56,30 L60,30 L60,62 Z", "#5A4030"),
        P("M18,22 L66,22 L66,44 L18,44 Z", "#7A5A3A", "#4A3424"),
        P("M22,26 L62,26 L62,40 L22,40 Z", "#D8CFB8"),
        P("M26,36 Q34,28 42,34 Q50,40 58,30", "none", "#8A3A2E", 1.2),
        P("M40,33 a1.5,1.5 0 1 1 3,0 a1.5,1.5 0 1 1 -3,0 Z", "#8A3A2E"),
        P("M16,22 Q42,15 68,22 L66,24 L18,24 Z", Snow),
        P("M74,62 L74,44 L77,44 L77,62 Z", "#5A4030"),
        P("M73,46 L78,46 L78,50 L73,50 Z", "#E8743A"),
        P("M4,62 L4,48 L6,48 L6,62 Z", "#5A4030"),
        P("M1,45 L13,45 L16,48.5 L13,52 L1,52 Z", "#7A5A3A", "#4A3424", 0.8));

    private static Canvas SkiLodge() => Group(IconWidth, IconHeight,
        P("M0,64 Q42,54 84,64 Z", SnowShade),
        P("M10,62 L42,6 L74,62 Z", "#6B4A32", "#3E2A1C"),
        P("M8,62 L42,4 L45,9 L13,62 Z M76,62 L42,4 L39,9 L71,62 Z", Snow),
        Glow(42, 38, 20, C("#E8C26A"), 0.5),
        P("M30,46 L42,24 L54,46 Z", "#E8C26A", "#8A6F35"),
        P("M42,24 L42,46 M33,40 L51,40", "none", "#6B4A32", 1.2),
        P("M22,49 L62,49", "none", "#3E2A1C", 1.6),
        P("M37,62 L37,52 L47,52 L47,62 Z", "#2B1F18"),
        P("M70,62 L76,40 M73,62 L79,41", "none", "#B8463A", 1.6));

    private static Canvas Cabins() => Group(IconWidth, IconHeight,
        P("M0,64 Q42,56 84,64 Z", SnowShade),
        P("M2,62 L14,40 L26,62 Z", "#6B4A32", "#3E2A1C"),
        P("M1,62 L14,38 L16,42 L5,62 Z", Snow),
        P("M11,52 L17,52 L17,57 L11,57 Z", "#E8C26A"),
        P("M29,62 L43,32 L57,62 Z", "#5E4230", "#3E2A1C"),
        P("M28,62 L43,30 L45,34 L32,62 Z", Snow),
        P("M39,62 L39,51 L47,51 L47,62 Z", "#0E0E0E"),
        P("M47,62 L47,51 L50.5,52.5 L50.5,62 Z", "#3E2A1C"),
        P("M60,62 L72,42 L84,62 Z", "#6B4A32", "#3E2A1C"),
        P("M59,62 L72,40 L74,44 L63,62 Z", Snow),
        P("M69,53 L75,53 L75,58 L69,58 Z", "#141414"));

    private static Canvas Sawmill() => Group(IconWidth, IconHeight,
        P("M0,64 Q42,56 84,64 Z", SnowShade),
        P("M6,62 L6,32 L60,32 L60,62 Z", "#6E5238", "#3E2A1C"),
        P("M14,32 L14,62 M22,32 L22,62 M30,32 L30,62 M38,32 L38,62 M46,32 L46,62 M54,32 L54,62", "none", "#5A422C", 1),
        P("M0,34 L33,18 L66,34 Z", "#3A2E24", "#231B16"),
        P("M2,33 L33,17 L64,33 L58,32 L33,21 L8,32 Z", Snow),
        P("M18,62 L18,42 L48,42 L48,62 Z", "#1A1410"),
        P("M25,53 a8,8 0 1 1 16,0 a8,8 0 1 1 -16,0 Z", "#9AA3A8", "#5A6268"),
        P("M31.5,53 a1.5,1.5 0 1 1 3,0 a1.5,1.5 0 1 1 -3,0 Z", "#3A3A3A"),
        P("M64,62 a4,4 0 1 1 8,0 a4,4 0 1 1 -8,0 Z M72,62 a4,4 0 1 1 8,0 a4,4 0 1 1 -8,0 Z M68,55 a4,4 0 1 1 8,0 a4,4 0 1 1 -8,0 Z", "#8A6A44", "#5A422C"),
        P("M67,51.5 Q72,48 77,51.5 Z", Snow));

    private static Canvas FireTower() => Group(IconWidth, IconHeight,
        P("M0,64 Q42,56 84,64 Z", SnowShade),
        P("M26,62 L36,6 M58,62 L48,6", "none", "#5A4A3A", 2.2),
        P("M28,52 L56,40 M56,52 L28,40 M31,34 L53,22 M53,34 L31,22", "none", "#5A4A3A", 1.2),
        P("M40,62 L46,52 L38,42 L46,32 L38,22 L44,10", "none", "#7A6A5A", 1),
        P("M30,6 L54,6 L54,-10 L30,-10 Z", "#6B4A32", "#3E2A1C"),
        Glow(42, -2, 16, C("#E8C26A"), 0.55),
        P("M34,-6 L50,-6 L50,2 L34,2 Z", "#E8C26A", "#8A6F35"),
        P("M27,-10 L42,-20 L57,-10 Z", "#3A2A20", "#231B16"),
        P("M28,-11 L42,-21 L56,-11 L52,-11 L42,-17 L32,-11 Z", Snow),
        P("M54,-6 L59,-6", "none", "#3A2A20", 1),
        P("M56,2 Q56,-5 59,-5 Q62,-5 62,2 Z", "#B89A4A", "#7A6430", 0.6));

    private static Canvas FrozenLake() => Group(IconWidth, IconHeight,
        P("M2,52 Q4,36 42,34 Q80,36 82,52 Q80,64 42,64 Q4,64 2,52 Z", "#A9C6D6", "#DDEFF7", 1.5),
        P("M14,50 L24,46 L30,52 L40,47 M48,58 L56,50 L66,54 M30,40 L36,44", "none", "#E8F6FC", 1),
        P("M44,48 a7,4 0 1 1 14,0 a7,4 0 1 1 -14,0 Z", "#0E2A36", "#DDEFF7"),
        P("M8,48 L8,38 L20,38 L20,48 Z", "#8A3A2E", "#5A251D"),
        P("M6,38 L14,32 L22,38 Z", "#3A2A20"),
        P("M6,38 L14,31 L22,38 L20,38 L14,34 L8,38 Z", Snow),
        P("M74,42 l-2,-8 M76,42 l1,-9 M78,43 l3,-7", "none", "#5A5040", 1.2));

    private static Canvas OldMine() => Group(IconWidth, IconHeight,
        P("M0,64 Q10,20 42,14 Q74,20 84,64 Z", "#5A606A", "#3A3F48"),
        P("M14,30 Q42,6 70,30 Q60,22 42,22 Q24,22 14,30 Z", Snow),
        P("M28,62 L28,34 L56,34 L56,62 Z", "#0B0B0B"),
        P("M26,62 L26,32 L30,32 L30,62 Z M54,62 L54,32 L58,32 L58,62 Z M24,30 L60,30 L60,35 L24,35 Z", "#6B4A32", "#3E2A1C"),
        P("M34,62 L38,48 M50,62 L46,48 M33,58 L51,58 M35,54 L49,54", "none", "#6A6A6A", 1.2),
        P("M60,60 L62,52 L78,52 L80,60 Z", "#5A5A5A", "#3A3A3A"),
        P("M63,62 a2,2 0 1 1 4,0 a2,2 0 1 1 -4,0 Z M73,62 a2,2 0 1 1 4,0 a2,2 0 1 1 -4,0 Z", "#2A2A2A"));

    // ================================================================== St Agnes icons

    private static Canvas Gatehouse() => Group(IconWidth, IconHeight,
        P("M10,62 L10,20 L22,20 L22,62 Z M62,62 L62,20 L74,20 L74,62 Z", "#6E6A62", "#45423C"),
        P("M8,20 L24,20 L24,16 L8,16 Z M60,20 L76,20 L76,16 L60,16 Z", "#55524C"),
        P("M12,16 a4,4 0 1 1 8,0 Z M64,16 a4,4 0 1 1 8,0 Z", "#55524C"),
        P("M26,62 L26,31 M31,62 L31,28.5 M36,62 L36,27 M41,62 L41,26 M46,62 L46,27 M51,62 L51,28.5 M56,62 L56,31", "none", "#1E1E1E", 1.6),
        P("M24,32 Q42,18 60,32", "none", "#1E1E1E", 1.6),
        P("M24,44 L60,44", "none", "#1E1E1E", 1.4),
        Glow(16, 9, 13, C("#FFE08A"), 0.6),
        Glow(68, 9, 13, C("#FFE08A"), 0.6),
        P("M14,11 L18,11 L18,6 L14,6 Z M66,11 L70,11 L70,6 L66,6 Z", "#FFE08A", "#8A6F35", 0.6));

    private static Canvas Courtyard() => Group(IconWidth, IconHeight,
        P("M2,58 Q4,46 42,46 Q80,46 82,58 Q80,66 42,66 Q4,66 2,58 Z", "#5A5850"),
        P("M18,56 Q18,48 42,48 Q66,48 66,56 Q66,60 42,60 Q18,60 18,56 Z", "#8A867C", "#55524C"),
        P("M22,55 Q24,51 42,51 Q60,51 62,55 Q60,57 42,57 Q24,57 22,55 Z", "#3A3832"),
        P("M39,53 L39,30 L45,30 L45,53 Z", "#8A867C", "#55524C"),
        P("M30,30 Q42,36 54,30 L52,27 L32,27 Z", "#8A867C", "#55524C"),
        P("M40,27 L42,18 L44,27 Z", "#8A867C"),
        P("M2,44 L14,44 L14,46 L2,46 Z M70,44 L82,44 L82,46 L70,46 Z", "#5A4030"),
        P("M3,46 L3,49 M13,46 L13,49 M71,46 L71,49 M81,46 L81,49", "none", "#3A2A20", 1));

    private static Canvas GothicChapel() => Group(IconWidth, IconHeight,
        P("M2,62 L2,56 Q5,53 8,56 L8,62 Z M76,62 L76,55 Q79,52 82,55 L82,62 Z", "#5A5852"),
        P("M14,62 L14,30 L58,30 L58,62 Z", "#6E6A62", "#45423C"),
        P("M10,32 L36,8 L62,32 Z", "#2E2C30", "#1E1C20"),
        Glow(36, 22, 15, C("#E89A8A"), 0.45),
        P("M30,22 a6,6 0 1 1 12,0 a6,6 0 1 1 -12,0 Z", "#B8746A", "#3A2A2A", 0.8),
        P("M30,22 L42,22 M36,16 L36,28 M32,18 L40,26 M40,18 L32,26", "none", "#3A2A2A", 0.7),
        P("M20,52 L20,42 Q23,36 26,42 L26,52 Z M46,52 L46,42 Q49,36 52,42 L52,52 Z", "#E8C26A", "#8A6F35"),
        P("M32,62 L32,50 Q36,44 40,50 L40,62 Z", "#2B1F18"),
        P("M60,62 L60,14 L70,14 L70,62 Z", "#6E6A62", "#45423C"),
        P("M58,15 L65,-6 L72,15 Z", "#2E2C30", "#1E1C20"),
        P("M64,-12 L66,-12 L66,-5 L64,-5 Z M62,-10 L68,-10 L68,-8 L62,-8 Z", "#CFC8B8"));

    private static Canvas Ward(bool derelict)
    {
        string body = derelict ? "#6A6458" : "#7A6E60";
        var c = Group(IconWidth, IconHeight,
            P("M14,18 L14,10 L20,10 L20,18 Z M64,18 L64,10 L70,10 L70,18 Z", "#4A4238"),
            P("M0,62 L0,28 L84,28 L84,62 Z", body, "#4A4238"),
            P("M-2,30 L8,18 L76,18 L86,30 Z", "#3A3634", "#24221F"));
        // Two rows of six windows; the door takes the middle of the lower row.
        var lit = new System.Text.StringBuilder();
        var dark = new System.Text.StringBuilder();
        var boards = new System.Text.StringBuilder();
        int[] xs = [6, 19, 32, 45, 58, 71];
        for (int row = 0; row < 2; row++)
        {
            int y = row == 0 ? 34 : 48;
            for (int i = 0; i < xs.Length; i++)
            {
                int x = xs[i];
                if (row == 1 && (i == 2 || i == 3)) continue;
                bool isLit = !derelict && ((row == 0 && (i == 1 || i == 4)) || (row == 1 && i == 5));
                (isLit ? lit : dark).Append($"M{x},{y} h7 v9 h-7 Z ");
                if (derelict && (i + row) % 3 == 0) boards.Append($"M{x},{y} L{x + 7},{y + 9} M{x + 7},{y} L{x},{y + 9} ");
            }
        }
        if (dark.Length > 0) c.Children.Add(P(dark.ToString(), "#1A1A1A"));
        if (lit.Length > 0)
        {
            c.Children.Add(Glow(22.5, 38.5, 10, C("#E8C26A"), 0.35));
            c.Children.Add(Glow(61.5, 38.5, 10, C("#E8C26A"), 0.35));
            c.Children.Add(P(lit.ToString(), "#E8C26A", "#8A6F35", 0.6));
        }
        if (boards.Length > 0) c.Children.Add(P(boards.ToString(), "none", "#6B4A32", 1.4));
        if (derelict) c.Children.Add(P("M30,18 L44,18 L40,24 L34,22 Z", "#0E0E0E"));
        c.Children.Add(P("M38,62 L38,51 L46,51 L46,62 Z", "#2B1F18"));
        return c;
    }

    private static Canvas RecordsOffice() => Group(IconWidth, IconHeight,
        P("M8,62 L8,32 L76,32 L76,62 Z", "#7A7064", "#4A4238"),
        P("M4,34 L42,22 L80,34 Z", "#3A3634", "#24221F"),
        P("M34,32 L34,4 L50,4 L50,32 Z", "#857A6C", "#4A4238"),
        P("M32,4 L42,-8 L52,4 Z", "#3A3634", "#24221F"),
        P("M36,14 a6,6 0 1 1 12,0 a6,6 0 1 1 -12,0 Z", "#E8E0C8", "#4A4238"),
        P("M42,14 L42,10 M42,14 L45,15", "none", "#2A2A2A", 1),
        P("M14,40 h6 v8 h-6 Z M54,40 h6 v8 h-6 Z M64,40 h6 v8 h-6 Z", "#1A1A1A"),
        Glow(27, 44, 10, C("#E8C26A"), 0.4),
        P("M24,40 h6 v8 h-6 Z", "#E8C26A", "#8A6F35", 0.6),
        P("M38,62 L38,48 Q42,44 46,48 L46,62 Z", "#2B1F18"),
        P("M60,61 l6,-1 l1,2 l-6,1 Z M18,62 l5,-2 l2,2 l-5,1 Z", "#D8D2C0"));

    private static Canvas BoilerHouse() => Group(IconWidth, IconHeight,
        P("M6,62 L6,30 L56,30 L56,62 Z", "#7A4A3A", "#4E2F25"),
        P("M6,38 L56,38 M6,46 L56,46 M6,54 L56,54", "none", "#5E3A2E", 0.8),
        P("M4,30 L58,30 L58,26 L4,26 Z", "#3A2E28"),
        P("M62,62 L62,0 L72,0 L72,62 Z", "#6E4234", "#4E2F25"),
        P("M62,10 L72,10 M62,30 L72,30", "none", "#4E2F25", 1.2),
        P("M56,40 L62,40 M56,48 L62,48", "none", "#8A8A8A", 2.4),
        Glow(28, 54, 18, C("#FF8A3A"), 0.55),
        P("M20,62 L20,46 L36,46 L36,62 Z", "#E8743A", "#7A3A1E"),
        P("M42,36 h8 v6 h-8 Z", "#1A1A1A"));

    private static Canvas Morgue() => Group(IconWidth, IconHeight,
        P("M4,62 L4,36 L80,36 L80,62 Z", "#8A8A84", "#55554F"),
        P("M2,36 L82,36 L82,32 L2,32 Z", "#4A4A46"),
        P("M10,42 h12 v8 h-12 Z M62,42 h12 v8 h-12 Z", "#B8C8C4", "#6E7A78", 0.6),
        Glow(42, 34, 13, C("#CFE8FF"), 0.5),
        P("M32,62 L32,42 L52,42 L52,62 Z", "#3A4040", "#22282A"),
        P("M42,42 L42,62", "none", "#22282A", 1),
        P("M36,47 a2,2 0 1 1 4,0 a2,2 0 1 1 -4,0 Z M44,47 a2,2 0 1 1 4,0 a2,2 0 1 1 -4,0 Z", "#B8C8C4"),
        P("M30,38 L54,38 L54,41 L30,41 Z", "#2A2A2A"),
        P("M60,62 h10 v-2 h-10 Z", "#3A3A3A"));

    // ================================================================== figures

    /// <summary>Dana: ranger's parka, fur-trimmed hood, torch with a cold beam. 32 x 48, feet at (16, 47).</summary>
    private static Figure Dana()
    {
        var legL = new RotateTransform(0, 13, 36);
        var legR = new RotateTransform(0, 19, 36);
        var leftLeg = P("M11,36 L15,36 L15,47 L11,47 Z", "#2A2A2A");
        leftLeg.RenderTransform = legL;
        var rightLeg = P("M17,36 L21,36 L21,47 L17,47 Z", "#343434");
        rightLeg.RenderTransform = legR;
        var body = Group(32, 48,
            P("M5,47 A11,2.5 0 1 1 27,47 A11,2.5 0 1 1 5,47 Z", "#000000").Also(e => e.Opacity = 0.4),
            Glow(29, 30, 15, C("#CFE8FF"), 0.55),
            rightLeg, leftLeg,
            P("M8,17 L24,17 L26.5,38 L5.5,38 Z", "#3E5B3A", "#23361F"),
            P("M16,18 L16,37", "none", "#23361F", 0.8),
            P("M10.5,24 h3 v3 h-3 Z", "#E8C26A"),
            P("M23,21 L28,28", "none", "#2F4A2C", 2.6),
            P("M26.5,26.5 L31.5,26.5 L31.5,30.5 L26.5,30.5 Z", "#2A2A2A"),
            P("M10,11.5 A6,6.5 0 1 1 22,11.5 A6,6.5 0 1 1 10,11.5 Z", "#8A6E52"),
            P("M12.5,12 A3.5,4 0 1 1 19.5,12 A3.5,4 0 1 1 12.5,12 Z", "#B07A58", "#7A5038", 0.5),
            P("M14.3,12.5 L15.1,12.5 M17,12.5 L17.8,12.5", "none", "#1E140E", 1.1));
        return Wrap(body, 32, 48, 16, 47, legL, legR);
    }

    /// <summary>Eli: dark hoodie, backpack, camera and torch. 32 x 48, feet at (16, 47).</summary>
    private static Figure Eli()
    {
        var legL = new RotateTransform(0, 13, 36);
        var legR = new RotateTransform(0, 19, 36);
        var leftLeg = P("M11,36 L15,36 L15,47 L11,47 Z", "#2E3C58");
        leftLeg.RenderTransform = legL;
        var rightLeg = P("M17,36 L21,36 L21,47 L17,47 Z", "#36466A");
        rightLeg.RenderTransform = legR;
        var body = Group(32, 48,
            P("M5,47 A11,2.5 0 1 1 27,47 A11,2.5 0 1 1 5,47 Z", "#000000").Also(e => e.Opacity = 0.45),
            Glow(28, 29, 15, C("#FFE08A"), 0.5),
            rightLeg, leftLeg,
            P("M3.5,20 L9,20 L9,34 L3.5,34 Z", "#6B4A2E", "#3E2A1C"),
            P("M8,18 L24,18 L26,38 L6,38 Z", "#3A3F4A", "#23262E"),
            P("M10,19 L12,30 M22,19 L20,30", "none", "#23262E", 1),
            P("M23,22 L27,27", "none", "#3A3F4A", 2.6),
            P("M24,25 L30,25 L30,30.5 L24,30.5 Z", "#1E1E1E"),
            P("M25.6,27.8 a1.4,1.4 0 1 1 2.8,0 a1.4,1.4 0 1 1 -2.8,0 Z", "#4A6A8A"),
            P("M9.5,11.5 A6.5,7 0 1 1 22.5,11.5 A6.5,7 0 1 1 9.5,11.5 Z", "#3A3F4A", "#23262E", 0.6),
            P("M12.5,12.5 A3.5,4 0 1 1 19.5,12.5 A3.5,4 0 1 1 12.5,12.5 Z", "#E2C3A0", "#9C7E63", 0.5),
            P("M14.3,13 L15.1,13 M17,13 L17.8,13", "none", "#2B1F18", 1.1));
        return Wrap(body, 32, 48, 16, 47, legL, legR);
    }

    /// <summary>The Hollow Stag: gaunt bark-and-bone body, a stag's skull, glowing frost eyes. 40 x 68, feet at (20, 67).</summary>
    private static Figure HollowStag()
    {
        var eyes = P("M16.5,10.5 a1.4,1.6 0 1 1 2.8,0 a1.4,1.6 0 1 1 -2.8,0 Z M20.7,10.5 a1.4,1.6 0 1 1 2.8,0 a1.4,1.6 0 1 1 -2.8,0 Z", "#DFF4FF");
        eyes.Effect = new DropShadowEffect { Color = C("#BFE6FF"), BlurRadius = 9, ShadowDepth = 0, Opacity = 1 };
        var body = Group(40, 68,
            P("M2,67 A18,3.5 0 1 1 38,67 A18,3.5 0 1 1 2,67 Z", "#BFE6FF").Also(e => e.Opacity = 0.35),
            Glow(20, 34, 28, C("#BFE6FF"), 0.16),
            P("M15,40 L13,66 L16,66 L18,42 Z M25,40 L27,66 L24,66 L22,42 Z", "#1E1A16"),
            P("M12,20 L28,20 L26,44 L14,44 Z", "#2A231D", "#120E0B", 0.8),
            P("M16,22 L15,42 M20,21 L20,43 M24,22 L25,42", "none", "#3A3028", 0.7),
            P("M12,22 Q6,34 7,52 M28,22 Q34,34 33,52", "none", "#2A231D", 2.4),
            P("M7,52 l-2,4 M7,52 l0,4 M7,52 l2,4 M33,52 l-2,4 M33,52 l0,4 M33,52 l2,4", "none", "#1E1A16", 1),
            P("M15,7 L10,-4 L6,-6 M10,-4 L9,-12 M10,-4 L4,-1 M25,7 L30,-4 L34,-6 M30,-4 L31,-12 M30,-4 L36,-1", "none", "#4A3E32", 1.6),
            P("M14,8 Q20,4 26,8 L24,18 Q20,21 16,18 Z", "#D8D2C4", "#8E887A", 0.6),
            P("M18,16 L18,18 M22,16 L22,18", "none", "#8E887A", 0.6),
            eyes);
        return Wrap(body, 40, 68, 20, 67);
    }

    /// <summary>The Matron: dark dress and apron, starched veil, ring of keys, face in shadow. 36 x 62, feet at (18, 61).</summary>
    private static Figure Matron()
    {
        var eyes = P("M15.2,10.6 L17.2,10.6 M18.8,10.6 L20.8,10.6", "none", "#E86A55", 1);
        eyes.Effect = new DropShadowEffect { Color = C("#C8553D"), BlurRadius = 7, ShadowDepth = 0, Opacity = 1 };
        var body = Group(36, 62,
            P("M3,61 A15,3 0 1 1 33,61 A15,3 0 1 1 3,61 Z", "#000000").Also(e => e.Opacity = 0.45),
            Glow(18, 30, 26, C("#C8553D"), 0.14),
            P("M10,8 Q18,0 26,8 L29,30 L7,30 Z", "#E6E2D8", "#B8B4AA", 0.5).Also(e => e.Opacity = 0.92),
            P("M10,20 L26,20 L31,60 L5,60 Z", "#262A36", "#14161E", 0.8),
            P("M13,26 L23,26 L25,54 L11,54 Z", "#D8D4C8").Also(e => e.Opacity = 0.85),
            P("M10,22 Q6,32 9,42 M26,22 Q30,32 27,42", "none", "#262A36", 2.6),
            P("M8,42 a1.6,1.6 0 1 1 3.2,0 a1.6,1.6 0 1 1 -3.2,0 Z M25.8,42 a1.6,1.6 0 1 1 3.2,0 a1.6,1.6 0 1 1 -3.2,0 Z", "#CFC8BC"),
            P("M25,46 a2.5,2.5 0 1 1 5,0 a2.5,2.5 0 1 1 -5,0 Z", "none", "#B89A4A", 1),
            P("M27,48.5 l-1,4 M28.6,48.5 l1,4", "none", "#B89A4A", 0.9),
            P("M13,10 A5,6 0 1 1 23,10 A5,6 0 1 1 13,10 Z", "#2A2A30", "#14141A", 0.5),
            P("M11,6 L25,6 L24,2 L12,2 Z", "#F0ECE2"),
            P("M17.2,2.6 h1.6 v3 h-1.6 Z M16.5,3.3 h3 v1.5 h-3 Z", "#A83A32"),
            eyes);
        return Wrap(body, 36, 62, 18, 61);
    }

    // ================================================================== weather

    /// <summary>Snow drifting down across the board.</summary>
    public static void AddSnow(Canvas layer, double width, double height)
    {
        var rng = new Random(23);
        var flake = new SolidColorBrush(Color.FromArgb(220, 240, 246, 250));
        flake.Freeze();
        for (int i = 0; i < 90; i++)
        {
            double size = 1.5 + rng.NextDouble() * 2.5;
            var e = new Ellipse { Width = size, Height = size, Fill = flake, IsHitTestVisible = false, Opacity = 0.5 + rng.NextDouble() * 0.5 };
            var move = new TranslateTransform();
            e.RenderTransform = move;
            Canvas.SetLeft(e, rng.NextDouble() * width);
            Canvas.SetTop(e, -10);
            double seconds = 7 + rng.NextDouble() * 8;
            var begin = TimeSpan.FromSeconds(rng.NextDouble() * seconds);
            move.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(0, height + 20, new Duration(TimeSpan.FromSeconds(seconds)))
            { RepeatBehavior = RepeatBehavior.Forever, BeginTime = begin });
            move.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(-18, 18, new Duration(TimeSpan.FromSeconds(2 + rng.NextDouble() * 2)))
            { RepeatBehavior = RepeatBehavior.Forever, AutoReverse = true, EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut } });
            layer.Children.Add(e);
        }
    }

    /// <summary>Rain streaks, and an occasional flash of lightning.</summary>
    public static void AddRain(Canvas layer, double width, double height)
    {
        var rng = new Random(31);
        var streak = new SolidColorBrush(Color.FromArgb(110, 190, 210, 220));
        streak.Freeze();
        for (int i = 0; i < 110; i++)
        {
            var l = new Line { X1 = 0, Y1 = 0, X2 = -4, Y2 = 14, Stroke = streak, StrokeThickness = 1, IsHitTestVisible = false };
            var move = new TranslateTransform();
            l.RenderTransform = move;
            Canvas.SetLeft(l, rng.NextDouble() * (width + 120));
            Canvas.SetTop(l, -20);
            double seconds = 0.7 + rng.NextDouble() * 0.5;
            var begin = TimeSpan.FromSeconds(rng.NextDouble() * seconds);
            move.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(0, height + 40, new Duration(TimeSpan.FromSeconds(seconds)))
            { RepeatBehavior = RepeatBehavior.Forever, BeginTime = begin });
            move.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(0, -(height + 40) * 0.28, new Duration(TimeSpan.FromSeconds(seconds)))
            { RepeatBehavior = RepeatBehavior.Forever, BeginTime = begin });
            layer.Children.Add(l);
        }

        // Rain rings on the puddles.
        foreach (var (x, y) in Scenery.Puddles)
        {
            for (int k = 0; k < 2; k++)
            {
                var ring = new Ellipse { Width = 8, Height = 3, Stroke = streak, StrokeThickness = 0.8, IsHitTestVisible = false, RenderTransformOrigin = new Point(0.5, 0.5) };
                var grow = new ScaleTransform(0.2, 0.2);
                ring.RenderTransform = grow;
                Canvas.SetLeft(ring, x - 4 + rng.Next(-6, 7));
                Canvas.SetTop(ring, y - 1.5 + rng.Next(-2, 3));
                var dur = new Duration(TimeSpan.FromSeconds(0.9 + rng.NextDouble()));
                var begin = TimeSpan.FromSeconds(rng.NextDouble());
                grow.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(0.2, 1.8, dur) { RepeatBehavior = RepeatBehavior.Forever, BeginTime = begin });
                grow.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(0.2, 1.8, dur) { RepeatBehavior = RepeatBehavior.Forever, BeginTime = begin });
                ring.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0.9, 0, dur) { RepeatBehavior = RepeatBehavior.Forever, BeginTime = begin });
                layer.Children.Add(ring);
            }
        }

        // Lightning: two quick flashes every 14 seconds.
        var flash = new Rectangle { Width = width, Height = height, Fill = new SolidColorBrush(Color.FromRgb(230, 236, 255)), IsHitTestVisible = false, Opacity = 0 };
        var frames = new DoubleAnimationUsingKeyFrames { Duration = new Duration(TimeSpan.FromSeconds(14)), RepeatBehavior = RepeatBehavior.Forever };
        frames.KeyFrames.Add(new DiscreteDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.Zero)));
        frames.KeyFrames.Add(new DiscreteDoubleKeyFrame(0.45, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(9.00))));
        frames.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(9.12))));
        frames.KeyFrames.Add(new DiscreteDoubleKeyFrame(0.3, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(9.25))));
        frames.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(9.6))));
        flash.BeginAnimation(UIElement.OpacityProperty, frames);
        layer.Children.Add(flash);
    }

    /// <summary>Faint northern lights rippling across the top of the Whitepine sky.</summary>
    public static void AddAurora(Canvas layer, double width)
    {
        var brush = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(0, 1) };
        brush.GradientStops.Add(new GradientStop(Color.FromArgb(0, 120, 255, 200), 0));
        brush.GradientStops.Add(new GradientStop(Color.FromArgb(70, 120, 255, 200), 0.45));
        brush.GradientStops.Add(new GradientStop(Color.FromArgb(0, 120, 255, 200), 1));
        brush.Freeze();
        var band = P($"M0,30 Q{width * 0.25},0 {width * 0.5},28 T{width},20 L{width},70 Q{width * 0.75},40 {width * 0.5},74 T0,66 Z", "#000000");
        band.Fill = brush;
        band.IsHitTestVisible = false;
        band.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0.25, 0.9, new Duration(TimeSpan.FromSeconds(5)))
        { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever, EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut } });
        var drift = new TranslateTransform();
        band.RenderTransform = drift;
        drift.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(-30, 30, new Duration(TimeSpan.FromSeconds(11)))
        { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever, EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut } });
        layer.Children.Add(band);
    }

    // ================================================================== new weather

    /// <summary>Larkspur Lane: dream-dust motes drifting slowly upward, glowing and fading.</summary>
    public static void AddMotes(Canvas layer, double width, double height)
    {
        var rng = new Random(41);
        Color[] tints = [C("#E8D8FF"), C("#C8A8FF"), C("#FFE8F4"), C("#BFE6FF")];
        for (int i = 0; i < 70; i++)
        {
            double size = 1.5 + rng.NextDouble() * 3;
            var tint = tints[rng.Next(tints.Length)];
            var brush = new RadialGradientBrush(Color.FromArgb(230, tint.R, tint.G, tint.B), Color.FromArgb(0, tint.R, tint.G, tint.B));
            brush.Freeze();
            var e = new Ellipse { Width = size * 2.4, Height = size * 2.4, Fill = brush, IsHitTestVisible = false };
            var move = new TranslateTransform();
            e.RenderTransform = move;
            Canvas.SetLeft(e, rng.NextDouble() * width);
            Canvas.SetTop(e, height * (0.3 + rng.NextDouble() * 0.75));
            double seconds = 10 + rng.NextDouble() * 12;
            var begin = TimeSpan.FromSeconds(-rng.NextDouble() * seconds);
            move.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(0, -height * 0.6, new Duration(TimeSpan.FromSeconds(seconds)))
            { RepeatBehavior = RepeatBehavior.Forever, BeginTime = begin });
            move.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(-14, 14, new Duration(TimeSpan.FromSeconds(3 + rng.NextDouble() * 3)))
            { RepeatBehavior = RepeatBehavior.Forever, AutoReverse = true, EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut } });
            e.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 0.9, new Duration(TimeSpan.FromSeconds(seconds / 2)))
            { RepeatBehavior = RepeatBehavior.Forever, AutoReverse = true, BeginTime = begin });
            layer.Children.Add(e);
        }
        // A slow lilac haze that breathes in and out, like falling asleep.
        var haze = new Rectangle { Width = width, Height = height, IsHitTestVisible = false, Fill = Hex("#2A1A4A") };
        haze.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 0.22, new Duration(TimeSpan.FromSeconds(6)))
        { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever, EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut } });
        layer.Children.Add(haze);
    }

    /// <summary>Kestrel Motel: faint scanlines, a rolling interference band, twinkling static, and the odd picture jump.</summary>
    public static void AddStatic(Canvas layer, double width, double height)
    {
        var rng = new Random(53);
        var lines = new System.Text.StringBuilder();
        for (double y = 0; y < height; y += 3) lines.Append($"M0,{y:F0} H{width:F0} ");
        var scan = P(lines.ToString(), "none", "#000000", 1);
        scan.Opacity = 0.12;
        scan.IsHitTestVisible = false;
        layer.Children.Add(scan);

        var bandBrush = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(0, 1) };
        bandBrush.GradientStops.Add(new GradientStop(Color.FromArgb(0, 200, 240, 255), 0));
        bandBrush.GradientStops.Add(new GradientStop(Color.FromArgb(40, 200, 240, 255), 0.5));
        bandBrush.GradientStops.Add(new GradientStop(Color.FromArgb(0, 200, 240, 255), 1));
        bandBrush.Freeze();
        var band = new Rectangle { Width = width, Height = 70, Fill = bandBrush, IsHitTestVisible = false };
        var roll = new TranslateTransform();
        band.RenderTransform = roll;
        roll.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(-80, height + 10, new Duration(TimeSpan.FromSeconds(7))) { RepeatBehavior = RepeatBehavior.Forever });
        layer.Children.Add(band);

        for (int i = 0; i < 80; i++)
        {
            var speck = new Rectangle { Width = 1.5 + rng.NextDouble() * 2, Height = 1.2, Fill = Hex(rng.Next(2) == 0 ? "#E8FFFF" : "#8AB8C4"), IsHitTestVisible = false, Opacity = 0 };
            Canvas.SetLeft(speck, rng.NextDouble() * width);
            Canvas.SetTop(speck, rng.NextDouble() * height);
            var frames = new DoubleAnimationUsingKeyFrames { Duration = new Duration(TimeSpan.FromSeconds(0.6 + rng.NextDouble() * 1.4)), RepeatBehavior = RepeatBehavior.Forever, BeginTime = TimeSpan.FromSeconds(rng.NextDouble() * 2) };
            frames.KeyFrames.Add(new DiscreteDoubleKeyFrame(0, KeyTime.FromPercent(0)));
            frames.KeyFrames.Add(new DiscreteDoubleKeyFrame(0.8, KeyTime.FromPercent(0.1)));
            frames.KeyFrames.Add(new DiscreteDoubleKeyFrame(0, KeyTime.FromPercent(0.2)));
            speck.BeginAnimation(UIElement.OpacityProperty, frames);
            layer.Children.Add(speck);
        }

        // Every 11 seconds the picture flickers.
        var flicker = new Rectangle { Width = width, Height = height, Fill = Hex("#C8F0FF"), IsHitTestVisible = false, Opacity = 0 };
        var f = new DoubleAnimationUsingKeyFrames { Duration = new Duration(TimeSpan.FromSeconds(11)), RepeatBehavior = RepeatBehavior.Forever };
        f.KeyFrames.Add(new DiscreteDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.Zero)));
        f.KeyFrames.Add(new DiscreteDoubleKeyFrame(0.12, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(7.0))));
        f.KeyFrames.Add(new DiscreteDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(7.06))));
        f.KeyFrames.Add(new DiscreteDoubleKeyFrame(0.08, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(7.14))));
        f.KeyFrames.Add(new DiscreteDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(7.2))));
        flicker.BeginAnimation(UIElement.OpacityProperty, f);
        layer.Children.Add(flicker);
    }

    /// <summary>Ashgrove House: dust drifting in candlelight, and the whole house flickering as the candles gutter.</summary>
    public static void AddEmbers(Canvas layer, double width, double height)
    {
        var rng = new Random(61);
        var dust = new SolidColorBrush(Color.FromArgb(200, 255, 220, 160));
        dust.Freeze();
        for (int i = 0; i < 90; i++)
        {
            double size = 0.8 + rng.NextDouble() * 1.6;
            var e = new Ellipse { Width = size, Height = size, Fill = dust, IsHitTestVisible = false };
            var move = new TranslateTransform();
            e.RenderTransform = move;
            Canvas.SetLeft(e, rng.NextDouble() * width);
            Canvas.SetTop(e, rng.NextDouble() * height);
            double seconds = 8 + rng.NextDouble() * 10;
            move.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(-20, 20, new Duration(TimeSpan.FromSeconds(seconds)))
            { RepeatBehavior = RepeatBehavior.Forever, AutoReverse = true, EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut } });
            move.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(-12, 16, new Duration(TimeSpan.FromSeconds(seconds * 0.7)))
            { RepeatBehavior = RepeatBehavior.Forever, AutoReverse = true, EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut } });
            e.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0.1, 0.8, new Duration(TimeSpan.FromSeconds(1.5 + rng.NextDouble() * 3)))
            { RepeatBehavior = RepeatBehavior.Forever, AutoReverse = true });
            layer.Children.Add(e);
        }
        var warm = new Rectangle { Width = width, Height = height, Fill = Hex("#FF9A3A"), IsHitTestVisible = false, Opacity = 0.04 };
        var f = new DoubleAnimationUsingKeyFrames { Duration = new Duration(TimeSpan.FromSeconds(2.3)), RepeatBehavior = RepeatBehavior.Forever };
        foreach (var (t, v) in new[] { (0.0, 0.04), (0.3, 0.07), (0.5, 0.03), (0.9, 0.06), (1.4, 0.02), (1.8, 0.05), (2.3, 0.04) })
            f.KeyFrames.Add(new LinearDoubleKeyFrame(v, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(t))));
        warm.BeginAnimation(UIElement.OpacityProperty, f);
        layer.Children.Add(warm);
    }

    /// <summary>A padlock badge for a door on the map: gold and shut while locked, faint and open once unlocked. 24 x 24.</summary>
    public static Canvas Padlock(bool locked)
    {
        var c = Group(24, 24,
            P("M1,12 a11,11 0 1 1 22,0 a11,11 0 1 1 -22,0 Z", "#1A1210", locked ? "#E8C26A" : "#6A6A5A", 1.5),
            P("M7,11 h10 v7 h-10 Z", locked ? "#E8C26A" : "#8A8A7A"),
            P(locked ? "M8.8,11 v-2.6 a3.2,3.2 0 0 1 6.4,0 v2.6" : "M8.8,11 v-2.6 a3.2,3.2 0 0 1 6.4,0 v-1", "none", locked ? "#E8C26A" : "#8A8A7A", 1.4),
            P("M12,13.6 v2.2", "none", "#1A1210", 1.2));
        if (locked)
            c.Effect = new DropShadowEffect { Color = C("#E8C26A"), BlurRadius = 8, ShadowDepth = 0, Opacity = 0.6 };
        else
            c.Opacity = 0.55;
        return c;
    }

    /// <summary>A light that blinks on and off (the broadcast tower's warning light).</summary>
    public static void AddBlink(Canvas layer, double x, double y, Color colour, double seconds)
    {
        var g = Glow(x, y, 9, colour, 0.95);
        var frames = new DoubleAnimationUsingKeyFrames { Duration = new Duration(TimeSpan.FromSeconds(seconds)), RepeatBehavior = RepeatBehavior.Forever };
        frames.KeyFrames.Add(new DiscreteDoubleKeyFrame(1, KeyTime.FromPercent(0)));
        frames.KeyFrames.Add(new DiscreteDoubleKeyFrame(0, KeyTime.FromPercent(0.5)));
        g.BeginAnimation(UIElement.OpacityProperty, frames);
        layer.Children.Add(g);
    }
}
