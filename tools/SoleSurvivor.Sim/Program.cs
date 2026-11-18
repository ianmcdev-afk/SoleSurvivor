using SoleSurvivor.Core;

// Headless harness: plays many games with a heuristic player and a random-legal-move fuzzer,
// checks invariants, and prints balance numbers. Usage: SoleSurvivor.Sim [games]

if (args.Length > 1 && args[0] == "dump") { Dump.One(int.Parse(args[1]), PlayHeuristicTurn, args.Length > 2 ? args[2] : null); return; }
int games = args.Length > 0 ? int.Parse(args[0]) : 2000;

var only = args.Length > 1 ? args[1] : null;
foreach (var scenario in Scenarios.All)
{
    if (only is not null && scenario.Id != only) continue;
    Console.WriteLine($"== {scenario.Title} ({scenario.Id})");
    foreach (var d in Enum.GetValues<Difficulty>())
        Run($"Heur/{d}", games, PlayHeuristicTurn, d, scenario);
    Run("Random", games, PlayRandomTurn, Difficulty.Normal, scenario);
}

static void Run(string name, int games, Action<Game, Random> playTurn, Difficulty difficulty, ScenarioDef scenario)
{
    int wins = 0, rounds = 0, saved = 0, lost = 0, finales = 0, finaleWins = 0, errors = 0;
    for (int seed = 0; seed < games; seed++)
    {
        var g = new Game(seed, difficulty, scenario); BotState.BoughtRound = -1;
        var ai = new Random(seed * 7919 + 1);
        try
        {
            if (g.CanEndTurn || g.CanSearch) throw new Exception("actions allowed before starting pick");
            if (g.ChooseStartingCards(new[] { 0, 0, 1 })) throw new Exception("duplicate pick accepted");
            if (!g.ChooseStartingCards(name == "Random" ? new[] { 1, 3, 4 } : new[] { 0, 1, 2 })) throw new Exception("starting pick refused");
            if (g.Hand.Count != Game.StartingPicks) throw new Exception("starting hand size");
            int guard = 0;
            while (!g.IsOver && guard++ < 200)
            {
                playTurn(g, ai);
                if (g.Pending is not null)
                {
                    if (g.CanEndTurn || g.CanSearch) throw new Exception("actions allowed while a roll is pending");
                    if (g.ConvertPartials(Array.Empty<int>(), new[] { 0 })) throw new Exception("cards without dice accepted");
                    var p0 = g.Pending;
                    int two = Enumerable.Range(0, p0.Roll.Values.Count).FirstOrDefault(i => p0.Roll.Values[i] == 3, -1);
                    if (two >= 0 && g.Hand.Count >= 1 && g.ConvertPartials(new[] { two }, new[] { 0 })) throw new Exception("a 3 converted with one card");
                    g.ConvertPartials(Array.Empty<int>(), Array.Empty<int>());
                }
                Check(g);
                if (!g.IsOver)
                {
                    int her = g.TidewifeLocation;
                    g.EndTurn();
                    // The event stream must replay her movement exactly (the UI animates from it).
                    foreach (var ev in g.LastTide!.Events)
                    {
                        if (ev.Kind is not (TideEventKind.Moved or TideEventKind.Rose)) continue;
                        if (ev.From != her) throw new Exception($"event chain broken: {ev.Kind} from {ev.From}, expected {her}");
                        if (ev.Kind == TideEventKind.Moved && !Board.AreAdjacent(ev.From, ev.To)) throw new Exception("non-adjacent step");
                        if (ev.Kind == TideEventKind.Moved && g.IsLocked(ev.From, ev.To)) throw new Exception("killer walked through a locked door");
                        her = ev.To;
                    }
                    if (her != g.TidewifeLocation) throw new Exception("events don't end where she is");
                }
                Check(g);
            }
            if (!g.IsOver) throw new Exception("game did not finish in 200 rounds");
        }
        catch (Exception ex)
        {
            if (errors++ < 3) Console.WriteLine($"  [{name}] seed {seed}: {ex.Message}");
        }
        if (g.Phase == Phase.Won) wins++;
        if (g.InFinale) { finales++; if (g.Phase == Phase.Won) finaleWins++; }
        rounds += g.Round;
        saved += g.VillagersSaved; lost += g.VillagersLost;
    }
    Console.WriteLine($"{name,-12} win {100.0 * wins / games,5:F1}%  avg rounds {(double)rounds / games,5:F1}  avg saved {(double)saved / games,4:F1}  avg lost {(double)lost / games,4:F1}  reached finale {100.0 * finales / games,5:F1}% (won {(finales == 0 ? 0 : 100.0 * finaleWins / finales):F1}% of those)  errors {errors}");
}

static void Check(Game g)
{
    if (g.Health < 0 || g.Health > g.SurvivorMaxHealth) throw new Exception($"health {g.Health}");
    if (g.Time < 0 || g.Time > Game.MaxTime) throw new Exception($"time {g.Time}");
    if (g.Hope < 0) throw new Exception("hope");
    if (g.Dread < 0 || g.Dread > Game.MaxDread) throw new Exception("dread");
    if (g.SpecialVictimAt >= 0 && g.Villagers[g.SpecialVictimAt] == 0) throw new Exception("special victim on an empty location");
    for (int i = 0; i < g.Inventory.Count; i++)
        if (Items.All[g.Inventory[i]].Consumable && g.CanUse(i) != (g.UseBlockedReason(i) is null))
            throw new Exception($"use reason disagrees with CanUse for {g.Inventory[i]}");
    if (g.ActiveEvents.Any(e => e.RoundsLeft < 0)) throw new Exception("event timer");
    if (g.TidewifeHealth < 0) throw new Exception("tidewife hp");
    if (g.Villagers.Any(v => v < 0)) throw new Exception("villagers");
    if (g.Villagers[Board.Sanctuary] != 0) throw new Exception("villagers left standing in lighthouse");
    if (g.Hand.Count > Game.HandLimit) throw new Exception("hand");
    if (g.Phase == Phase.Won && g.TidewifeHealth != 0) throw new Exception("won without kill");
    if (g.Phase == Phase.Lost && g.Health != 0) throw new Exception("lost while alive");
}

static void PlayRandomTurn(Game g, Random r)
{
    for (int i = 0; i < 40 && !g.IsOver; i++)
    {
        if (g.Pending is { } p)
        {
            var dice = Enumerable.Range(0, p.Roll.Values.Count).Where(i => DiceRoll.CardsFor(p.Roll.Values[i]) > 0).OrderBy(_ => r.Next()).ToList();
            var chosen = new List<int>(); int need = 0;
            foreach (int d in dice)
            {
                int c = DiceRoll.CardsFor(p.Roll.Values[d]);
                if (chosen.Count >= p.MaxConversions || need + c > g.Hand.Count || r.Next(2) == 0) continue;
                chosen.Add(d); need += c;
            }
            var picks = Enumerable.Range(0, g.Hand.Count).OrderBy(_ => r.Next()).Take(need).ToList();
            if (!g.ConvertPartials(chosen, picks)) throw new Exception("conversion refused");
            continue;
        }
        var options = new List<Func<bool>>();
        foreach (int n in Board.Neighbours(g.SurvivorLocation))
        {
            int to = n;
            if (g.CanMove(to)) options.Add(() => g.Move(to));
            if (g.CanEscort(to)) options.Add(() => g.Escort(to));
        }
        if (g.CanSearch) options.Add(g.Search);
        if (g.CanFight) options.Add(g.Fight);
        if (g.CanRest) options.Add(g.Rest);
        for (int m = 0; m < g.Market.Count; m++) { int k = m; if (g.CanBuy(k)) options.Add(() => g.Buy(k)); }
        for (int h = 0; h < g.Hand.Count; h++) { int k = h; if (g.CanPlay(k)) options.Add(() => g.Play(k)); }
        for (int h = 0; h < g.Hand.Count; h++) { int k = h; if (g.CanDiscard(k)) options.Add(() => g.Discard(k)); }
        for (int it = 0; it < g.Inventory.Count; it++) { int k = it; if (g.CanUse(k)) options.Add(() => g.Use(k)); }
        if (options.Count == 0 || r.Next(6) == 0) return;
        if (r.Next(4) == 0) g.SteelYourself = true;
        if (!options[r.Next(options.Count)]()) throw new Exception("CanX said yes but X refused");
    }
}

static void PlayHeuristicTurn(Game g, Random r)
{
    for (int guard = 0; guard < 40 && !g.IsOver; guard++)
    {
        if (!Step(g)) return;
    }
}

static bool Step(Game g)
{
    int Find<T>(IReadOnlyList<T> list, Func<T, bool> pred)
    {
        for (int i = 0; i < list.Count; i++) if (pred(list[i])) return i;
        return -1;
    }
    bool PlayEffect(CardEffect e)
    {
        int i = Find(g.Hand, c => c.Effect == e);
        return i >= 0 && g.CanPlay(i) && g.Play(i);
    }
    bool UseItem(ItemKind k)
    {
        int i = Find(g.Inventory, x => x == k);
        return i >= 0 && g.CanUse(i) && g.Use(i);
    }

    // A roll with partials: trade the cheapest cards in. Always for a fight; for a search only if it found nothing.
    if (g.Pending is { } p)
    {
        int want = p.Purpose == "fight" ? p.MaxConversions : (p.Roll.Successes == 0 ? 1 : 0);
        // Cheapest dice first (the one-card 4s), paid with the cheapest cards.
        var dice = Enumerable.Range(0, p.Roll.Values.Count).Where(i => DiceRoll.CardsFor(p.Roll.Values[i]) > 0)
            .OrderBy(i => DiceRoll.CardsFor(p.Roll.Values[i])).ToList();
        var chosen = new List<int>(); int need = 0;
        foreach (int d in dice)
        {
            int c = DiceRoll.CardsFor(p.Roll.Values[d]);
            if (chosen.Count >= want || need + c > g.Hand.Count) break;
            chosen.Add(d); need += c;
        }
        var cheapest = Enumerable.Range(0, g.Hand.Count).OrderBy(i => g.Hand[i].Cost).Take(need).ToList();
        if (!g.ConvertPartials(chosen, cheapest)) throw new Exception("conversion refused");
        return true;
    }

    // Free value first.
    if (UseItem(ItemKind.FlareGun)) return true;
    if (g.TidewifeHere && (PlayEffect(CardEffect.DesperatePlan) || PlayEffect(CardEffect.WildSwing))) return true;
    if (g.Health <= 3 && (UseItem(ItemKind.Bandages) || PlayEffect(CardEffect.PatchUp))) return true;
    if (PlayEffect(CardEffect.SteelNerves)) return true;
    if (PlayEffect(CardEffect.Sprint) || PlayEffect(CardEffect.Shepherd)) return true;
    if (g.TidewifeHere && PlayEffect(CardEffect.Shove)) return true;
    if (g.Villagers[g.SurvivorLocation] > 0 && PlayEffect(CardEffect.Guide)) return true;
    if (g.Health <= 3 && PlayEffect(CardEffect.CatchBreath)) return true;
    if (PlayEffect(CardEffect.GlanceAround)) return true;
    if (PlayEffect(CardEffect.LanternSignal)) return true;

    bool strong = g.FightBonus >= 1 || g.InFinale;
    if (g.TidewifeHere)
    {
        if (g.Hope > 0 && g.CanFight) g.SteelYourself = true;
        if (strong && g.CanFight && (g.Health >= 3 || g.InFinale)) return g.Fight();
        if (UseItem(ItemKind.BlessedSalt)) return true;
    }

    if (g.Health <= 2 && g.CanRest) return g.Rest();

    // Buy if we have slack.
    // Short of Time? Turn cards it can't use right now into Time.
    if (g.Time < 2)
        for (int h = g.Hand.Count - 1; h >= 0; h--)
            if (!g.CanPlay(h) && g.CanDiscard(h)) return g.Discard(h);

    // Buy only cards it will actually use, and only with Time to spare.
    for (int m = 0; m < g.Market.Count; m++)
    {
        var e = g.Market[m].Effect;
        bool useful = e switch
        {
            CardEffect.Sprint or CardEffect.Shepherd => !g.InFinale && g.VillagersRemaining > 0,
            CardEffect.WildSwing or CardEffect.DesperatePlan => g.FightBonus >= 1 || g.InFinale,
            CardEffect.PatchUp => g.Health <= 3,
            CardEffect.HoldBreath or CardEffect.BarTheDoor => g.TidewifeNear,
            CardEffect.SteelNerves => true,
            _ => false
        };
        // Free cards are always worth taking: they can be traded in for partial dice.
        if (g.Market[m].Cost == 0 && g.Hand.Count < Game.HandLimit && g.CanBuy(m)) return g.Buy(m);
        if (useful && BotState.BoughtRound != g.Round && g.CanBuy(m) && g.Time - g.Market[m].Cost >= 2)
        {
            BotState.BoughtRound = g.Round;   // a sensible player buys one card a round, not everything on offer
            return g.Buy(m);
        }
    }

    // Paths respect locked doors (passable if we carry a key); with a vehicle, go as far along as one move allows.
    List<int>? PathTo(int to) => Board.Path(g.SurvivorLocation, to, null, (a, b) => !g.IsLocked(a, b) || g.Keys > 0);
    int Furthest(List<int> path, Func<int, bool> can)
    {
        for (int i = path.Count - 1; i >= 0; i--) if (can(path[i])) return path[i];
        return -1;
    }

    // Escort villagers home.
    if (!g.InFinale && g.Villagers[g.SurvivorLocation] > 0)
    {
        var path = PathTo(Board.Sanctuary);
        int to = path is { Count: > 0 } ? Furthest(path, g.CanEscort) : -1;
        if (to >= 0) return g.Escort(to);
    }

    if (g.FightBonus < 2 && g.CanSearch && (g.VillagersRemaining == 0 || g.Time >= 4)) return g.Search();

    // Choose where to go.
    int target;
    if (g.InFinale || (g.FightBonus >= 2 && g.Health >= 4)) target = g.TidewifeLocation;
    else
    {
        target = -1;
        int best = int.MaxValue;
        for (int i = 0; i < Board.Count; i++)
        {
            if (g.Villagers[i] == 0 && (g.StashCount(i) == 0 || g.FightBonus >= 2)) continue;
            int d = (PathTo(i)?.Count ?? 99) * 2 - g.Villagers[i];
            if (d < best) { best = d; target = i; }
        }
        if (target < 0) target = g.TidewifeLocation;
    }
    var step = PathTo(target);
    if (step is { Count: > 0 })
    {
        int to = Furthest(step, g.CanMove);
        if (to < 0) return false;
        // Don't walk into her while weak.
        if (to == g.TidewifeLocation && !strong && !g.InFinale) return false;
        return g.Move(to);
    }
    return false;
}

static class Dump
{
    public static void One(int seed, Action<Game, Random> play, string? scenario = null)
    {
        var g = new Game(seed, Difficulty.Normal, scenario is null ? null : Scenarios.ById(scenario)); var r = new Random(1); g.ChooseStartingCards(new[] { 0, 1, 2 });
        while (!g.IsOver && g.Round < 60) { play(g, r); if (g.Pending is not null) g.ConvertPartials(Array.Empty<int>(), Array.Empty<int>()); if (!g.IsOver) g.EndTurn(); }
        foreach (var e in g.Log) Console.WriteLine($"R{e.Round,-2} {e.Kind,-6} {e.Text}");
    }
}

static class BotState
{
    /// <summary>Round in which the heuristic bot last bought a card.</summary>
    public static int BoughtRound = -1;
}
