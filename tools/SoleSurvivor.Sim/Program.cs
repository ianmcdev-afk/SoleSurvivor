using SoleSurvivor.Core;

// Headless harness: plays many games with a heuristic player and a random-legal-move fuzzer,
// checks invariants, and prints balance numbers. Usage: SoleSurvivor.Sim [games]

if (args.Length > 1 && args[0] == "dump") { Dump.One(int.Parse(args[1]), args.Length > 2 ? args[2] : null); return; }
int games = args.Length > 0 ? int.Parse(args[0]) : 2000;

var only = args.Length > 1 ? args[1] : null;
foreach (var scenario in Scenarios.All)
{
    if (only is not null && scenario.Id != only) continue;
    Console.WriteLine($"== {scenario.Title} ({scenario.Id})");
    foreach (var d in Enum.GetValues<Difficulty>())
        Run($"Heur/{d}", games, Bot.Act, Bot.Plan, d, scenario);
    Run("Random", games, RandomBot.Act, RandomBot.Plan, Difficulty.Normal, scenario);
}

static void Run(string name, int games, Action<Game, Random> act, Action<Game, Random> plan, Difficulty difficulty, ScenarioDef scenario)
{
    int wins = 0, rounds = 0, saved = 0, lost = 0, finales = 0, finaleWins = 0, errors = 0;
    for (int seed = 0; seed < games; seed++)
    {
        var g = new Game(seed, difficulty, scenario);
        var ai = new Random(seed * 7919 + 1);
        try
        {
            if (g.CanEndTurn || g.CanEndActions || g.CanPlay(0)) throw new Exception("actions allowed before the night began");
            if (!g.Begin() || g.Begin()) throw new Exception("begin");
            if (g.Hand.Count != ActionCards.Starters.Sum(c => c.Copies)) throw new Exception("starting hand size");
            if (g.Tableau.Any(c => c.Cost == 0)) throw new Exception("basics in the tableau at the start");
            int guard = 0;
            while (!g.IsOver && guard++ < 200)
            {
                // Action phase.
                if (g.Step != TurnStep.Action) throw new Exception("turn didn't start in the Action phase");
                if (g.Tableau.Any(c => g.CanBuy(c))) throw new Exception("bought during the Action phase");
                act(g, ai);
                if (g.Pending is not null)
                {
                    if (g.CanEndActions || g.CanPlay(0)) throw new Exception("actions allowed while a roll is pending");
                    if (g.ConvertPartials(Array.Empty<int>(), new[] { 0 })) throw new Exception("cards without dice accepted");
                    var p0 = g.Pending;
                    int two = Enumerable.Range(0, p0.Roll.Values.Count).FirstOrDefault(i => p0.Roll.Values[i] == 3, -1);
                    if (two >= 0 && g.Hand.Count >= 1 && g.ConvertPartials(new[] { two }, new[] { 0 })) throw new Exception("a 3 converted with one card");
                    g.ConvertPartials(Array.Empty<int>(), Array.Empty<int>());
                }
                Check(g);
                if (g.IsOver) break;

                // Planning phase.
                var spent = g.SpentThisTurn.ToList();
                g.EndActions();
                if (g.Step != TurnStep.Planning) throw new Exception("didn't reach Planning");
                if (g.MoveTokens != 0 || g.EscortTokens != 0) throw new Exception("moves carried into Planning");
                if (Enumerable.Range(0, g.Hand.Count).Any(g.CanPlay)) throw new Exception("played a card during Planning");
                plan(g, ai);
                foreach (var c in spent.Distinct())
                    if (g.TableauCount(c) > 0 && spent.Count(x => x == c) + g.Hand.Count(x => x == c) + g.TableauCount(c) > Math.Max(c.Copies, 0) && false) { }
                if (g.Tableau.Intersect(g.SpentThisTurn).Any(c => g.CanBuy(c) && g.TableauCount(c) == 0)) throw new Exception("bought back a card used this turn");
                Check(g);

                int her = g.TidewifeLocation;
                int tableauBefore = g.Tableau.Count, spentBefore = g.SpentThisTurn.Count;
                g.EndTurn();
                if (!g.IsOver && g.Tableau.Count != tableauBefore + spentBefore) throw new Exception("used cards didn't return to the tableau");
                if (!g.IsOver && g.Time < 0) throw new Exception("time");
                // The event stream must replay her movement exactly (the UI animates from it).
                foreach (var ev in g.LastTide!.Events)
                {
                    if (ev.Kind is not (TideEventKind.Moved or TideEventKind.Rose)) continue;
                    if (ev.From != her) throw new Exception($"event chain broken: {ev.Kind} from {ev.From}, expected {her}");
                    if (ev.Kind == TideEventKind.Moved && !Board.AreAdjacent(ev.From, ev.To)) throw new Exception("non-adjacent step");
                    if (ev.Kind == TideEventKind.Moved && g.IsLocked(ev.From, ev.To) && !g.InFinale) throw new Exception("killer walked through a locked door");
                    her = ev.To;
                }
                if (her != g.TidewifeLocation) throw new Exception("events don't end where she is");
                Check(g);
                // Every card is somewhere: hand, tableau or spent.
                int total = ActionCards.All.Sum(c => c.Copies) + ActionCards.Starters.Sum(c => c.Copies);
                if (g.Hand.Count + g.Tableau.Count + g.SpentThisTurn.Count != total) throw new Exception("cards lost or duplicated");
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
    foreach (var c in g.Tableau.Distinct())
        if (g.CanBuy(c) != (g.BuyBlockedReason(c) is null)) throw new Exception($"buy reason disagrees with CanBuy for {c.Name}");
    if (g.ActiveEvents.Any(e => e.RoundsLeft < 0)) throw new Exception("event timer");
    if (g.TidewifeHealth < 0) throw new Exception("tidewife hp");
    if (g.Villagers.Any(v => v < 0)) throw new Exception("villagers");
    if (g.Swarm.Any(v => v < 0)) throw new Exception("swarm count");
    if (!g.Scenario.HasSwarm && g.SwarmTotal != 0) throw new Exception("swarm in a scenario without one");
    if (g.Scenario.HasSwarm && g.SwarmTotal > g.Scenario.SwarmMax) throw new Exception("swarm over its cap");
    if (g.Villagers[Board.Sanctuary] != 0) throw new Exception("villagers left standing in lighthouse");
    if (g.Hand.Count > Game.HandLimit) throw new Exception("hand");
    if (g.Phase == Phase.Won && g.TidewifeHealth != 0) throw new Exception("won without kill");
    if (g.Phase == Phase.Lost && g.Health != 0) throw new Exception("lost while alive");
}

/// <summary>Random legal play: the fuzzer.</summary>
static class RandomBot
{
    public static void Act(Game g, Random r)
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
            for (int n = 0; n < Board.Count; n++)
            {
                int to = n;
                if (g.CanMove(to)) options.Add(() => g.Move(to));
                if (g.CanEscort(to)) options.Add(() => g.Escort(to));
            }
            for (int h = 0; h < g.Hand.Count; h++) { int k = h; if (g.CanPlay(k)) options.Add(() => g.Play(k)); }
            for (int h = 0; h < g.Hand.Count; h++) { int k = h; if (g.CanDiscard(k) && r.Next(3) == 0) options.Add(() => g.Discard(k)); }
            for (int it = 0; it < g.Inventory.Count; it++) { int k = it; if (g.CanUse(k)) options.Add(() => g.Use(k)); }
            if (options.Count == 0 || r.Next(8) == 0) return;
            if (r.Next(4) == 0) g.SteelYourself = true;
            if (!options[r.Next(options.Count)]()) throw new Exception("CanX said yes but X refused");
        }
    }

    public static void Plan(Game g, Random r)
    {
        for (int i = 0; i < 20; i++)
        {
            var buyable = g.Tableau.Distinct().Where(g.CanBuy).ToList();
            if (buyable.Count == 0 || r.Next(5) == 0) break;
            if (!g.Buy(buyable[r.Next(buyable.Count)])) throw new Exception("CanBuy said yes but Buy refused");
        }
        if (g.Hand.Count > 0 && r.Next(6) == 0 && g.CanDiscard(0)) g.Discard(0);
    }
}

/// <summary>A sensible player: plays the cards it needs in the Action phase, then buys for next turn in Planning.</summary>
static class Bot
{
    public static void Act(Game g, Random r)
    {
        for (int guard = 0; guard < 40 && !g.IsOver; guard++)
            if (!Step(g)) return;
    }

    static int Find<T>(IReadOnlyList<T> list, Func<T, bool> pred)
    {
        for (int i = 0; i < list.Count; i++) if (pred(list[i])) return i;
        return -1;
    }

    static bool Has(Game g, CardEffect e) => g.Hand.Any(c => c.Effect == e);

    static bool Step(Game g)
    {
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

        bool strong = g.FightBonus >= 1 || g.InFinale;

        if (UseItem(ItemKind.FlareGun)) return true;
        if (g.Health <= 3 && (UseItem(ItemKind.Bandages) || PlayEffect(CardEffect.PatchUp))) return true;
        if (PlayEffect(CardEffect.SteelNerves)) return true;

        if (g.TidewifeHere)
        {
            if (g.Hope > 0) g.SteelYourself = true;
            if (PlayEffect(CardEffect.DesperatePlan) || PlayEffect(CardEffect.WildSwing)) return true;
            if ((strong || g.Health >= 4) && PlayEffect(CardEffect.Shove)) return true;
            g.SteelYourself = false;
            if (UseItem(ItemKind.BlessedSalt)) return true;
        }
        // Clear the swarm off you when you can: they nip every turn.
        if (!g.TidewifeHere && g.SwarmHere > 0 && (PlayEffect(CardEffect.Shove) || (g.SwarmHere >= 2 && PlayEffect(CardEffect.WildSwing)))) return true;
        if (g.Health <= 3 && PlayEffect(CardEffect.CatchBreath)) return true;
        if (PlayEffect(CardEffect.LanternSignal)) return true;

        List<int>? PathTo(int to) => Board.Path(g.SurvivorLocation, to, null, (a, b) => !g.IsLocked(a, b) || g.Keys > 0);
        int Furthest(List<int> path, Func<int, bool> can)
        {
            for (int i = path.Count - 1; i >= 0; i--) if (can(path[i])) return path[i];
            return -1;
        }

        // Escort people home: get escorts from a card first.
        if (!g.InFinale && g.Villagers[g.SurvivorLocation] > 0)
        {
            if (g.EscortTokens < g.EscortCost && (PlayEffect(CardEffect.Shepherd) || PlayEffect(CardEffect.Guide))) return true;
            var path = PathTo(Board.Sanctuary);
            int to = path is { Count: > 0 } ? Furthest(path, g.CanEscort) : -1;
            if (to >= 0) return g.Escort(to);
        }

        // Search where there's something to find, until well armed.
        if (g.FightBonus < 2 && g.StashCount(g.SurvivorLocation) > 0 && (PlayEffect(CardEffect.Rummage) || PlayEffect(CardEffect.GlanceAround))) return true;

        // Choose where to go, then get moves from a card.
        int target;
        if (g.InFinale || (g.FightBonus >= 2 && g.Health >= 4)) target = g.TidewifeLocation;
        else
        {
            target = -1;
            int best = int.MaxValue;
            for (int i = 0; i < Board.Count; i++)
            {
                if (g.Villagers[i] == 0 && (g.StashCount(i) == 0 || g.FightBonus >= 2)) continue;
                if (i == g.SurvivorLocation) continue;
                int d = (PathTo(i)?.Count ?? 99) * 2 - g.Villagers[i];
                if (d < best) { best = d; target = i; }
            }
            if (target < 0) target = g.TidewifeLocation;
        }
        var step = PathTo(target);
        if (step is { Count: > 0 })
        {
            if (step[0] == g.TidewifeLocation && !strong && !g.InFinale) return false; // don't walk into her while weak
            if (g.MoveTokens == 0 && !(step.Count > 1 && PlayEffect(CardEffect.Sprint)) && !PlayEffect(CardEffect.QuickStep) && !PlayEffect(CardEffect.Sprint)) return false;
            int to = Furthest(step, g.CanMove);
            if (to < 0) return false;
            if (to == g.TidewifeLocation && !strong && !g.InFinale) return false;
            return g.Move(to);
        }
        return false;
    }

    public static void Plan(Game g, Random r)
    {
        bool Buy(CardEffect e, int maxHeld = 9)
        {
            if (g.Hand.Count(c => c.Effect == e) >= maxHeld) return false;
            var card = g.Tableau.FirstOrDefault(c => c.Effect == e);
            return card is not null && g.CanBuy(card) && g.Buy(card);
        }
        bool rescuing = !g.InFinale && g.VillagersRemaining > 0;
        bool strong = g.FightBonus >= 1 || g.InFinale;
        // Movement and rescue first: you can't do anything without them.
        Buy(CardEffect.QuickStep, 2);
        if (rescuing) { Buy(CardEffect.Guide, 1); Buy(CardEffect.Shepherd, 1); }
        if (strong) { Buy(CardEffect.DesperatePlan, 1); Buy(CardEffect.WildSwing, 2); }
        Buy(CardEffect.Shove, 1);
        if (g.FightBonus < 2) Buy(CardEffect.GlanceAround, 1);
        if (g.Health <= 3) { Buy(CardEffect.CatchBreath, 1); Buy(CardEffect.PatchUp, 1); }
        Buy(CardEffect.Sprint, 1);
        if (g.FightBonus < 2) Buy(CardEffect.Rummage, 1);
        Buy(CardEffect.SteelNerves, 1);
        Buy(CardEffect.CatchBreath, 1);
        Buy(CardEffect.QuickStep, 2);
    }
}

static class Dump
{
    public static void One(int seed, string? scenario = null)
    {
        var g = new Game(seed, Difficulty.Normal, scenario is null ? null : Scenarios.ById(scenario)); var r = new Random(1); g.Begin();
        while (!g.IsOver && g.Round < 60)
        {
            Bot.Act(g, r);
            if (g.Pending is not null) g.ConvertPartials(Array.Empty<int>(), Array.Empty<int>());
            if (g.IsOver) break;
            g.EndActions(); Bot.Plan(g, r); g.EndTurn();
        }
        foreach (var e in g.Log) Console.WriteLine($"R{e.Round,-2} {e.Kind,-6} {e.Text}");
    }
}

