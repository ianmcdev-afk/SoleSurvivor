namespace SoleSurvivor.Core;

public enum Phase { SurvivorTurn, Won, Lost }

/// <summary>Your turn has two steps, as in the board game: play cards (Action), then buy cards for next turn (Planning).</summary>
public enum TurnStep { Action, Planning }

public enum Difficulty { Easy, Normal, Hard }

public enum LogKind { Info, Good, Bad, Tide, System }

public sealed record LogEntry(int Round, LogKind Kind, string Text);

public enum DieFace { Blank, Partial, Success }

/// <summary>
/// A Horror roll. Faces: 1-2 blank; 3 shows two cards and 4 shows one card (partials: discard that many cards
/// to make the die a success); 5-6 a star (success). ConvertedDice = indices of dice bought with cards.
/// </summary>
public sealed record DiceRoll(string Label, IReadOnlyList<int> Values, int Threshold, int Successes, IReadOnlyList<int>? ConvertedDice = null)
{
    public static DieFace Face(int value) => value >= 5 ? DieFace.Success : value >= 3 ? DieFace.Partial : DieFace.Blank;

    /// <summary>Cards to discard to turn this face into a success: 2 for a 3, 1 for a 4, 0 if it can't be converted.</summary>
    public static int CardsFor(int value) => value switch { 3 => 2, 4 => 1, _ => 0 };

    public int Stars => Values.Count(v => Face(v) == DieFace.Success);
    public int Partials => Values.Count(v => Face(v) == DieFace.Partial);
    public int Converted => ConvertedDice?.Count ?? 0;
    public bool IsConverted(int index) => ConvertedDice?.Contains(index) == true;
}

/// <summary>A roll waiting for the player to decide whether to discard cards to convert partials.
/// MaxConversions is limited by partials rolled, cards in hand (2 each) and how many successes could matter.</summary>
public sealed record PendingRoll(DiceRoll Roll, int MaxConversions, string Purpose);

public enum TideEventKind
{
    CardDrawn,
    Narration,
    Moved,
    Rose,
    Blocked,
    Claimed,
    Straggler,
    Effect,
    Struck,
    Missed,
    Repelled,
    /// <summary>Bloodlust rose. Amount = the new level (0-based); Text names any special effect.</summary>
    Bloodlust,
    /// <summary>An event card was revealed. Amount = index into Scenario.EventCards; Text = what happened; To = where.</summary>
    EventRevealed,
    /// <summary>A trap sprang on the killer. To = where, Amount = wounds.</summary>
    TrapSprung,
    /// <summary>Something an event caused (a special victim died, an event ended). Text says what.</summary>
    EventNote,
    SurvivorDied,
    FinaleBegins,
    /// <summary>The swarm grew. To = where, Amount = how many appeared.</summary>
    SwarmSpawned,
    /// <summary>Part of the swarm moved one step. From/To, Amount = how many.</summary>
    SwarmMoved,
    /// <summary>The swarm attacked you. To = your location, Amount = damage (0 if you were protected).</summary>
    SwarmBit
}

/// <summary>One step of the tide, in order, so the UI can animate it. From/To are location ids (-1 if unused).</summary>
public sealed record TideEvent(TideEventKind Kind, string Text, int From, int To, int Amount, IReadOnlyList<int> Dice);

public sealed record TideReport(
    int Round,
    string CardName,
    string Flavour,
    bool IsFinaleTide,
    TideMove Move,
    /// <summary>Spaces moved, from the killer board at the time the card was drawn.</summary>
    int Steps,
    bool Attacks,
    /// <summary>Attack damage, from the killer board at the time the card was drawn.</summary>
    int Damage,
    int BloodlustBefore,
    bool RevealsEvent,
    IReadOnlyList<string> Lines,
    IReadOnlyList<TideEvent> Events,
    int Hits);

/// <summary>
/// The whole game state and rules. The UI calls CanX/X pairs and re-renders from the public state.
/// No UI types in here so the engine can be simulated headless.
/// </summary>
public sealed class Game
{
    /// <summary>Top of the Time track. Time resets to TimePerRound every turn; discards and events can push it higher.</summary>
    public const int MaxTime = 12;
    public const int MaxDread = 8;

    public const int FinaleHealthBonus = 3;
    public const int HandLimit = 7;
    /// <summary>Time gained by discarding a card from your hand.</summary>
    public const int DiscardRefund = 1;
    /// <summary>In the finale the killer hunts harder than the board says: +1 move and +1 damage.</summary>
    public const int FinaleMoveBonus = 1, FinaleDamageBonus = 1;

    private readonly Random _rng;

    private readonly int[] _villagers = new int[Board.Count];
    /// <summary>How many of the swarm are at each location (always zero if the scenario has no swarm).</summary>
    private readonly int[] _swarm = new int[Board.Count];
    public IReadOnlyList<int> Swarm => _swarm;
    public int SwarmTotal => _swarm.Sum();
    public int SwarmHere => _swarm[SurvivorLocation];
    /// <summary>How many of the swarm your last attack or item killed (for the UI).</summary>
    public int LastSwarmKilled { get; private set; }

    /// <summary>Damage the swarm does to you. They're brave in numbers: a lone one only hisses; 2-3 do 1, 4 or more do 2.</summary>
    public static int SwarmDamage(int count) => count <= 1 ? 0 : count >= 4 ? 2 : 1;
    private readonly List<ItemKind>[] _stashes = new List<ItemKind>[Board.Count];
    private readonly List<ItemKind> _inventory = new();
    /// <summary>The Action Tableau: every card you can buy, in piles by name.</summary>
    private readonly List<ActionCardDef> _tableau = new();
    /// <summary>Cards played or discarded since the last Planning phase. They go back to the tableau when it ends.</summary>
    private readonly List<ActionCardDef> _spent = new();
    private readonly List<ActionCardDef> _hand = new();
    private readonly List<TideCardDef> _tideDeck = new();
    private readonly List<EventCardDef> _eventDeck = new();
    private readonly List<ActiveEvent> _activeEvents = new();
    private readonly List<LogEntry> _log = new();
    private int _timeModNext;

    public int Seed { get; }
    public Difficulty Difficulty { get; }
    public int TimePerRound { get; }
    public int SurvivorMaxHealth { get; }
    public int TidewifeStartHealth { get; }
    public int Round { get; private set; } = 1;
    public Phase Phase { get; private set; } = Phase.SurvivorTurn;
    public bool InFinale { get; private set; }

    public int SurvivorLocation { get; private set; }
    public int TidewifeLocation { get; private set; }

    public int Health { get; private set; }
    public int Time { get; private set; }
    public int Hope { get; private set; } = 1;
    public int Dread { get; private set; }
    public int TidewifeHealth { get; private set; }
    public int TidewifeMaxHealth { get; private set; }

    public int VillagersSaved { get; private set; }
    public int VillagersLost { get; private set; }

    /// <summary>Moves and escorts granted by cards you've played this Action phase. Lost when it ends.</summary>
    public int MoveTokens { get; private set; }
    public int EscortTokens { get; private set; }

    /// <summary>Action phase (play cards) or Planning phase (buy cards).</summary>
    public TurnStep Step { get; private set; } = TurnStep.Action;

    public bool Barricaded { get; private set; }
    public bool HoldingBreath { get; private set; }
    public bool Repelled { get; private set; }
    public bool FogThisTurn { get; private set; }
    public bool FogNextTurn { get; private set; }

    /// <summary>When set, the next roll spends 1 Hope for +1 die.</summary>
    public bool SteelYourself { get; set; }

    public DiceRoll? LastRoll { get; private set; }
    public TideReport? LastTide { get; private set; }

    public IReadOnlyList<int> Villagers => _villagers;
    public IReadOnlyList<ItemKind> Inventory => _inventory;
    public IReadOnlyList<ActionCardDef> Tableau => _tableau;
    /// <summary>Played or discarded since the last Planning phase: back in the tableau after this turn's planning.</summary>
    public IReadOnlyList<ActionCardDef> SpentThisTurn => _spent;
    public IReadOnlyList<ActionCardDef> Hand => _hand;
    public IReadOnlyList<LogEntry> Log => _log;
    public int TideCardsLeft => _tideDeck.Count;

    // ---------------------------------------------------------------- events

    /// <summary>Ongoing events in play, oldest first.</summary>
    public IReadOnlyList<ActiveEvent> ActiveEvents => _activeEvents;
    public int EventCardsLeft => _eventDeck.Count;
    /// <summary>The most recent event revealed (the opening event right after the starting pick).</summary>
    public EventReveal? LastEvent { get; private set; }

    private ActiveEvent? Special => _activeEvents.FirstOrDefault(e => e.Card.Kind is EventKind.Companion or EventKind.Wounded);
    /// <summary>Where the special victim is (-1 if none). They are also counted in Villagers at that location.</summary>
    public int SpecialVictimAt => Special?.Location ?? -1;
    public EventKind? SpecialVictimKind => Special?.Card.Kind;
    private int ActiveCount(EventKind k) => _activeEvents.Count(e => e.Card.Kind == k);

    /// <summary>True until the night begins (after the briefing). Nothing can happen until then.</summary>
    public bool AwaitingStart { get; private set; } = true;
    public bool InPlanning => Phase == Phase.SurvivorTurn && !AwaitingStart && Pending is null && Step == TurnStep.Planning;
    public int StashCount(int location) => _stashes[location].Count;
    public int VillagersRemaining => _villagers.Sum();

    public bool IsOver => Phase != Phase.SurvivorTurn;
    public bool TidewifeHere => TidewifeLocation == SurvivorLocation;
    public bool TidewifeNear => TidewifeHere || (Board.AreAdjacent(TidewifeLocation, SurvivorLocation) && KillerCanPass(TidewifeLocation, SurvivorLocation));

    /// <summary>A star (5 or 6) is a success. A 3 or 4 is a partial: discard 2 cards from your hand to make it a success.</summary>
    public int SuccessThreshold => 5;

    /// <summary>A Horror roll waiting on the player's choice to convert partials. No other action is allowed meanwhile.</summary>
    public PendingRoll? Pending { get; private set; }
    private Action<int>? _resolvePending;

    /// <summary>The Horror track: how many dice your rolls start from at a given Dread.
    /// Uneasy (0-2): 3, Afraid (3-5): 2, Terror (6+) and the finale: 1.</summary>
    public static int HorrorDiceFor(int dread, bool finale) => finale ? 1 : dread switch { <= 2 => 3, <= 5 => 2, _ => 1 };

    /// <summary>Base dice for every Fight and Search right now.</summary>
    public int HorrorDice => HorrorDiceFor(Dread, InFinale);

    // ---------------------------------------------------------------- killer board

    /// <summary>Bloodlust marker, 0 = bottom of the track. Rises by 1 for every victim killed.</summary>
    public int Bloodlust { get; private set; }
    public int BloodlustMax => Scenario.BloodlustTrack.Length - 1;
    public BloodlustLevel KillerLevel => Scenario.BloodlustTrack[Bloodlust];
    /// <summary>Spaces the killer moves when a card says move (finale: +1).</summary>
    public int KillerMove => KillerLevel.Move + (InFinale ? FinaleMoveBonus : 0) + ActiveCount(EventKind.Hunted);
    /// <summary>Damage of one attack (finale: +1): health to you, or victims killed.</summary>
    public int KillerAttack => KillerLevel.Attack + (InFinale ? FinaleDamageBonus : 0);

    public string DreadLabel => InFinale ? Scenario.FinaleName : Dread switch
    {
        <= 2 => "Uneasy",
        <= 5 => "Afraid",
        _ => "Terror"
    };

    public int FightBonus => _inventory.Sum(i => Items.All[i].FightBonus);
    public int SearchBonus => _inventory.Sum(i => Items.All[i].SearchBonus);
    public int EscortCapacity => 2 + (_inventory.Contains(ItemKind.Rope) ? 1 : 0);
    /// <summary>Dice for a fight: Horror dice, plus a card's modifier, plus weapons. Never fewer than 1.</summary>
    public int FightDice(int modifier = 0) => Math.Max(1, HorrorDice + modifier + FightBonus);

    /// <summary>Dice for a search: Horror dice, plus a card's modifier, plus light, minus fog. Never fewer than 1.</summary>
    public int SearchDice(int modifier = 0) => Math.Max(1, HorrorDice + modifier + SearchBonus - (FogThisTurn ? 1 : 0) - ActiveCount(EventKind.Blackout));

    /// <summary>The scenario this game is played in.</summary>
    public ScenarioDef Scenario { get; }

    public Game(int? seed = null, Difficulty difficulty = Difficulty.Normal, ScenarioDef? scenario = null)
    {
        Scenario = scenario ?? Scenarios.Current;
        Scenarios.Use(Scenario);
        SurvivorLocation = Scenario.SanctuaryId;
        Dread = Scenario.StartDread;
        TidewifeLocation = Scenario.KillerStart;
        Seed = seed ?? Random.Shared.Next();
        _rng = new Random(Seed);
        Difficulty = difficulty;
        (TimePerRound, SurvivorMaxHealth, TidewifeStartHealth) = difficulty switch
        {
            Difficulty.Easy => (6, 6, 20),
            Difficulty.Hard => (4, 5, 25),
            _ => (5, 5, 23)
        };
        TidewifeStartHealth += Scenario.Killer.HealthBonus;
        Time = TimePerRound;
        Health = SurvivorMaxHealth;
        TidewifeHealth = TidewifeMaxHealth = TidewifeStartHealth;

        foreach (var loc in Board.Locations)
        {
            _villagers[loc.Id] = loc.StartVillagers;
            _stashes[loc.Id] = Shuffle(loc.Stash.ToList());
        }
        if (Scenario.HasSwarm)
        {
            // The swarm starts spread over the lairs (never on you).
            var lairs = Board.Locations.Where(l => l.IsLair && l.Id != SurvivorLocation).Select(l => l.Id).ToList();
            if (lairs.Count == 0) lairs.Add(TidewifeLocation);
            for (int i = 0; i < Scenario.SwarmStart; i++) _swarm[lairs[i % lairs.Count]]++;
        }

        // The tableau holds every buyable card; you start holding the zero-cost basics.
        foreach (var card in ActionCards.All)
            for (int i = 0; i < card.Copies; i++)
                _tableau.Add(card);
        foreach (var card in ActionCards.Starters)
            for (int i = 0; i < card.Copies; i++)
                _hand.Add(card);

        _tideDeck.AddRange(TideCards.All);
        Shuffle(_tideDeck);

        _eventDeck.AddRange(Scenario.EventCards);
        Shuffle(_eventDeck);

        Write(LogKind.System, Scenario.Intro);
    }

    /// <summary>Begin the night: reveal the opening event and start the first Action phase. Call once, after the briefing.</summary>
    public bool Begin()
    {
        if (!AwaitingStart || Phase != Phase.SurvivorTurn) return false;
        AwaitingStart = false;
        Write(LogKind.Info, "You set out with " + string.Join(", ", _hand.Select(c => c.Name)) + ".");
        RevealEvent();
        Write(LogKind.System, $"Round 1. Action phase: play cards from your hand. You have {Time} Time to spend in Planning.");
        return true;
    }

    // ------------------------------------------------------------------ helpers

    private List<T> Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = _rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
        return list;
    }

    private void Write(LogKind kind, string text) => _log.Add(new LogEntry(Round, kind, text));

    private static string Name(int location) => Board.Locations[location].Name;

    private static string Plural(int n, string one, string many) => n == 1 ? $"1 {one}" : $"{n} {many}";

    /// <summary>The Action phase: play cards, move, use items.</summary>
    private bool Acting => Phase == Phase.SurvivorTurn && !AwaitingStart && Pending is null && Step == TurnStep.Action;
    public bool InAction => Acting;

    private DiceRoll Roll(string label, int dice, int threshold)
    {
        if (SteelYourself && Hope > 0)
        {
            Hope--;
            dice++;
            label += " (steeled)";
        }
        SteelYourself = false;

        var values = new int[Math.Max(0, dice)];
        for (int i = 0; i < values.Length; i++) values[i] = _rng.Next(1, 7);
        var roll = new DiceRoll(label, values, threshold, values.Count(v => v >= threshold));
        LastRoll = roll;
        return roll;
    }

    private bool Spend(int cost)
    {
        if (Time < cost) return false;
        Time -= cost;
        return true;
    }

    private void Heal(int amount, string source)
    {
        int before = Health;
        Health = Math.Min(SurvivorMaxHealth, Health + amount);
        Write(LogKind.Good, $"{source}: healed {Health - before}.");
    }

    private void WoundTidewife(int wounds, string source)
    {
        if (wounds <= 0)
        {
            Write(LogKind.Info, $"{source}: {Scenario.Killer.SheLower} doesn't even flinch.");
            return;
        }
        TidewifeHealth = Math.Max(0, TidewifeHealth - wounds);
        Write(LogKind.Good, $"{source}: {Plural(wounds, "wound", "wounds")} to {Scenario.Killer.NameLower} ({TidewifeHealth} left).");
        foreach (var hunted in _activeEvents.Where(e => e.Card.Kind == EventKind.Hunted).ToList())
        {
            _activeEvents.Remove(hunted);
            Write(LogKind.Good, $"{hunted.Card.Name} is discarded: you hurt {Scenario.Killer.Her}, and {Scenario.Killer.SheLower} falls back.");
        }
        if (TidewifeHealth == 0)
        {
            Phase = Phase.Won;
            Write(LogKind.System, Scenario.WinText);
        }
    }

    private void ArriveVillagers(int location, int count, bool special = false)
    {
        if (count <= 0) return;
        if (Board.Locations[location].IsSanctuary)
        {
            VillagersSaved += count;
            Hope += count;
            Write(LogKind.Good, $"{Plural(count, Scenario.Victim + " reaches", Scenario.Victims + " reach")} the {Scenario.SanctuaryName}. +{count} Hope.");
            if (special) SpecialSaved();
        }
        else
        {
            _villagers[location] += count;
            if (special && Special is { } sp) sp.Location = location;
        }
    }

    // ------------------------------------------------------------------ doors, keys and vehicles

    private readonly HashSet<(int, int)> _unlocked = new();
    private static (int, int) EdgeKey(int a, int b) => (Math.Min(a, b), Math.Max(a, b));

    /// <summary>A door that is still locked. Nobody walks through it, the killer included, until you open it with a key.</summary>
    public bool IsLocked(int a, int b) => Scenario.IsLockedEdge(a, b) && !_unlocked.Contains(EdgeKey(a, b));
    public IEnumerable<(int A, int B)> LockedDoors => Scenario.LockedEdges.Where(e => IsLocked(e.A, e.B));
    private bool CanPass(int a, int b) => !IsLocked(a, b);
    /// <summary>The killer's doors: locked ones stop {her} until the finale, when nothing does (no hiding behind a door forever).</summary>
    private bool KillerCanPass(int a, int b) => InFinale || !IsLocked(a, b);

    public int Keys => _inventory.Count(i => i == ItemKind.Key);
    public bool HasVehicle => Scenario.VehicleRange >= 2 && _inventory.Contains(ItemKind.Vehicle);

    /// <summary>The route of your last walk, escort or drive, starting where you were. The UI animates along it.</summary>
    public IReadOnlyList<int> LastRoute { get; private set; } = Array.Empty<int>();

    /// <summary>
    /// How you would get to <paramref name="to"/> in one move, excluding where you are now, or null if you can't.
    /// On foot: one step, through a locked door only if you carry a key. With a vehicle: up to VehicleRange steps,
    /// never through a locked door, and never driving through the killer's space (you can stop there).
    /// </summary>
    public List<int>? RouteTo(int to)
    {
        int from = SurvivorLocation;
        if (to == from || to < 0 || to >= Board.Count) return null;
        if (Board.AreAdjacent(from, to))
            return !IsLocked(from, to) || Keys > 0 ? new List<int> { to } : null;
        if (!HasVehicle) return null;
        // Breadth-first over open roads, not passing through the killer.
        var prev = new int[Board.Count];
        var depth = new int[Board.Count];
        Array.Fill(prev, -1);
        var seen = new bool[Board.Count];
        var q = new Queue<int>();
        q.Enqueue(from);
        seen[from] = true;
        while (q.Count > 0)
        {
            int c = q.Dequeue();
            if (depth[c] >= Scenario.VehicleRange) continue;
            if (c != from && c == TidewifeLocation) continue; // can stop on her, not drive through her
            foreach (int n in Board.Neighbours(c))
            {
                if (seen[n] || !CanPass(c, n)) continue;
                seen[n] = true;
                prev[n] = c;
                depth[n] = depth[c] + 1;
                if (n == to)
                {
                    var path = new List<int>();
                    for (int p = to; p != from; p = prev[p]) path.Add(p);
                    path.Reverse();
                    return path;
                }
                q.Enqueue(n);
            }
        }
        return null;
    }

    /// <summary>Follow a route: open a locked door on the way with a key, and remember it for the animation.</summary>
    private void Travel(List<int> route)
    {
        int from = SurvivorLocation;
        if (route.Count == 1 && IsLocked(from, route[0]))
        {
            _inventory.Remove(ItemKind.Key);
            _unlocked.Add(EdgeKey(from, route[0]));
            Write(LogKind.Good, $"You unlock the door between the {Name(from)} and the {Name(route[0])} with the {Scenario.KeyName}. It stays open.");
        }
        var full = new List<int> { from };
        full.AddRange(route);
        LastRoute = full;
        SurvivorLocation = route[^1];
    }

    private string MoveWord(List<int> route) =>
        route.Count > 1 ? $"{Scenario.VehicleVerb} the {Scenario.VehicleName?.ToLowerInvariant()}" : "walk";

    // ------------------------------------------------------------------ movement

    /// <summary>Moving needs a move from a card you've played (Walk, Sprint...). There are no free moves.</summary>
    public bool CanMove(int to) =>
        Acting && MoveTokens > 0 && RouteTo(to) is not null;

    public bool Move(int to)
    {
        if (!CanMove(to)) return false;
        var route = RouteTo(to)!;
        MoveTokens--;
        Travel(route);
        Write(LogKind.Info, route.Count > 1 ? $"You {MoveWord(route)} to the {Name(to)}." : $"You walk to the {Name(to)}.");
        return true;
    }

    /// <summary>Escorts used by one escort from here: 1, or 2 if the badly hurt special victim is with you.</summary>
    public int EscortCost => 1 + (SpecialVictimAt == SurvivorLocation && SpecialVictimKind == EventKind.Wounded ? 1 : 0);

    /// <summary>Escorting needs escorts from a card you've played (Lead, Shepherd...).</summary>
    public bool CanEscort(int to) =>
        Acting && !InFinale && _villagers[SurvivorLocation] > 0
        && EscortTokens >= EscortCost && RouteTo(to) is not null;

    public bool Escort(int to)
    {
        if (!CanEscort(to)) return false;
        var route = RouteTo(to)!;
        EscortTokens -= EscortCost;
        int from = SurvivorLocation;
        int count = Math.Min(EscortCapacity, _villagers[from]);
        bool special = SpecialVictimAt == from; // the special victim always comes with you
        _villagers[from] -= count;
        Travel(route);
        Write(LogKind.Info, route.Count > 1
            ? $"You {MoveWord(route)} {Plural(count, Scenario.Victim, Scenario.Victims)} to the {Name(to)}."
            : $"You lead {Plural(count, Scenario.Victim, Scenario.Victims)} to the {Name(to)}.");
        ArriveVillagers(to, count, special);
        return true;
    }

    // ------------------------------------------------------------------ basic actions

    private void DoSearch(int dice, string label)
    {
        var roll = Roll(label, dice, SuccessThreshold);
        int useful = Math.Min(2, _stashes[SurvivorLocation].Count);
        Offer(roll, useful, "search", successes => FinishSearch(successes, label));
    }

    private void FinishSearch(int successes, string label)
    {
        var stash = _stashes[SurvivorLocation];
        int found = Math.Min(Math.Min(successes, 2), stash.Count);
        if (found == 0)
        {
            Write(LogKind.Bad, $"{label}: you find nothing useful in the {Name(SurvivorLocation)}.");
            return;
        }
        for (int i = 0; i < found; i++)
        {
            var item = stash[^1];
            stash.RemoveAt(stash.Count - 1);
            _inventory.Add(item);
            Write(LogKind.Good, $"{label}: you find {Items.All[item].Name}.");
        }
    }

    /// <summary>
    /// Fight whatever is in your space. With a swarm, each success kills one of them first (they shield the killer);
    /// what's left over wounds the killer, if it's here.
    /// </summary>
    private void DoFight(int dice, string label)
    {
        var roll = Roll(label, dice, SuccessThreshold);
        int useful = SwarmHere + (TidewifeHere ? TidewifeHealth : 0);
        LastSwarmKilled = 0;
        Offer(roll, useful, "fight", successes =>
        {
            successes -= KillSwarm(successes, label);
            if (TidewifeHere) WoundTidewife(successes, label);
            else if (successes == 0 && LastSwarmKilled == 0) Write(LogKind.Info, $"{label}: you miss.");
        });
    }

    /// <summary>Kill up to <paramref name="count"/> of the swarm in your space. Returns how many died.</summary>
    private int KillSwarm(int count, string source)
    {
        int killed = Math.Min(count, _swarm[SurvivorLocation]);
        if (killed <= 0) return 0;
        _swarm[SurvivorLocation] -= killed;
        LastSwarmKilled += killed;
        Write(LogKind.Good, $"{source}: you kill {Plural(killed, Scenario.SwarmName!, Scenario.SwarmNames)}"
            + (_swarm[SurvivorLocation] > 0 ? $" ({_swarm[SurvivorLocation]} still here)." : "."));
        return killed;
    }

    /// <summary>Resolve a roll now, or, if partials could be converted to something useful, wait for the player.</summary>
    private void Offer(DiceRoll roll, int usefulSuccesses, string purpose, Action<int> resolve)
    {
        // How many partials could you actually afford, cheapest first, and how many would matter?
        int affordable = 0, cards = _hand.Count;
        foreach (int cost in roll.Values.Select(DiceRoll.CardsFor).Where(c => c > 0).OrderBy(c => c))
        {
            if (cost > cards) break;
            cards -= cost;
            affordable++;
        }
        int max = Math.Min(affordable, Math.Max(0, usefulSuccesses - roll.Successes));
        if (max <= 0)
        {
            resolve(roll.Successes);
            return;
        }
        Pending = new PendingRoll(roll, max, purpose);
        _resolvePending = resolve;
        Write(LogKind.Info, $"{roll.Label}: {Plural(roll.Partials, "partial", "partials")}. Discard the cards shown on a die (2 for a 3, 1 for a 4) to make it a success.");
    }

    /// <summary>
    /// Convert the chosen partial dice by discarding exactly the cards they show (2 for a 3, 1 for a 4), then resolve the roll.
    /// Pass no dice and no cards to keep the roll as it is. Discarded starting cards are gone; others go to the discard pile.
    /// </summary>
    public bool ConvertPartials(IReadOnlyCollection<int> dieIndices, IReadOnlyCollection<int> handIndices)
    {
        if (Pending is not { } pending || _resolvePending is null) return false;
        var dice = dieIndices.Distinct().ToList();
        var picks = handIndices.Distinct().ToList();
        if (dice.Count > pending.MaxConversions) return false;
        if (dice.Any(d => d < 0 || d >= pending.Roll.Values.Count || DiceRoll.CardsFor(pending.Roll.Values[d]) == 0)) return false;
        if (picks.Count != dice.Sum(d => DiceRoll.CardsFor(pending.Roll.Values[d]))) return false;
        if (picks.Any(i => i < 0 || i >= _hand.Count)) return false;

        int converted = dice.Count;
        var names = new List<string>();
        foreach (int i in picks.OrderByDescending(i => i))
        {
            var card = _hand[i];
            _hand.RemoveAt(i);
            _spent.Add(card);
            names.Add(card.Name);
        }
        var roll = pending.Roll with { Successes = pending.Roll.Successes + converted, ConvertedDice = dice.OrderBy(d => d).ToList() };
        LastRoll = roll;
        var resolve = _resolvePending;
        Pending = null;
        _resolvePending = null;
        if (converted > 0)
            Write(LogKind.Good, $"You discard {string.Join(", ", names)}: {Plural(converted, "partial becomes a success", "partials become successes")}.");
        resolve(roll.Successes);
        return true;
    }

    // ------------------------------------------------------------------ cards

    /// <summary>
    /// Planning phase: buy a card from the tableau with Time (its cost). Zero-cost cards are always free to take.
    /// Cards you played or discarded this turn aren't in the tableau until Planning ends. Hand limit applies.
    /// </summary>
    public bool CanBuy(ActionCardDef card) =>
        InPlanning && _hand.Count < HandLimit && _tableau.Contains(card) && Time >= card.Cost;

    /// <summary>Why a tableau card can't be bought right now, or null if it can.</summary>
    public string? BuyBlockedReason(ActionCardDef card)
    {
        if (!InPlanning) return "buy in the Planning phase";
        if (!_tableau.Contains(card)) return _spent.Contains(card) ? "used this turn" : "none left";
        if (_hand.Count >= HandLimit) return "hand full";
        if (Time < card.Cost) return "not enough Time";
        return null;
    }

    /// <summary>How many of this card are in the tableau now.</summary>
    public int TableauCount(ActionCardDef card) => _tableau.Count(c => ReferenceEquals(c, card));

    public bool Buy(ActionCardDef card)
    {
        if (!CanBuy(card)) return false;
        Spend(card.Cost);
        _tableau.Remove(card);
        _hand.Add(card);
        Write(LogKind.Info, card.Cost == 0 ? $"You take {card.Name} back into your hand." : $"You buy {card.Name} for {card.Cost} Time.");
        return true;
    }

    /// <summary>Any time on your turn (Action or Planning), a card in hand can be thrown away for +1 Time.</summary>
    public bool CanDiscard(int handIndex) =>
        (Acting || InPlanning) && handIndex >= 0 && handIndex < _hand.Count && Time < MaxTime;

    public bool Discard(int handIndex)
    {
        if (!CanDiscard(handIndex)) return false;
        var card = _hand[handIndex];
        _hand.RemoveAt(handIndex);
        _spent.Add(card);
        Time = Math.Min(MaxTime, Time + DiscardRefund);
        Write(LogKind.Info, $"You discard {card.Name} for +{DiscardRefund} Time.");
        return true;
    }

    public bool CanPlay(int handIndex)
    {
        if (!Acting || handIndex < 0 || handIndex >= _hand.Count) return false;
        return _hand[handIndex].Effect switch
        {
            CardEffect.WildSwing or CardEffect.DesperatePlan => TidewifeHere || SwarmHere > 0,
            CardEffect.Rummage => _stashes[SurvivorLocation].Count > 0,
            CardEffect.BarTheDoor => !TidewifeHere && !Barricaded,
            CardEffect.HoldBreath => !HoldingBreath,
            CardEffect.LanternSignal => !InFinale && Board.Neighbours(SurvivorLocation).Any(n => _villagers[n] > 0),
            CardEffect.PatchUp => Health < SurvivorMaxHealth,
            CardEffect.Lure => !TidewifeHere,
            CardEffect.Shepherd => !InFinale,
            CardEffect.Guide or CardEffect.Holler => !InFinale,
            CardEffect.Peek => _stashes[SurvivorLocation].Count > 0,
            CardEffect.Shove => TidewifeHere || SwarmHere > 0,
            CardEffect.GlanceAround => _stashes[SurvivorLocation].Count > 0,
            CardEffect.QuickStep => true,
            CardEffect.CatchBreath => Health < SurvivorMaxHealth && !TidewifeHere,
            _ => true
        };
    }

    public bool Play(int handIndex)
    {
        if (!CanPlay(handIndex)) return false;
        var card = _hand[handIndex];
        _hand.RemoveAt(handIndex);
        _spent.Add(card); // back in the tableau once this turn's Planning ends

        switch (card.Effect)
        {
            case CardEffect.Sprint:
                MoveTokens += 3;
                Write(LogKind.Info, "Sprint: up to 3 moves. Click where to go.");
                break;
            case CardEffect.Shepherd:
                EscortTokens += 2;
                Write(LogKind.Info, "Shepherd: up to 2 escorts.");
                break;
            case CardEffect.WildSwing:
                DoFight(FightDice(+1), card.Name);
                break;
            case CardEffect.DesperatePlan:
                DoFight(FightDice(+2), card.Name);
                break;
            case CardEffect.Rummage:
                DoSearch(SearchDice(+1), card.Name);
                break;
            case CardEffect.BarTheDoor:
                Barricaded = true;
                Write(LogKind.Info, $"You bar the door. {Scenario.Killer.She} cannot come in on the next {Scenario.RoundWord}.");
                break;
            case CardEffect.HoldBreath:
                HoldingBreath = true;
                Write(LogKind.Info, "You hold your breath. The next hit will glance off.");
                break;
            case CardEffect.LanternSignal:
            {
                int total = 0;
                bool special = false;
                foreach (int n in Board.Neighbours(SurvivorLocation))
                {
                    total += _villagers[n];
                    _villagers[n] = 0;
                    if (SpecialVictimAt == n) special = true;
                }
                Write(LogKind.Info, $"You signal. {Plural(total, Scenario.Victim + " comes", Scenario.Victims + " come")} running.");
                ArriveVillagers(SurvivorLocation, total, special);
                break;
            }
            case CardEffect.SteelNerves:
                Hope++;
                Write(LogKind.Good, "Steel Nerves: +1 Hope.");
                break;
            case CardEffect.PatchUp:
                Heal(2, "Patch Up");
                break;
            case CardEffect.QuickStep:
            case CardEffect.Scramble:
                MoveTokens += 1;
                Write(LogKind.Info, $"{card.Name}: 1 move. Click where to go.");
                break;
            case CardEffect.Guide:
            case CardEffect.Holler:
                EscortTokens += 1;
                Write(LogKind.Info, $"{card.Name}: 1 escort.");
                break;
            case CardEffect.Shove:
                DoFight(FightDice(-1), card.Name);
                break;
            case CardEffect.GlanceAround:
                DoSearch(SearchDice(), card.Name);
                break;
            case CardEffect.Peek:
                DoSearch(SearchDice(-1), card.Name);
                break;
            case CardEffect.CatchBreath:
                Heal(1, card.Name);
                break;
            case CardEffect.Lure:
            {
                var path = Board.Path(TidewifeLocation, SurvivorLocation, null, KillerCanPass);
                if (path is { Count: > 0 })
                {
                    TidewifeLocation = path[0];
                    Write(LogKind.Info, $"{Scenario.LureText} {Scenario.Killer.She} moves to the {Name(TidewifeLocation)}.");
                }
                break;
            }
        }
        return true;
    }

    // ------------------------------------------------------------------ items

    /// <summary>Why a one-use item can't be used right now, in a few words, or null if it can (or isn't one-use).</summary>
    public string? UseBlockedReason(int inventoryIndex)
    {
        if (inventoryIndex < 0 || inventoryIndex >= _inventory.Count) return null;
        var kind = _inventory[inventoryIndex];
        if (!Items.All[kind].Consumable) return null;
        if (IsOver) return "the night is over";
        if (AwaitingStart) return "the night hasn't started";
        if (Step == TurnStep.Planning) return "only in your Action phase";
        if (Pending is not null) return "finish your roll first";
        string killer = Scenario.Killer.ShortName;
        return kind switch
        {
            ItemKind.FlareGun when !TidewifeNear && SwarmHere == 0 => Scenario.HasSwarm ? $"needs {killer} or {Scenario.SwarmNames} nearby" : $"needs {killer} here or next door",
            ItemKind.BlessedSalt when !TidewifeNear => $"needs {killer} here or next door",
            ItemKind.Bandages when Health >= SurvivorMaxHealth => "you're at full health",
            ItemKind.IronNails when TidewifeHere => $"not with {killer} here",
            ItemKind.IronNails when Barricaded => "door already barred",
            _ => null
        };
    }

    public bool CanUse(int inventoryIndex)
    {
        if (!Acting || inventoryIndex < 0 || inventoryIndex >= _inventory.Count) return false;
        return _inventory[inventoryIndex] switch
        {
            ItemKind.FlareGun => TidewifeNear || SwarmHere > 0,
            ItemKind.BlessedSalt => TidewifeNear,
            ItemKind.Bandages => Health < SurvivorMaxHealth,
            ItemKind.IronNails => !TidewifeHere && !Barricaded,
            _ => false
        };
    }

    public bool Use(int inventoryIndex)
    {
        if (!CanUse(inventoryIndex)) return false;
        var item = _inventory[inventoryIndex];
        _inventory.RemoveAt(inventoryIndex);
        switch (item)
        {
            case ItemKind.FlareGun:
                LastSwarmKilled = 0;
                KillSwarm(2, Items.All[ItemKind.FlareGun].Name); // with a swarm, the flare also burns up to 2 of them here
                if (TidewifeNear) WoundTidewife(2, Items.All[ItemKind.FlareGun].Name);
                break;
            case ItemKind.BlessedSalt:
                Repelled = true;
                WoundTidewife(1, Items.All[ItemKind.BlessedSalt].Name);
                if (Phase == Phase.SurvivorTurn) Write(LogKind.Info, $"{Scenario.RepelUse} {Scenario.Killer.She} won't strike you next {Scenario.RoundWord}.");
                break;
            case ItemKind.Bandages:
                Heal(2, Items.All[ItemKind.Bandages].Name);
                break;
            case ItemKind.IronNails:
                Barricaded = true;
                Write(LogKind.Info, $"{Scenario.BarricadeText} {Scenario.Killer.She} cannot come in on the next {Scenario.RoundWord}.");
                break;
        }
        return true;
    }

    // ------------------------------------------------------------------ the tide

    private readonly List<TideEvent> _tideEvents = new();
    private readonly List<string> _tideLines = new();

    private void Emit(TideEventKind kind, string? text, int from = -1, int to = -1, int amount = 0, IReadOnlyList<int>? dice = null)
    {
        _tideEvents.Add(new TideEvent(kind, text ?? "", from, to, amount, dice ?? Array.Empty<int>()));
        if (!string.IsNullOrEmpty(text))
        {
            _tideLines.Add(text);
            Write(LogKind.Tide, text);
        }
    }

    /// <summary>End the Action phase and move on to Planning. Unused moves and escorts are lost.</summary>
    public bool CanEndActions => Acting;

    public void EndActions()
    {
        if (!Acting) return;
        Step = TurnStep.Planning;
        MoveTokens = 0;
        EscortTokens = 0;
        Write(LogKind.System, $"Planning phase: buy cards for next turn with your {Time} Time. Unspent Time is lost.");
    }

    /// <summary>End Planning: Time resets, cards used this turn go back to the tableau, and the killer takes a turn.</summary>
    public bool CanEndTurn => InPlanning;

    public void EndTurn()
    {
        if (!InPlanning) return;
        _tableau.AddRange(_spent);
        _spent.Clear();
        ResolveTide();
        if (Phase == Phase.SurvivorTurn) StartRound();
    }

    private void StartRound()
    {
        Round++;
        Time = Math.Clamp(TimePerRound + _timeModNext, 0, MaxTime);
        _timeModNext = 0;
        Step = TurnStep.Action;
        FogThisTurn = FogNextTurn;
        FogNextTurn = false;
        MoveTokens = 0;
        EscortTokens = 0;
        SteelYourself = false;
        TickEvents();
        Write(LogKind.System, $"Round {Round}. Action phase: play cards from your hand. {Time} Time for Planning.");
    }

    private void ResolveTide()
    {
        _tideEvents.Clear();
        _tideLines.Clear();
        _lastCard = null;

        if (!InFinale && _tideDeck.Count == 0)
            BeginFinale();

        int? blocked = Barricaded ? SurvivorLocation : null;
        bool finaleTide = InFinale;
        int bloodlustBefore = Bloodlust;
        int steps = KillerMove, damage = KillerAttack;
        string cardName, flavour;
        bool attacks;
        TideMove move;

        if (finaleTide)
        {
            cardName = Scenario.FinaleName;
            flavour = Scenario.FinaleFlavour;
            move = TideMove.TowardSurvivor;
            attacks = true;
            Emit(TideEventKind.CardDrawn, null);
            MoveToward(SurvivorLocation, steps, huntingSurvivor: true, blocked);
        }
        else
        {
            var card = _tideDeck[^1];
            _tideDeck.RemoveAt(_tideDeck.Count - 1);
            _lastCard = card;
            cardName = card.Name;
            flavour = card.Flavour;
            move = card.Move;
            attacks = card.Attacks;
            Emit(TideEventKind.CardDrawn, null);

            switch (card.Move)
            {
                case TideMove.TowardVillagers:
                    MoveToward(VillagerTarget(blocked), steps, huntingSurvivor: false, blocked);
                    break;
                case TideMove.TowardSurvivor:
                    MoveToward(SurvivorLocation, steps, huntingSurvivor: true, blocked);
                    break;
                case TideMove.ToSummonPoint:
                    Teleport(Scenario.SummonId, blocked);
                    break;
                case TideMove.ToMostVillagers:
                {
                    int best = -1;
                    for (int i = 0; i < Board.Count; i++)
                        if (_villagers[i] > 0 && i != blocked && (best < 0 || _villagers[i] > _villagers[best])) best = i;
                    if (best >= 0) Teleport(best, blocked);
                    else Emit(TideEventKind.Narration, Scenario.NobodyThere);
                    break;
                }
                case TideMove.ToLair:
                {
                    var shores = Board.Locations.Where(l => l.IsLair && l.Id != blocked).Select(l => l.Id).ToList();
                    if (shores.Count > 0) Teleport(shores[_rng.Next(shores.Count)], blocked);
                    break;
                }
                case TideMove.Stay:
                    Emit(TideEventKind.Narration, $"{Scenario.Killer.She} {Scenario.LingerVerb} at the {Name(TidewifeLocation)}.");
                    break;
            }
        }

        // A trap springs if she ends her move on it.
        if (_activeEvents.FirstOrDefault(e => e.Card.Kind == EventKind.Trap && e.Location == TidewifeLocation) is { } trap)
        {
            _activeEvents.Remove(trap);
            Emit(TideEventKind.TrapSprung, $"{trap.Card.Name}: the trap springs on {Scenario.Killer.NameLower} at the {Name(TidewifeLocation)}!",
                to: TidewifeLocation, amount: 2);
            WoundTidewife(2, trap.Card.Name);
        }

        // Attack everyone in her space. Damage comes from the killer board: that many victims die, and you lose that much health.
        int hits = 0;
        if (attacks && Phase == Phase.SurvivorTurn)
        {
            bool victimsHere = _villagers[TidewifeLocation] > 0;
            if (victimsHere) KillVictims(damage);
            if (TidewifeHere) hits = StrikeSurvivor(damage);
        }

        if (Scenario.HasSwarm && Phase == Phase.SurvivorTurn) ResolveSwarm(blocked, finaleTide, attacks);

        bool revealsEvent = false;
        if (!finaleTide && _lastCard is { } drawn && Phase == Phase.SurvivorTurn)
        {
            ApplyEffect(drawn.Effect);
            if (drawn.Event && Phase == Phase.SurvivorTurn)
            {
                revealsEvent = true;
                RevealEvent();
            }
        }

        Barricaded = false;
        HoldingBreath = false;
        Repelled = false;

        if (Phase == Phase.SurvivorTurn && !InFinale && Dread >= MaxDread)
            BeginFinale();

        LastTide = new TideReport(Round, cardName, flavour, finaleTide, move, steps, attacks, damage, bloodlustBefore, revealsEvent,
            _tideLines.ToList(), _tideEvents.ToList(), hits);
    }

    private TideCardDef? _lastCard;

    /// <summary>
    /// The swarm's turn, after the killer's: every group creeps one step toward you, then on an attack card the killer
    /// spawns more where it stands, and any on you nip for small damage.
    /// </summary>
    private void ResolveSwarm(int? blocked, bool finale, bool cardAttacks)
    {
        string one = Scenario.SwarmName!, many = Scenario.SwarmNames;
        // Each group moves at once, from where it stood at the start of the swarm's move.
        var start = _swarm.ToArray();
        for (int from = 0; from < start.Length; from++)
        {
            int n = start[from];
            if (n == 0 || from == SurvivorLocation) continue;
            var path = Board.Path(from, SurvivorLocation, null, KillerCanPass);
            if (path is not { Count: > 0 }) continue;
            int to = path[0];
            if (to == blocked) continue; // a barred door keeps them out too
            _swarm[from] -= n;
            _swarm[to] += n;
            Emit(TideEventKind.SwarmMoved, null, from: from, to: to, amount: n);
        }

        // Then she spawns more where she stands, but only on cards where she attacks (and every turn of the finale).
        // New ones don't move until next time.
        int spawn = cardAttacks ? Math.Min(Scenario.SwarmSpawn + (finale ? 1 : 0), Scenario.SwarmMax - SwarmTotal) : 0;
        if (spawn > 0)
        {
            _swarm[TidewifeLocation] += spawn;
            Emit(TideEventKind.SwarmSpawned, $"{Plural(spawn, one, many)} {(spawn == 1 ? "crawls" : "crawl")} out at the {Name(TidewifeLocation)}.",
                to: TidewifeLocation, amount: spawn);
        }

        int here = _swarm[SurvivorLocation];
        if (here == 0) return;
        if (here == 1)
        {
            Emit(TideEventKind.SwarmBit, $"A lone {one} hisses at you from under a car, but won't come at you on its own.", to: SurvivorLocation, amount: 0);
            return;
        }
        if (Repelled)
        {
            Emit(TideEventKind.SwarmBit, $"The {many} circle you, but won't come close.", to: SurvivorLocation, amount: 0);
            return;
        }
        int damage = Math.Max(0, SwarmDamage(here) - (HoldingBreath ? 1 : 0));
        if (damage == 0)
        {
            Emit(TideEventKind.SwarmBit, $"The {many} {Scenario.SwarmBite}, but you keep still and they lose interest.", to: SurvivorLocation, amount: 0);
            return;
        }
        Health = Math.Max(0, Health - damage);
        string who = $"{here} {many} {Scenario.SwarmBite}";
        Emit(TideEventKind.SwarmBit, $"{who} for {damage}. Health {Health}.", to: SurvivorLocation, amount: damage);
        if (Health == 0)
        {
            Phase = Phase.Lost;
            Emit(TideEventKind.SurvivorDied, Scenario.LoseText, to: SurvivorLocation);
        }
    }

    // ------------------------------------------------------------------ events

    private static readonly ItemKind[] SupplyItems =
        [ItemKind.Bandages, ItemKind.FlareGun, ItemKind.BlessedSalt, ItemKind.IronNails, ItemKind.Lantern, ItemKind.GuttingKnife];

    /// <summary>Reveal the next event card and apply it. If it can't be applied it is simply ignored (no redraw).</summary>
    private void RevealEvent()
    {
        if (_eventDeck.Count == 0) return;
        var card = _eventDeck[^1];
        _eventDeck.RemoveAt(_eventDeck.Count - 1);
        int where = -1;
        string? result = null; // null = could not be applied

        switch (card.Kind)
        {
            case EventKind.Companion:
            case EventKind.Wounded:
            {
                if (Special is not null || InFinale) break;
                int best = -1, bestDist = int.MaxValue;
                for (int i = 0; i < Board.Count; i++)
                {
                    if (_villagers[i] == 0) continue;
                    int d = Board.Distance(SurvivorLocation, i, null, CanPass);
                    if (d < bestDist) { best = i; bestDist = d; }
                }
                if (best < 0) break;
                where = best;
                _activeEvents.Add(new ActiveEvent { Card = card, Location = best });
                result = card.Kind == EventKind.Companion
                    ? $"A {Scenario.Victim} at the {Name(best)} sticks close to you. Get them to the {Scenario.SanctuaryName}."
                    : $"A {Scenario.Victim} at the {Name(best)} is badly hurt. Escorting them takes two escorts at once.";
                break;
            }
            case EventKind.SecondWind:
            {
                int before = Health;
                Health = Math.Min(SurvivorMaxHealth, Health + 2);
                where = SurvivorLocation;
                result = $"You heal {Health - before}.";
                break;
            }
            case EventKind.Supplies:
            {
                var item = SupplyItems[_rng.Next(SupplyItems.Length)];
                _inventory.Add(item);
                where = SurvivorLocation;
                result = $"You gain {Items.All[item].Name}.";
                break;
            }
            case EventKind.Resolve:
                Dread = Math.Max(0, Dread - 1);
                Hope++;
                where = SurvivorLocation;
                result = $"Dread falls to {Dread}. +1 Hope.";
                break;
            case EventKind.Adrenaline:
            case EventKind.Blackout:
                _activeEvents.Add(new ActiveEvent { Card = card, RoundsLeft = EventCards.TimedRounds });
                result = card.Kind == EventKind.Adrenaline
                    ? $"+1 Time at the start of each of the next {EventCards.TimedRounds} rounds."
                    : $"Searches roll 1 fewer die for the next {EventCards.TimedRounds} rounds.";
                break;
            case EventKind.Trap:
                _activeEvents.Add(new ActiveEvent { Card = card, Location = SurvivorLocation });
                where = SurvivorLocation;
                result = $"A trap is set at the {Name(SurvivorLocation)}.";
                break;
            case EventKind.MoreVictims:
            {
                if (InFinale) break;
                int far = -1, farDist = -1;
                for (int i = 0; i < Board.Count; i++)
                {
                    if (Board.Locations[i].IsSanctuary || i == TidewifeLocation) continue;
                    int d = Board.Distance(TidewifeLocation, i, null, KillerCanPass);
                    if (d != int.MaxValue && d > farDist) { far = i; farDist = d; }
                }
                if (far < 0) break;
                _villagers[far] += 2;
                where = far;
                result = $"2 {Scenario.Victims} turn up at the {Name(far)}.";
                break;
            }
            case EventKind.BadOmen:
                Dread = Math.Min(MaxDread, Dread + 1);
                where = SurvivorLocation;
                result = $"Dread rises to {Dread}.";
                break;
            case EventKind.Hunted:
                _activeEvents.Add(new ActiveEvent { Card = card });
                where = TidewifeLocation;
                result = $"{Scenario.Killer.She} moves 1 extra space until you wound {Scenario.Killer.Her}.";
                break;
            case EventKind.Bloodscent:
                where = TidewifeLocation;
                result = $"{Scenario.Killer.Name}'s Bloodlust rises.";
                break;
        }

        string text = result is null
            ? $"Event: {card.Name}. It can't take effect right now, so it is ignored."
            : $"Event: {card.Name}. {result}";
        LastEvent = new EventReveal(card, result is not null, text, where);
        Emit(TideEventKind.EventRevealed, text, to: where, amount: IndexOfEvent(card));
        if (card.Kind == EventKind.Bloodscent) RaiseBloodlust(1);
    }

    private int IndexOfEvent(EventCardDef card)
    {
        for (int i = 0; i < Scenario.EventCards.Count; i++)
            if (ReferenceEquals(Scenario.EventCards[i], card)) return i;
        return -1;
    }

    /// <summary>At the start of each round: timed events give their effect, count down, and are discarded when done.</summary>
    private void TickEvents()
    {
        foreach (var e in _activeEvents.Where(e => e.Card.Kind is EventKind.Adrenaline or EventKind.Blackout).ToList())
        {
            if (e.RoundsLeft <= 0)
            {
                _activeEvents.Remove(e);
                Write(LogKind.Info, $"{e.Card.Name} is discarded.");
                continue;
            }
            if (e.Card.Kind == EventKind.Adrenaline)
            {
                Time = Math.Min(MaxTime, Time + 1);
                Write(LogKind.Good, $"{e.Card.Name}: +1 Time.");
            }
            e.RoundsLeft--;
        }
    }

    private void SpecialSaved()
    {
        if (Special is not { } sp) return;
        _activeEvents.Remove(sp);
        if (sp.Card.Kind == EventKind.Companion)
        {
            Hope += 2;
            Write(LogKind.Good, $"{sp.Card.Name}: they made it! +2 extra Hope.");
        }
        else
        {
            Hope += 1;
            Health = Math.Min(SurvivorMaxHealth, Health + 2);
            Write(LogKind.Good, $"{sp.Card.Name}: they made it! Heal 2 and +1 extra Hope.");
        }
    }

    private void SpecialKilled(bool inYourSpace)
    {
        if (Special is not { } sp) return;
        _activeEvents.Remove(sp);
        if (sp.Card.Kind == EventKind.Companion)
        {
            if (inYourSpace)
            {
                _timeModNext += 3;
                Emit(TideEventKind.EventNote, $"{sp.Card.Name}: they die in front of you. Rage: +3 Time next round.", to: TidewifeLocation, amount: 3);
            }
            else
            {
                Dread = Math.Min(MaxDread, Dread + 1);
                Emit(TideEventKind.EventNote, $"{sp.Card.Name}: they're gone. Dread +1.", to: TidewifeLocation, amount: 1);
            }
        }
        else
        {
            Emit(TideEventKind.EventNote, $"{sp.Card.Name}: they didn't make it.", to: TidewifeLocation);
        }
    }

    /// <summary>The killer attacks you for a flat amount from the killer board. Returns damage taken.</summary>
    private int StrikeSurvivor(int damage)
    {
        if (Repelled)
        {
            Emit(TideEventKind.Repelled, Scenario.RepelText, to: SurvivorLocation);
            return 0;
        }
        bool breathSaved = HoldingBreath;
        int hits = Math.Max(0, damage - (breathSaved ? 1 : 0));
        if (hits > 0)
        {
            Health = Math.Max(0, Health - hits);
            Emit(TideEventKind.Struck, $"{Scenario.Killer.She} attacks you for {hits}. Health {Health}."
                + (breathSaved ? " Holding your breath saved you from 1 damage." : ""),
                to: SurvivorLocation, amount: hits);
        }
        else
        {
            Emit(TideEventKind.Missed, $"{Scenario.Killer.She} lunges, but you held your breath and twisted away.", to: SurvivorLocation);
        }
        if (Health == 0)
        {
            Phase = Phase.Lost;
            Emit(TideEventKind.SurvivorDied, Scenario.LoseText, to: SurvivorLocation);
        }
        return hits;
    }

    /// <summary>The killer attacks the victims in her space: one dies per point of damage. Each death raises Bloodlust.</summary>
    private void KillVictims(int damage)
    {
        int here = _villagers[TidewifeLocation];
        int taken = Math.Min(damage, here);
        if (taken == 0) return;
        bool specialDies = SpecialVictimAt == TidewifeLocation && _rng.Next(here) < taken;
        _villagers[TidewifeLocation] -= taken;
        VillagersLost += taken;
        Emit(TideEventKind.Claimed,
            $"{Scenario.Killer.She} {string.Format(Scenario.ClaimText, Plural(taken, Scenario.Victim, Scenario.Victims))} at the {Name(TidewifeLocation)}.",
            to: TidewifeLocation, amount: taken);
        if (specialDies) SpecialKilled(inYourSpace: TidewifeHere);
        RaiseBloodlust(taken);
    }

    /// <summary>Move the Bloodlust marker up, firing each level's special effect. Past the top, the final effect repeats.</summary>
    private void RaiseBloodlust(int levels)
    {
        for (int i = 0; i < levels; i++)
        {
            if (Bloodlust >= BloodlustMax)
            {
                TidewifeMaxHealth = Math.Max(TidewifeMaxHealth, TidewifeHealth + Core.Bloodlust.FinalEffectHeal);
                TidewifeHealth += Core.Bloodlust.FinalEffectHeal;
                Emit(TideEventKind.Bloodlust, $"Bloodlust is full: {Scenario.Killer.SheLower} recovers {Core.Bloodlust.FinalEffectHeal} health.",
                    to: TidewifeLocation, amount: Bloodlust);
                continue;
            }
            Bloodlust++;
            var level = KillerLevel;
            switch (level.Effect)
            {
                case BloodlustEffect.DreadUp:
                    Dread = Math.Min(MaxDread, Dread + 1);
                    break;
                case BloodlustEffect.Frenzy:
                    TidewifeHealth = Math.Min(TidewifeMaxHealth, TidewifeHealth + 2);
                    break;
            }
            string fx = Core.Bloodlust.Describe(level.Effect, Scenario);
            Emit(TideEventKind.Bloodlust,
                $"Bloodlust {Bloodlust}: {Scenario.Killer.SheLower} now moves {level.Move} and attacks for {level.Attack}." + (fx.Length > 0 ? " " + fx : ""),
                to: TidewifeLocation, amount: Bloodlust);
        }
    }

    private int VillagerTarget(int? blocked)
    {
        int best = -1, bestDist = int.MaxValue;
        for (int i = 0; i < Board.Count; i++)
        {
            if (_villagers[i] == 0) continue;
            int d = Board.Distance(TidewifeLocation, i, blocked, KillerCanPass);
            if (d == int.MaxValue) continue;
            if (d < bestDist || (d == bestDist && _villagers[i] > _villagers[best]))
            {
                best = i;
                bestDist = d;
            }
        }
        return best >= 0 ? best : SurvivorLocation;
    }

    private void MoveToward(int target, int steps, bool huntingSurvivor, int? blocked)
    {
        int start = TidewifeLocation;
        for (int s = 0; s < steps; s++)
        {
            if (TidewifeLocation == SurvivorLocation) break;
            if (!huntingSurvivor && _villagers[TidewifeLocation] > 0) break;
            var path = Board.Path(TidewifeLocation, target, blocked, KillerCanPass);
            if (path is null || path.Count == 0)
            {
                if (blocked.HasValue && Board.AreAdjacent(TidewifeLocation, blocked.Value))
                    Emit(TideEventKind.Blocked, Scenario.BarredDoor, TidewifeLocation, blocked.Value);
                break;
            }
            int from = TidewifeLocation;
            TidewifeLocation = path[0];
            Emit(TideEventKind.Moved, null, from, TidewifeLocation);
        }
        Emit(TideEventKind.Narration, TidewifeLocation != start
            ? $"{Scenario.Killer.She} {Scenario.MoveVerb} to the {Name(TidewifeLocation)}."
            : $"{Scenario.Killer.She} {Scenario.LingerVerb} at the {Name(TidewifeLocation)}.");
    }

    private void Teleport(int to, int? blocked)
    {
        if (to == blocked)
        {
            Emit(TideEventKind.Blocked, Scenario.BarredRise, TidewifeLocation, to);
            return;
        }
        int from = TidewifeLocation;
        TidewifeLocation = to;
        Emit(TideEventKind.Rose, $"{Scenario.Killer.She} {Scenario.RiseText} at the {Name(to)}.", from, to);
    }

    private void ApplyEffect(TideEffect effect)
    {
        switch (effect)
        {
            case TideEffect.None:
                return;
            case TideEffect.Fog:
                FogNextTurn = true;
                break;
            case TideEffect.Straggler:
            {
                var options = Board.Locations.Where(l => !l.IsSanctuary && l.Id != TidewifeLocation).Select(l => l.Id).ToList();
                int at = options[_rng.Next(options.Count)];
                _villagers[at]++;
                Emit(TideEventKind.Straggler, $"A straggler appears at the {Name(at)}.", to: at, amount: 1);
                return;
            }
            case TideEffect.DreadUp:
                Dread = Math.Min(MaxDread, Dread + 1);
                break;
            case TideEffect.LowWater:
                _timeModNext += 1;
                break;
            case TideEffect.ColdBreath:
                _timeModNext -= 1;
                break;
            case TideEffect.Mend:
                TidewifeHealth = Math.Min(TidewifeMaxHealth, TidewifeHealth + 1);
                break;
        }
        Emit(TideEventKind.Effect, TideCards.DescribeEffect(effect), to: TidewifeLocation);
    }

    private void BeginFinale()
    {
        InFinale = true;
        int lost = VillagersRemaining;
        Array.Clear(_villagers);
        if (Special is { } sp)
        {
            _activeEvents.Remove(sp);
            Write(LogKind.Bad, $"{sp.Card.Name} is discarded: they are lost with the others.");
        }
        VillagersLost += lost;
        TidewifeMaxHealth += FinaleHealthBonus;
        TidewifeHealth += FinaleHealthBonus;
        Emit(TideEventKind.FinaleBegins, lost > 0
            ? $"{Scenario.FinaleName.ToUpperInvariant()}. {Scenario.FinaleTakes} and takes the last {Plural(lost, Scenario.Victim, Scenario.Victims)}."
            : $"{Scenario.FinaleName.ToUpperInvariant()}. {Scenario.FinaleTakes}.", amount: lost);
        Emit(TideEventKind.Narration, $"{Scenario.Killer.Name} swells to full strength ({TidewifeHealth} health) and hunts only you. Fight or die.");
    }
}
