namespace SoleSurvivor.Core;

public enum Phase { SurvivorTurn, Won, Lost }

public enum Difficulty { Easy, Normal, Hard }

public enum LogKind { Info, Good, Bad, Tide, System }

public sealed record LogEntry(int Round, LogKind Kind, string Text);

public enum DieFace { Blank, Partial, Success }

/// <summary>A Horror roll. Faces: 1-2 blank, 3-4 partial (the cards icon), 5-6 success (the star).
/// Converted = how many partials were turned into successes by discarding 2 cards each.</summary>
public sealed record DiceRoll(string Label, IReadOnlyList<int> Values, int Threshold, int Successes, int Converted = 0)
{
    public static DieFace Face(int value) => value >= 5 ? DieFace.Success : value >= 3 ? DieFace.Partial : DieFace.Blank;
    public int Stars => Values.Count(v => Face(v) == DieFace.Success);
    public int Partials => Values.Count(v => Face(v) == DieFace.Partial);
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
    FinaleBegins
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
    public const int MaxTime = 8;
    public const int StartTime = 5;
    public const int MaxDread = 8;

    public const int FinaleHealthBonus = 3;
    public const int HandLimit = 4;
    public const int MarketSize = 5;
    /// <summary>Time gained by discarding a card from your hand.</summary>
    public const int DiscardRefund = 1;
    /// <summary>In the finale the killer hunts harder than the board says: +1 move and +1 damage.</summary>
    public const int FinaleMoveBonus = 1, FinaleDamageBonus = 1;
    public const int StartingPicks = 3;

    public const int CostMove = 1, CostEscort = 1, CostSearch = 2, CostFight = 2, CostRest = 2;

    private readonly Random _rng;

    private readonly int[] _villagers = new int[Board.Count];
    private readonly List<ItemKind>[] _stashes = new List<ItemKind>[Board.Count];
    private readonly List<ItemKind> _inventory = new();
    private readonly List<ActionCardDef> _actionDeck = new();
    private readonly List<ActionCardDef> _actionDiscard = new();
    private readonly List<ActionCardDef> _market = new();
    private readonly List<ActionCardDef> _hand = new();
    private readonly List<TideCardDef> _tideDeck = new();
    private readonly List<EventCardDef> _eventDeck = new();
    private readonly List<ActiveEvent> _activeEvents = new();
    private readonly List<LogEntry> _log = new();
    private int _timeModNext;
    private readonly List<ActionCardDef> _startingOffer = new(ActionCards.Starters);

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
    public int Time { get; private set; } = StartTime;
    public int Hope { get; private set; } = 1;
    public int Dread { get; private set; }
    public int TidewifeHealth { get; private set; }
    public int TidewifeMaxHealth { get; private set; }

    public int VillagersSaved { get; private set; }
    public int VillagersLost { get; private set; }

    public int MoveTokens { get; private set; }
    public int EscortTokens { get; private set; }

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
    public IReadOnlyList<ActionCardDef> Market => _market;
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

    /// <summary>True until the player has chosen their free starting cards. No other action is allowed until then.</summary>
    public bool AwaitingStartPick { get; private set; } = true;
    public IReadOnlyList<ActionCardDef> StartingOffer => _startingOffer;
    public int StashCount(int location) => _stashes[location].Count;
    public int VillagersRemaining => _villagers.Sum();

    public bool IsOver => Phase != Phase.SurvivorTurn;
    public bool TidewifeHere => TidewifeLocation == SurvivorLocation;
    public bool TidewifeNear => TidewifeHere || Board.AreAdjacent(TidewifeLocation, SurvivorLocation);

    /// <summary>A star (5 or 6) is a success. A 3 or 4 is a partial: discard 2 cards from your hand to make it a success.</summary>
    public int SuccessThreshold => 5;
    public const int CardsPerConversion = 2;

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
            Difficulty.Easy => (5, 6, 14),
            Difficulty.Hard => (4, 5, 17),
            _ => (5, 5, 16)
        };
        TidewifeStartHealth += Scenario.Killer.HealthBonus;
        Health = SurvivorMaxHealth;
        TidewifeHealth = TidewifeMaxHealth = TidewifeStartHealth;

        foreach (var loc in Board.Locations)
        {
            _villagers[loc.Id] = loc.StartVillagers;
            _stashes[loc.Id] = Shuffle(loc.Stash.ToList());
        }

        foreach (var card in ActionCards.All)
            for (int i = 0; i < card.Copies; i++)
                _actionDeck.Add(card);
        Shuffle(_actionDeck);

        _tideDeck.AddRange(TideCards.All);
        Shuffle(_tideDeck);

        _eventDeck.AddRange(Scenario.EventCards);
        Shuffle(_eventDeck);

        Write(LogKind.System, Scenario.Intro);
        RefillMarket();
    }

    /// <summary>Take exactly <see cref="StartingPicks"/> of the free starting cards into your hand.
    /// Starting cards are single-use: once played (or left behind) they are gone.</summary>
    public bool ChooseStartingCards(IReadOnlyCollection<int> indices)
    {
        if (!AwaitingStartPick || Phase != Phase.SurvivorTurn) return false;
        var picked = indices.Distinct().ToList();
        if (picked.Count != StartingPicks || picked.Any(i => i < 0 || i >= _startingOffer.Count)) return false;

        foreach (int i in picked.OrderBy(i => i)) _hand.Add(_startingOffer[i]);
        AwaitingStartPick = false;
        Write(LogKind.Info, "You set out with " + string.Join(", ", picked.OrderBy(i => i).Select(i => _startingOffer[i].Name)) + ".");
        // The opening event: one is always revealed at the start of the night.
        RevealEvent();
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

    private bool Acting => Phase == Phase.SurvivorTurn && !AwaitingStartPick && Pending is null;

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

    private ActionCardDef? DrawActionCard()
    {
        if (_actionDeck.Count == 0)
        {
            if (_actionDiscard.Count == 0) return null;
            _actionDeck.AddRange(_actionDiscard);
            _actionDiscard.Clear();
            Shuffle(_actionDeck);
        }
        var card = _actionDeck[^1];
        _actionDeck.RemoveAt(_actionDeck.Count - 1);
        return card;
    }

    private void RefillMarket()
    {
        _actionDiscard.AddRange(_market);
        _market.Clear();
        for (int i = 0; i < MarketSize; i++)
        {
            var card = DrawActionCard();
            if (card is null) break;
            _market.Add(card);
        }
    }

    // ------------------------------------------------------------------ movement

    public bool CanMove(int to) =>
        Acting && Board.AreAdjacent(SurvivorLocation, to) && (MoveTokens > 0 || Time >= CostMove);

    public bool Move(int to)
    {
        if (!CanMove(to)) return false;
        if (MoveTokens > 0) MoveTokens--; else Spend(CostMove);
        SurvivorLocation = to;
        Write(LogKind.Info, $"You walk to the {Name(to)}.");
        return true;
    }

    /// <summary>Escorting costs 1, or 2 if the badly hurt special victim is with you.</summary>
    public int EscortCost => CostEscort + (SpecialVictimAt == SurvivorLocation && SpecialVictimKind == EventKind.Wounded ? 1 : 0);

    public bool CanEscort(int to) =>
        Acting && !InFinale && Board.AreAdjacent(SurvivorLocation, to) && _villagers[SurvivorLocation] > 0
        && (EscortTokens > 0 || Time >= EscortCost);

    public bool Escort(int to)
    {
        if (!CanEscort(to)) return false;
        if (EscortTokens > 0) EscortTokens--; else Spend(EscortCost);
        int from = SurvivorLocation;
        int count = Math.Min(EscortCapacity, _villagers[from]);
        bool special = SpecialVictimAt == from; // the special victim always comes with you
        _villagers[from] -= count;
        SurvivorLocation = to;
        Write(LogKind.Info, $"You lead {Plural(count, Scenario.Victim, Scenario.Victims)} to the {Name(to)}.");
        ArriveVillagers(to, count, special);
        return true;
    }

    // ------------------------------------------------------------------ basic actions

    public bool CanSearch => Acting && Time >= CostSearch && _stashes[SurvivorLocation].Count > 0;

    public bool Search()
    {
        if (!CanSearch) return false;
        Spend(CostSearch);
        DoSearch(SearchDice(), "Search");
        return true;
    }

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

    public bool CanFight => Acting && TidewifeHere && Time >= CostFight;

    public bool Fight()
    {
        if (!CanFight) return false;
        Spend(CostFight);
        DoFight(FightDice(), "Fight");
        return true;
    }

    private void DoFight(int dice, string label)
    {
        var roll = Roll(label, dice, SuccessThreshold);
        Offer(roll, TidewifeHealth, "fight", successes => WoundTidewife(successes, label));
    }

    /// <summary>Resolve a roll now, or, if partials could be converted to something useful, wait for the player.</summary>
    private void Offer(DiceRoll roll, int usefulSuccesses, string purpose, Action<int> resolve)
    {
        int max = Math.Min(Math.Min(roll.Partials, _hand.Count / CardsPerConversion), Math.Max(0, usefulSuccesses - roll.Successes));
        if (max <= 0)
        {
            resolve(roll.Successes);
            return;
        }
        Pending = new PendingRoll(roll, max, purpose);
        _resolvePending = resolve;
        Write(LogKind.Info, $"{roll.Label}: {Plural(roll.Partials, "partial", "partials")} (3 or 4). Discard {CardsPerConversion} cards to turn each into a success.");
    }

    /// <summary>Convert partials by discarding cards from your hand (2 per partial), then resolve the roll.
    /// Pass no cards to keep the roll as it is. Discarded starting cards are gone; others go to the discard pile.</summary>
    public bool ConvertPartials(IReadOnlyCollection<int> handIndices)
    {
        if (Pending is not { } pending || _resolvePending is null) return false;
        var picks = handIndices.Distinct().ToList();
        if (picks.Count % CardsPerConversion != 0 || picks.Count / CardsPerConversion > pending.MaxConversions
            || picks.Any(i => i < 0 || i >= _hand.Count)) return false;

        int converted = picks.Count / CardsPerConversion;
        var names = new List<string>();
        foreach (int i in picks.OrderByDescending(i => i))
        {
            var card = _hand[i];
            _hand.RemoveAt(i);
            if (!ActionCards.IsStarter(card.Effect)) _actionDiscard.Add(card);
            names.Add(card.Name);
        }
        var roll = pending.Roll with { Successes = pending.Roll.Successes + converted, Converted = converted };
        LastRoll = roll;
        var resolve = _resolvePending;
        Pending = null;
        _resolvePending = null;
        if (converted > 0)
            Write(LogKind.Good, $"You discard {string.Join(", ", names)}: {Plural(converted, "partial becomes a success", "partials become successes")}.");
        resolve(roll.Successes);
        return true;
    }

    public bool CanRest => Acting && Time >= CostRest && Health < SurvivorMaxHealth && !TidewifeHere;

    public bool Rest()
    {
        if (!CanRest) return false;
        Spend(CostRest);
        Heal(1, "You rest");
        return true;
    }

    // ------------------------------------------------------------------ cards

    public bool CanBuy(int marketIndex) =>
        Acting && marketIndex >= 0 && marketIndex < _market.Count && _hand.Count < HandLimit && Time >= _market[marketIndex].Cost;

    public bool Buy(int marketIndex)
    {
        if (!CanBuy(marketIndex)) return false;
        var card = _market[marketIndex];
        Spend(card.Cost);
        _market.RemoveAt(marketIndex);
        _hand.Add(card);
        Write(LogKind.Info, $"You take {card.Name} into your hand.");
        return true;
    }

    /// <summary>A card in hand can be thrown away for +1 Time, as long as that Time isn't wasted at the cap.</summary>
    public bool CanDiscard(int handIndex) =>
        Acting && handIndex >= 0 && handIndex < _hand.Count && Time < MaxTime;

    public bool Discard(int handIndex)
    {
        if (!CanDiscard(handIndex)) return false;
        var card = _hand[handIndex];
        _hand.RemoveAt(handIndex);
        if (!ActionCards.IsStarter(card.Effect)) _actionDiscard.Add(card); // starting cards don't recycle
        Time = Math.Min(MaxTime, Time + DiscardRefund);
        Write(LogKind.Info, $"You discard {card.Name} for +{DiscardRefund} Time.");
        return true;
    }

    public bool CanPlay(int handIndex)
    {
        if (!Acting || handIndex < 0 || handIndex >= _hand.Count) return false;
        return _hand[handIndex].Effect switch
        {
            CardEffect.WildSwing or CardEffect.DesperatePlan => TidewifeHere,
            CardEffect.Rummage => _stashes[SurvivorLocation].Count > 0,
            CardEffect.BarTheDoor => !TidewifeHere && !Barricaded,
            CardEffect.HoldBreath => !HoldingBreath,
            CardEffect.LanternSignal => !InFinale && Board.Neighbours(SurvivorLocation).Any(n => _villagers[n] > 0),
            CardEffect.PatchUp => Health < SurvivorMaxHealth,
            CardEffect.Lure => !TidewifeHere,
            CardEffect.Shepherd => !InFinale,
            CardEffect.Guide or CardEffect.Holler => !InFinale,
            CardEffect.Peek => _stashes[SurvivorLocation].Count > 0,
            CardEffect.Shove => TidewifeHere,
            CardEffect.GlanceAround => _stashes[SurvivorLocation].Count > 0,
            CardEffect.CatchBreath => Health < SurvivorMaxHealth && !TidewifeHere,
            _ => true
        };
    }

    public bool Play(int handIndex)
    {
        if (!CanPlay(handIndex)) return false;
        var card = _hand[handIndex];
        _hand.RemoveAt(handIndex);
        if (!ActionCards.IsStarter(card.Effect)) _actionDiscard.Add(card); // starting cards don't recycle

        switch (card.Effect)
        {
            case CardEffect.Sprint:
                MoveTokens += 3;
                Write(LogKind.Info, "Sprint: 3 free walks.");
                break;
            case CardEffect.Shepherd:
                EscortTokens += 2;
                Write(LogKind.Info, "Shepherd: 2 free escorts.");
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
                Write(LogKind.Info, $"{card.Name}: 1 free walk.");
                break;
            case CardEffect.Guide:
            case CardEffect.Holler:
                EscortTokens += 1;
                Write(LogKind.Info, $"{card.Name}: 1 free escort.");
                break;
            case CardEffect.Shove:
                DoFight(FightDice(-1), card.Name);
                break;
            case CardEffect.GlanceAround:
            case CardEffect.Peek:
                DoSearch(SearchDice(-1), card.Name);
                break;
            case CardEffect.CatchBreath:
                Heal(1, "Catch Your Breath");
                break;
            case CardEffect.Lure:
            {
                var path = Board.Path(TidewifeLocation, SurvivorLocation);
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

    public bool CanUse(int inventoryIndex)
    {
        if (!Acting || inventoryIndex < 0 || inventoryIndex >= _inventory.Count) return false;
        return _inventory[inventoryIndex] switch
        {
            ItemKind.FlareGun => TidewifeNear,
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
                WoundTidewife(2, Items.All[ItemKind.FlareGun].Name);
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

    public bool CanEndTurn => Acting;

    public void EndTurn()
    {
        if (!Acting) return;
        ResolveTide();
        if (Phase == Phase.SurvivorTurn) StartRound();
    }

    private void StartRound()
    {
        Round++;
        Time = Math.Clamp(Time + TimePerRound + _timeModNext, 0, MaxTime);
        _timeModNext = 0;
        FogThisTurn = FogNextTurn;
        FogNextTurn = false;
        MoveTokens = 0;
        EscortTokens = 0;
        SteelYourself = false;
        TickEvents();
        RefillMarket();
        Write(LogKind.System, $"Round {Round}. You have {Time} Time.");
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
                    int d = Board.Distance(SurvivorLocation, i, null);
                    if (d < bestDist) { best = i; bestDist = d; }
                }
                if (best < 0) break;
                where = best;
                _activeEvents.Add(new ActiveEvent { Card = card, Location = best });
                result = card.Kind == EventKind.Companion
                    ? $"A {Scenario.Victim} at the {Name(best)} sticks close to you. Get them to the {Scenario.SanctuaryName}."
                    : $"A {Scenario.Victim} at the {Name(best)} is badly hurt. Escorting them costs 1 extra Time.";
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
                    int d = Board.Distance(TidewifeLocation, i, null);
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
            int d = Board.Distance(TidewifeLocation, i, blocked);
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
            var path = Board.Path(TidewifeLocation, target, blocked);
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
