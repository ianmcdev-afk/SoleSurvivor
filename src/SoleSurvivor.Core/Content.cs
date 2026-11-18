namespace SoleSurvivor.Core;

// ---------------------------------------------------------------------------
//  Game content shared by every scenario: item kinds, action cards, tide-card
//  mechanics. Anything themed (places, names, flavour) lives in Scenarios.cs;
//  Board / Items / TideCards below read from the scenario in play.
// ---------------------------------------------------------------------------

/// <summary>Item mechanics. Each scenario gives them its own names (a Boat Hook in Gallowmouth is a Felling Axe in Whitepine).</summary>
public enum ItemKind
{
    BoatHook,      // weapon: +1 fight die
    GuttingKnife,  // weapon: +1 fight die
    Lantern,       // +1 search die
    Rope,          // escort one extra
    FlareGun,      // use: 2 wounds, here or adjacent
    Bandages,      // use: heal 2
    BlessedSalt,   // use: 1 wound + she can't strike next round
    IronNails,     // use: barricade
    Key,           // opens one locked door (used up when you walk through it)
    Vehicle        // walk or escort several spaces in one move (scenarios with VehicleRange)
}

public sealed record ItemDef(ItemKind Kind, string Name, string Text, bool Consumable, int FightBonus = 0, int SearchBonus = 0);

public static class Items
{
    /// <summary>The current scenario's items.</summary>
    public static IReadOnlyDictionary<ItemKind, ItemDef> All => Scenarios.Current.Items;
}

/// <param name="IsLair">The killer can surface here (tide cards that send it to a lair).</param>
public sealed record LocationDef(
    int Id,
    string Name,
    string Flavour,
    double X,
    double Y,
    bool IsLair,
    bool IsSanctuary,
    int StartVillagers,
    ItemKind[] Stash);

/// <summary>The map of the scenario in play.</summary>
public static class Board
{
    /// <summary>Logical canvas size the X/Y coordinates are laid out on.</summary>
    public const double Width = 800, Height = 580;

    public static IReadOnlyList<LocationDef> Locations => Scenarios.Current.Locations;
    public static (int A, int B)[] Edges => Scenarios.Current.Edges;
    public static int Count => Locations.Count;
    public static int Sanctuary => Scenarios.Current.SanctuaryId;

    public static IReadOnlyList<int> Neighbours(int id) => Scenarios.Current.Adjacency[id];

    public static bool AreAdjacent(int a, int b) => Scenarios.Current.Adjacency[a].Contains(b);

    /// <summary>Shortest path from <paramref name="from"/> to <paramref name="to"/>, excluding <paramref name="from"/>.
    /// Returns null if unreachable. <paramref name="blocked"/> may not be entered; <paramref name="canPass"/>, if given,
    /// says whether an edge can be crossed (locked doors).</summary>
    public static List<int>? Path(int from, int to, int? blocked = null, Func<int, int, bool>? canPass = null)
    {
        if (from == to) return new List<int>();
        if (blocked == to) return null;
        var prev = new int[Count];
        Array.Fill(prev, -1);
        var seen = new bool[Count];
        var queue = new Queue<int>();
        queue.Enqueue(from);
        seen[from] = true;
        while (queue.Count > 0)
        {
            int cur = queue.Dequeue();
            foreach (int n in Neighbours(cur))
            {
                if (seen[n] || n == blocked || (canPass is not null && !canPass(cur, n))) continue;
                seen[n] = true;
                prev[n] = cur;
                if (n == to)
                {
                    var path = new List<int>();
                    for (int p = to; p != from; p = prev[p]) path.Add(p);
                    path.Reverse();
                    return path;
                }
                queue.Enqueue(n);
            }
        }
        return null;
    }

    public static int Distance(int from, int to, int? blocked = null, Func<int, int, bool>? canPass = null) =>
        Path(from, to, blocked, canPass)?.Count ?? int.MaxValue;
}

// ---------------------------------------------------------------------------
//  Survivor action cards (bought from the market with Time, played for free).
//  Text uses scenario tokens: {she} {her} {victim} {victims} {round}.
// ---------------------------------------------------------------------------

public enum CardEffect
{
    Sprint,
    Shepherd,
    WildSwing,
    Rummage,
    BarTheDoor,
    HoldBreath,
    LanternSignal,
    SteelNerves,
    PatchUp,
    Lure,
    DesperatePlan,

    // Free market cards (cost 0): weak on their own, but good to trade in for partial dice.
    Scramble,
    Peek,
    Holler,

    // Free starting cards (cost 0). You pick some at the start; each is used once.
    QuickStep,
    Guide,
    Shove,
    GlanceAround,
    CatchBreath
}

public sealed record ActionCardDef(CardEffect Effect, string Name, int Cost, string Text, int Copies);

public static class ActionCards
{
    internal static readonly ActionCardDef[] Templates =
    [
        new(CardEffect.Sprint,        "Sprint",          2, "Move up to 3 times this turn.", 3),
        new(CardEffect.Shepherd,      "Shepherd",        2, "Escort up to 2 times this turn.", 3),
        new(CardEffect.WildSwing,     "Wild Swing",      2, "Fight now: Horror dice +1, plus weapons.", 3),
        new(CardEffect.Rummage,       "Rummage",         1, "Search now: Horror dice +1, plus light.", 2),
        new(CardEffect.BarTheDoor,    "Bar the Door",    2, "{She} cannot enter your location next {round}.", 2),
        new(CardEffect.HoldBreath,    "Hold Your Breath",1, "Take 1 less damage if {she} attacks you next {round}.", 2),
        new(CardEffect.LanternSignal, "Signal",          2, "{Victims} in adjacent locations come to you.", 2),
        new(CardEffect.SteelNerves,   "Steel Nerves",    1, "Gain 1 Hope.", 2),
        new(CardEffect.PatchUp,       "Patch Up",        2, "Heal 2, even with {her} beside you.", 2),
        new(CardEffect.Lure,          "Lure",            1, "{She} moves 1 step toward you.", 1),
        new(CardEffect.DesperatePlan, "Desperate Plan",  3, "Fight now: Horror dice +2, plus weapons.", 2),
    ];

    internal static readonly ActionCardDef[] StarterTemplates =
    [
        new(CardEffect.QuickStep,    "Walk",              0, "Move 1 space.", 2),
        new(CardEffect.Guide,        "Lead",              0, "Escort once: move 1 space with {victims} from here.", 1),
        new(CardEffect.GlanceAround, "Search",            0, "Search now: Horror dice, plus light.", 1),
        new(CardEffect.Shove,        "Weak Attack",       0, "Fight now: Horror dice -1, plus weapons.", 1),
        new(CardEffect.CatchBreath,  "Rest",              0, "Heal 1, if {she} isn't beside you.", 1),
    ];

    /// <summary>The market deck, worded for the current scenario.</summary>
    public static IReadOnlyList<ActionCardDef> All => Scenarios.Current.ActionCards;

    /// <summary>The zero-cost basics you start with: Walk x2, Lead, Search, Weak Attack, Rest.</summary>
    public static IReadOnlyList<ActionCardDef> Starters => Scenarios.Current.StarterCards;

    public static bool IsStarter(CardEffect e) => e >= CardEffect.QuickStep;
}

// ---------------------------------------------------------------------------
//  Tide cards: the killer's deck. Every scenario uses the same 18 mechanics in
//  the same order (TideCards.Mechanics) with its own names and flavour.
//  Drawing the last one starts the finale.
// ---------------------------------------------------------------------------

public enum TideMove
{
    TowardVillagers,
    TowardSurvivor,
    ToSummonPoint,   // the scenario's summoning place (Gallowmouth: the Chapel bell)
    ToMostVillagers,
    ToLair,          // one of the scenario's lairs (Gallowmouth: the shore)
    Stay
}

public enum TideEffect
{
    None,
    Fog,
    Straggler,
    DreadUp,
    LowWater,
    ColdBreath,
    Mend
}

/// <summary>A killer card: who the killer goes for, how (walk or appear), whether they attack, and an effect.
/// How far they walk and how hard they hit come from the killer board (Bloodlust), not the card.</summary>
public sealed record TideCardDef(string Name, string Flavour, TideMove Move, bool Attacks, TideEffect Effect, bool Event = false);

public static class TideCards
{
    /// <summary>The 18 killer-card mechanics, in deck order. Scenarios pair these with names.</summary>
    /// <summary>Event = the card shows the event icon: reveal the next event card after it resolves.</summary>
    public static readonly (TideMove Move, bool Attacks, TideEffect Effect, bool Event)[] Mechanics =
    [
        (TideMove.TowardVillagers, true,  TideEffect.None, false),
        (TideMove.TowardVillagers, true,  TideEffect.None, true),
        (TideMove.TowardSurvivor,  true,  TideEffect.DreadUp, false),
        (TideMove.TowardVillagers, true,  TideEffect.None, false),
        (TideMove.TowardVillagers, false, TideEffect.Fog, false),
        (TideMove.TowardVillagers, false, TideEffect.Straggler, false),
        (TideMove.ToSummonPoint,   true,  TideEffect.None, false),
        (TideMove.TowardSurvivor,  true,  TideEffect.None, true),
        (TideMove.TowardVillagers, false, TideEffect.LowWater, false),
        (TideMove.TowardSurvivor,  true,  TideEffect.None, false),
        (TideMove.TowardVillagers, true,  TideEffect.None, false),
        (TideMove.ToMostVillagers, true,  TideEffect.None, true),
        (TideMove.TowardVillagers, false, TideEffect.Straggler, false),
        (TideMove.TowardSurvivor,  true,  TideEffect.ColdBreath, false),
        (TideMove.Stay,            true,  TideEffect.Mend, false),
        (TideMove.TowardVillagers, true,  TideEffect.Fog, false),
        (TideMove.TowardVillagers, true,  TideEffect.None, true),
        (TideMove.ToLair,          false, TideEffect.DreadUp, false),
    ];

    /// <summary>The current scenario's deck.</summary>
    public static IReadOnlyList<TideCardDef> All => Scenarios.Current.TideCards;

    public static string DescribeEffect(TideEffect e)
    {
        var s = Scenarios.Current;
        return e switch
        {
            TideEffect.Fog => $"{s.FogName}: searches roll one fewer die next turn.",
            TideEffect.Straggler => $"A straggler: a new {s.Victim} appears somewhere.",
            TideEffect.DreadUp => "Dread rises by 1.",
            TideEffect.LowWater => "Respite: +1 Time next turn.",
            TideEffect.ColdBreath => "Chilled: -1 Time next turn.",
            TideEffect.Mend => $"{s.Killer.She} mends 1 wound.",
            _ => ""
        };
    }
}

// ---------------------------------------------------------------------------
//  The killer board: a Bloodlust track. Every victim the killer kills moves the
//  marker up one level; each level sets how far the killer moves and how much
//  damage an attack does, and some levels trigger a one-time special effect.
// ---------------------------------------------------------------------------

public enum BloodlustEffect
{
    None,
    /// <summary>Your Dread rises by 1 (the skull on the board).</summary>
    DreadUp,
    /// <summary>The killer recovers 2 health.</summary>
    Frenzy
}

/// <param name="Move">Spaces the killer moves on a card that says "move".</param>
/// <param name="Attack">Damage dealt by an attack: health to you, or victims killed.</param>
/// <param name="Effect">One-time effect when the marker reaches this level.</param>
public sealed record BloodlustLevel(int Move, int Attack, BloodlustEffect Effect = BloodlustEffect.None);

public static class Bloodlust
{
    /// <summary>Health the killer recovers each time Bloodlust would rise past the top of the track (the final effect).</summary>
    public const int FinalEffectHeal = 1;

    public static string Describe(BloodlustEffect e, ScenarioDef s) => e switch
    {
        BloodlustEffect.DreadUp => "Your Dread rises by 1.",
        BloodlustEffect.Frenzy => $"Frenzy: {s.Killer.SheLower} recovers 2 health.",
        _ => ""
    };

    public static string Icon(BloodlustEffect e) => e switch
    {
        BloodlustEffect.DreadUp => "☠",
        BloodlustEffect.Frenzy => "♥",
        _ => ""
    };
}

// ---------------------------------------------------------------------------
//  Event cards. One is revealed at the start of the night, and another every
//  time a killer card shows the event icon. Some are one-time, some stay in
//  play (ongoing) until their discard condition. Some are good, some bad.
//  Mechanics are shared; each scenario gives them names and flavour.
// ---------------------------------------------------------------------------

public enum EventKind
{
    /// <summary>Special victim. Saved: +2 Hope. Killed in your space: +3 Time next round. Killed elsewhere: Dread +1.</summary>
    Companion,
    /// <summary>Special victim. Escorting them costs +1 Time. Saved: heal 2 and +1 Hope.</summary>
    Wounded,
    SecondWind,
    Supplies,
    Resolve,
    Adrenaline,
    Trap,
    MoreVictims,
    BadOmen,
    Blackout,
    Hunted,
    Bloodscent
}

/// <summary>Whether an event helps (+1), hurts (-1) or cuts both ways (0). Only used for colour.</summary>
public sealed record EventCardDef(EventKind Kind, string Name, string Flavour, string Text, string? Discard, bool Ongoing, int Tone);

/// <summary>An ongoing event in play. RoundsLeft counts down at the start of each round (0 = no timer).</summary>
public sealed class ActiveEvent
{
    public required EventCardDef Card { get; init; }
    public int RoundsLeft { get; set; }
    /// <summary>Where it applies, if anywhere (the trap's location, the special victim's location).</summary>
    public int Location { get; set; } = -1;
}

/// <summary>An event that was just revealed. Applied = false if it couldn't take effect and was ignored.</summary>
public sealed record EventReveal(EventCardDef Card, bool Applied, string Text, int Location);

public static class EventCards
{
    public const int TimedRounds = 3;

    /// <summary>Rules text for each kind, with scenario tokens. Order matches EventKind and ScenarioDef.EventNames.</summary>
    public static readonly (EventKind Kind, bool Ongoing, int Tone, string Text, string? Discard)[] Templates =
    [
        (EventKind.Companion, true, 1,
            "The {victim} nearest you becomes special (blue). If they reach the {sanctuary}: +2 extra Hope. If {she} kills them in your space, rage: +3 Time next round. If {she} kills them anywhere else: Dread +1.",
            "Discard when they are saved or killed."),
        (EventKind.Wounded, true, 0,
            "The {victim} nearest you is badly hurt (orange). Escorting them takes two escorts at once. If they reach the {sanctuary}: heal 2 and +1 extra Hope.",
            "Discard when they are saved or killed."),
        (EventKind.SecondWind, false, 1, "Heal 2.", null),
        (EventKind.Supplies, false, 1, "You find something useful: gain a random item.", null),
        (EventKind.Resolve, false, 1, "Dread -1 and +1 Hope.", null),
        (EventKind.Adrenaline, true, 1, "+1 Time at the start of each turn.", "Discard after 3 rounds."),
        (EventKind.Trap, true, 1, "You rig a trap where you stand. The first time {killer} ends a move there, {she} takes 2 wounds.", "Discard when it springs."),
        (EventKind.MoreVictims, false, 0, "2 more {victims} turn up at the place farthest from {killer}.", null),
        (EventKind.BadOmen, false, -1, "Dread +1.", null),
        (EventKind.Blackout, true, -1, "Searches roll 1 fewer die.", "Discard after 3 rounds."),
        (EventKind.Hunted, true, -1, "{She} moves 1 extra space.", "Discard when you wound {her}."),
        (EventKind.Bloodscent, false, -1, "{Killer}'s Bloodlust rises by 1.", null),
    ];

    public static string Icon(EventCardDef e) => e.Tone > 0 ? "✚" : e.Tone < 0 ? "✖" : "◆";
}
