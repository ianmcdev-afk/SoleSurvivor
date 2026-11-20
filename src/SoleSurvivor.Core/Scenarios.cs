namespace SoleSurvivor.Core;

public sealed record SurvivorDef(string Name, string ShortName, string Role, string Bio);

/// <param name="Name">With article, title case: "The Tidewife".</param>
/// <param name="She">Subject pronoun, capitalised: "She" / "It".</param>
/// <param name="Her">Object pronoun: "her" / "it".</param>
public sealed record KillerDef(string Name, string ShortName, string She, string Her, string Bio, int HealthBonus = 0)
{
    public string SheLower => She.ToLowerInvariant();
    public string NameLower => Name.StartsWith("The ") ? "the " + Name[4..] : Name;
}

/// <summary>
/// One playable scenario: its map, characters, item names, killer deck and all the words the game uses for them.
/// Mechanics are shared; only content and wording differ.
/// </summary>
public sealed class ScenarioDef
{
    // Identity
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string Place { get; init; }
    public required string Tagline { get; init; }
    public required string Blurb { get; init; }
    public required string Intro { get; init; }

    public required SurvivorDef Survivor { get; init; }
    public required KillerDef Killer { get; init; }

    // Words
    public required string Victim { get; init; }
    public required string Victims { get; init; }
    /// <summary>The killer's turn, as in "next tide".</summary>
    public required string RoundWord { get; init; }
    public required string DeckName { get; init; }
    public required string PhaseBanner { get; init; }
    public required string FinaleName { get; init; }
    public required string FinaleFlavour { get; init; }
    public required string FinaleTakes { get; init; }
    public required string FinaleBannerSub { get; init; }
    /// <summary>"drags {0} into the water": {0} is "2 villagers".</summary>
    public required string ClaimText { get; init; }
    public required string MoveVerb { get; init; }
    public required string LingerVerb { get; init; }
    public required string RiseText { get; init; }
    public required string NobodyThere { get; init; }
    public required string BarredRise { get; init; }
    public required string BarredDoor { get; init; }
    public required string BarricadeText { get; init; }
    public required string LureText { get; init; }
    public required string RepelText { get; init; }
    public required string RepelUse { get; init; }
    public required string WinText { get; init; }
    public required string LoseText { get; init; }
    public required string FogName { get; init; }

    // Map
    public required LocationDef[] Locations { get; init; }
    public required (int A, int B)[] Edges { get; init; }
    public required int SanctuaryId { get; init; }
    public required int KillerStart { get; init; }
    public required int SummonId { get; init; }
    /// <summary>Dread at the start of the night (Horror track). 1 = Uneasy with room to spare; 2 = one loss from Afraid.</summary>
    public int StartDread { get; init; } = 1;

    // Indoor / special map features
    /// <summary>Doors that start locked. Nobody walks through a locked door (the killer included) until you open it with a key.</summary>
    public (int A, int B)[] LockedEdges { get; init; } = [];
    /// <summary>Name of the key item (needed if there are locked doors), e.g. "Room Key".</summary>
    public string? KeyName { get; init; }
    /// <summary>Name of the vehicle item, e.g. "Bicycle". Null = no vehicle in this scenario.</summary>
    public string? VehicleName { get; init; }
    /// <summary>How many spaces one move covers with the vehicle.</summary>
    public int VehicleRange { get; init; }
    /// <summary>Verb for moving with the vehicle: "cycle", "drive".</summary>
    public string VehicleVerb { get; init; } = "drive";
    /// <summary>Background loop and stinger names; defaults are amb_{Id}.</summary>
    public string VehicleSound { get; init; } = "drive_car";

    // The swarm: small creatures that spawn from the killer, chase you and nip for small damage. Null = no swarm.
    /// <summary>One of the swarm, lower case: "sootling". Null if this scenario has no swarm.</summary>
    public string? SwarmName { get; init; }
    public string SwarmNames { get; init; } = "";
    /// <summary>How many start on the board (spread over the lairs).</summary>
    public int SwarmStart { get; init; }
    /// <summary>How many the killer spawns where it stands, each of its turns (one more in the finale).</summary>
    public int SwarmSpawn { get; init; } = 1;
    /// <summary>Most that can be on the board at once.</summary>
    public int SwarmMax { get; init; } = 8;
    /// <summary>"nip and claw at you": what they do when they reach you.</summary>
    public string SwarmBite { get; init; } = "swarm over you";
    public bool HasSwarm => SwarmName is not null;

    public bool IsLockedEdge(int a, int b) => LockedEdges.Any(e => (e.A == a && e.B == b) || (e.A == b && e.B == a));

    /// <summary>The killer board's Bloodlust track, bottom (start) to top. Kills move the marker up.</summary>
    public required BloodlustLevel[] BloodlustTrack { get; init; }

    // Content
    /// <summary>Names for the eight item kinds, in ItemKind order.</summary>
    public required string[] ItemNames { get; init; }
    /// <summary>Names and flavour for the 12 events, in EventCards.Templates (EventKind) order.</summary>
    public required (string Name, string Flavour)[] EventNames { get; init; }

    /// <summary>Names and flavour for the 18 tide mechanics, in TideCards.Mechanics order.</summary>
    public required (string Name, string Flavour)[] TideNames { get; init; }

    public string SanctuaryName => Locations[SanctuaryId].Name;

    /// <summary>Replace the scenario tokens in a piece of text.</summary>
    public string Fill(string text) => text
        .Replace("{She}", Killer.She)
        .Replace("{she}", Killer.SheLower)
        .Replace("{her}", Killer.Her)
        .Replace("{Killer}", Killer.Name)
        .Replace("{killer}", Killer.NameLower)
        .Replace("{Victims}", char.ToUpperInvariant(Victims[0]) + Victims[1..])
        .Replace("{victims}", Victims)
        .Replace("{victim}", Victim)
        .Replace("{round}", RoundWord)
        .Replace("{place}", Place)
        .Replace("{sanctuary}", SanctuaryName);

    private List<int>[]? _adjacency;
    public List<int>[] Adjacency => _adjacency ??= BuildAdjacency();

    private List<int>[] BuildAdjacency()
    {
        var adj = new List<int>[Locations.Length];
        for (int i = 0; i < adj.Length; i++) adj[i] = new List<int>();
        foreach (var (a, b) in Edges)
        {
            adj[a].Add(b);
            adj[b].Add(a);
        }
        return adj;
    }

    private Dictionary<ItemKind, ItemDef>? _items;
    public IReadOnlyDictionary<ItemKind, ItemDef> Items => _items ??= BuildItems();

    private Dictionary<ItemKind, ItemDef> BuildItems()
    {
        var items = BaseItems();
        if (KeyName is not null)
            items[ItemKind.Key] = new(ItemKind.Key, KeyName, Fill("Opens one locked door. Walk through it to unlock it for good (for {her} too)."), false);
        if (VehicleName is not null)
            items[ItemKind.Vehicle] = new(ItemKind.Vehicle, VehicleName,
                Fill($"Walk or escort up to {VehicleRange} spaces in one move. Not through locked doors, and not past {{her}}."), false);
        return items;
    }

    private Dictionary<ItemKind, ItemDef> BaseItems() => new()
    {
        [ItemKind.BoatHook] = new(ItemKind.BoatHook, ItemNames[0], "+1 die when you fight.", false, FightBonus: 1),
        [ItemKind.GuttingKnife] = new(ItemKind.GuttingKnife, ItemNames[1], "+1 die when you fight.", false, FightBonus: 1),
        [ItemKind.Lantern] = new(ItemKind.Lantern, ItemNames[2], "+1 die when you search.", false, SearchBonus: 1),
        [ItemKind.Rope] = new(ItemKind.Rope, ItemNames[3], Fill("Escort one extra {victim}."), false),
        [ItemKind.FlareGun] = new(ItemKind.FlareGun, ItemNames[4], Fill("Use: 2 wounds to {killer} if {she} is here or adjacent."), true),
        [ItemKind.Bandages] = new(ItemKind.Bandages, ItemNames[5], "Use: heal 2.", true),
        [ItemKind.BlessedSalt] = new(ItemKind.BlessedSalt, ItemNames[6], Fill("Use: 1 wound if {she} is here or adjacent, and {she} cannot strike you next {round}."), true),
        [ItemKind.IronNails] = new(ItemKind.IronNails, ItemNames[7], Fill("Use: {She} cannot enter your location next {round}."), true),
    };

    private TideCardDef[]? _tide;
    public IReadOnlyList<TideCardDef> TideCards => _tide ??= TideNames
        .Select((n, i) =>
        {
            var m = Core.TideCards.Mechanics[i];
            return new TideCardDef(n.Name, n.Flavour, m.Move, m.Attacks, m.Effect, m.Event);
        })
        .ToArray();

    private EventCardDef[]? _events;
    public IReadOnlyList<EventCardDef> EventCards => _events ??= EventNames
        .Select((n, i) =>
        {
            var t = Core.EventCards.Templates[i];
            return new EventCardDef(t.Kind, n.Name, n.Flavour, Fill(t.Text), t.Discard is null ? null : Fill(t.Discard), t.Ongoing, t.Tone);
        })
        .ToArray();

    private ActionCardDef[]? _actions, _starters;
    public IReadOnlyList<ActionCardDef> ActionCards => _actions ??= Core.ActionCards.Templates.Select(c => c with { Text = Fill(c.Text) }).ToArray();
    public IReadOnlyList<ActionCardDef> StarterCards => _starters ??= Core.ActionCards.StarterTemplates.Select(c => c with { Text = Fill(c.Text) }).ToArray();

    /// <summary>Starting from the sanctuary, open doors with keys found in reachable places: every location must end up
    /// reachable, and every victim must be able to get home without passing a locked door.</summary>
    private void CheckKeysReachEverything()
    {
        var open = new HashSet<(int, int)>();
        bool Pass(int a, int b) => !IsLockedEdge(a, b) || open.Contains((Math.Min(a, b), Math.Max(a, b)));
        HashSet<int> Reach()
        {
            var seen = new HashSet<int> { SanctuaryId };
            var q = new Queue<int>(seen);
            while (q.Count > 0)
            {
                int c = q.Dequeue();
                foreach (int n in Adjacency[c])
                    if (Pass(c, n) && seen.Add(n)) q.Enqueue(n);
            }
            return seen;
        }
        var reach = Reach();
        int keysUsed = 0;
        while (true)
        {
            int keys = reach.Sum(i => Locations[i].Stash.Count(k => k == ItemKind.Key)) - keysUsed;
            var door = LockedEdges.Select(e => (Math.Min(e.A, e.B), Math.Max(e.A, e.B)))
                .FirstOrDefault(e => !open.Contains(e) && (reach.Contains(e.Item1) || reach.Contains(e.Item2)));
            if (keys <= 0 || door == default) break;
            open.Add(door);
            keysUsed++;
            reach = Reach();
        }
        if (reach.Count != Locations.Length) throw new InvalidOperationException($"{Id}: not enough reachable keys to open every area");
        foreach (var loc in Locations.Where(l => l.StartVillagers > 0))
        {
            // Victims start outside locked areas, so the starting map always has a way home.
            var seen = new HashSet<int> { loc.Id };
            var q = new Queue<int>(seen);
            while (q.Count > 0)
            {
                int c = q.Dequeue();
                foreach (int n in Adjacency[c])
                    if (!IsLockedEdge(c, n) && seen.Add(n)) q.Enqueue(n);
            }
            if (!seen.Contains(SanctuaryId)) throw new InvalidOperationException($"{Id}: victims at {loc.Name} start behind a locked door");
        }
    }

    /// <summary>Checks the scenario is well formed. Throws with a clear message if not.</summary>
    public void Validate()
    {
        if (Locations.Length < 2) throw new InvalidOperationException($"{Id}: needs locations");
        for (int i = 0; i < Locations.Length; i++)
            if (Locations[i].Id != i) throw new InvalidOperationException($"{Id}: location {i} has id {Locations[i].Id}");
        if (!Locations[SanctuaryId].IsSanctuary) throw new InvalidOperationException($"{Id}: sanctuary flag");
        if (Locations.Count(l => l.IsSanctuary) != 1) throw new InvalidOperationException($"{Id}: exactly one sanctuary");
        if (!Locations.Any(l => l.IsLair)) throw new InvalidOperationException($"{Id}: needs a lair");
        if (Locations[SanctuaryId].StartVillagers != 0) throw new InvalidOperationException($"{Id}: sanctuary must start empty");
        if (ItemNames.Length != 8) throw new InvalidOperationException($"{Id}: 8 item names");
        if (TideNames.Length != Core.TideCards.Mechanics.Length) throw new InvalidOperationException($"{Id}: {Core.TideCards.Mechanics.Length} tide names");
        if (EventNames.Length != Core.EventCards.Templates.Length) throw new InvalidOperationException($"{Id}: {Core.EventCards.Templates.Length} event names");
        foreach (var (a, b) in LockedEdges)
            if (!Edges.Any(e => (e.A == a && e.B == b) || (e.A == b && e.B == a))) throw new InvalidOperationException($"{Id}: locked door {a}-{b} is not a road");
        if (LockedEdges.Length > 0 && KeyName is null) throw new InvalidOperationException($"{Id}: locked doors but no key name");
        if (Locations.Any(l => l.Stash.Contains(ItemKind.Vehicle)) && (VehicleName is null || VehicleRange < 2)) throw new InvalidOperationException($"{Id}: vehicle needs a name and range 2+");
        CheckKeysReachEverything();
        if (HasSwarm && (SwarmNames.Length == 0 || SwarmMax < 1 || SwarmStart > SwarmMax || SwarmSpawn < 0)) throw new InvalidOperationException($"{Id}: bad swarm settings");
        if (BloodlustTrack.Length < 2 || BloodlustTrack.Any(l => l.Move < 1 || l.Attack < 1)) throw new InvalidOperationException($"{Id}: bad Bloodlust track");
        foreach (var (a, b) in Edges)
            if (a == b || a < 0 || b < 0 || a >= Locations.Length || b >= Locations.Length) throw new InvalidOperationException($"{Id}: bad edge {a}-{b}");
        // Connected?
        var seen = new HashSet<int> { 0 };
        var queue = new Queue<int>(seen);
        while (queue.Count > 0)
            foreach (int n in Adjacency[queue.Dequeue()])
                if (seen.Add(n)) queue.Enqueue(n);
        if (seen.Count != Locations.Length) throw new InvalidOperationException($"{Id}: map is not connected");
    }
}

/// <summary>All scenarios, and which one is in play.</summary>
public static class Scenarios
{
    private static ScenarioDef? _current;

    /// <summary>The scenario in play (Gallowmouth until another is chosen).</summary>
    public static ScenarioDef Current => _current ?? Gallowmouth;

    public static void Use(ScenarioDef scenario)
    {
        scenario.Validate();
        _current = scenario;
    }

    public static IReadOnlyList<ScenarioDef> All => [Gallowmouth, Whitepine, StAgnes, Larkspur, Kestrel, Ashgrove, CedarHollow, Holloway, Crowhurst, Merriwick];

    public static ScenarioDef ById(string? id) => All.FirstOrDefault(s => s.Id == id) ?? Gallowmouth;

    // ================================================================== Gallowmouth (coastal folk horror)

    public static ScenarioDef Gallowmouth { get; } = new()
    {
        Id = "gallowmouth",
        Title = "Gallowmouth",
        Place = "Gallowmouth",
        Tagline = "The night the sea came walking.",
        Blurb = "A Cornish fishing village. Something drowned has come up the slipway, and it is hungry.",
        Intro = "Gallowmouth, the night the sea came walking. Get the villagers to the Lighthouse, and put the Tidewife back in the sea.",
        Survivor = new("Wren Calloway", "Wren", "The keeper's daughter",
            "Grew up in the lighthouse. Knows every path on the cliff, and every old story about the sea."),
        Killer = new("The Tidewife", "Tidewife", "She", "her",
            "Drowned, pale, hair like kelp. She sings under the boards, and she takes whoever she finds.", HealthBonus: -1),
        Victim = "villager", Victims = "villagers",
        RoundWord = "tide",
        DeckName = "Tide card",
        PhaseBanner = "THE TIDE COMES IN",
        FinaleName = "Spring Tide",
        FinaleFlavour = "The whole sea stands up and walks inland, hunting you.",
        FinaleTakes = "The sea floods Gallowmouth",
        FinaleBannerSub = "The sea takes the village. Now she hunts only you.",
        ClaimText = "drags {0} into the water",
        MoveVerb = "drifts", LingerVerb = "lingers",
        RiseText = "sinks into the water and rises again",
        NobodyThere = "She rises from a well, finds nobody, and sinks again.",
        BarredRise = "Water bubbles up under your barred door, but she cannot get in.",
        BarredDoor = "She hammers on your barred door, but it holds.",
        BarricadeText = "You nail the doors shut.",
        LureText = "You whistle into the dark.",
        RepelText = "She reaches for you, then flinches from the salt.",
        RepelUse = "She recoils from the salt.",
        WinText = "The Tidewife comes apart like wet sand. The sea goes back where it belongs.",
        LoseText = "Cold water closes over your head. Gallowmouth belongs to the sea.",
        FogName = "Fog",
        Locations =
        [
            new(0, "Lighthouse", "The lamp still turns. Villagers brought here are safe.", 95, 70, false, true, 0, [ItemKind.FlareGun]),
            new(1, "Cliff Path", "Wind, gorse, and a long drop to the rocks.", 175, 235, false, false, 1, [ItemKind.Rope]),
            new(2, "Chapel", "St Brannoc's. The bell rings on its own tonight.", 440, 105, false, false, 1, [ItemKind.BlessedSalt, ItemKind.Bandages]),
            new(3, "Anchor Inn", "The inn's fire is out. Someone is under the tables.", 410, 300, false, false, 2, [ItemKind.GuttingKnife, ItemKind.Bandages, ItemKind.Lantern]),
            new(4, "Cottages", "Doors banging. Wet prints on every step.", 665, 235, false, false, 2, [ItemKind.IronNails, ItemKind.BlessedSalt]),
            new(5, "Harbour", "The water is far too still.", 390, 490, true, false, 0, [ItemKind.BoatHook]),
            new(6, "Boathouse", "Upturned hulls, and something breathing beneath one.", 130, 475, true, false, 1, [ItemKind.FlareGun, ItemKind.IronNails]),
            new(7, "Cannery", "Hooks, brine vats, and the stink of old fish.", 660, 465, true, false, 1, [ItemKind.GuttingKnife, ItemKind.Bandages]),
        ],
        Edges = [(0, 1), (1, 2), (1, 3), (1, 6), (2, 3), (2, 4), (3, 4), (3, 5), (4, 7), (5, 7), (5, 6)],
        SanctuaryId = 0,
        KillerStart = 5,
        SummonId = 2,
        BloodlustTrack =
        [
            new(1, 1),
            new(1, 1, BloodlustEffect.DreadUp),
            new(1, 2, BloodlustEffect.DreadUp),
            new(2, 2, BloodlustEffect.DreadUp),
            new(2, 2, BloodlustEffect.DreadUp),
            new(2, 2, BloodlustEffect.Frenzy),
            new(2, 2, BloodlustEffect.DreadUp),
            new(2, 3, BloodlustEffect.DreadUp),
            new(3, 3, BloodlustEffect.DreadUp),
            new(3, 3, BloodlustEffect.DreadUp),
            new(3, 3),
        ],
        ItemNames = ["Boat Hook", "Gutting Knife", "Storm Lantern", "Mooring Rope", "Flare Gun", "Bandages", "Blessed Salt", "Iron Nails"],
        EventNames =
        [
            ("Sweetheart", "\"Wren! I'm not leaving without you.\""),
            ("The Drowned Boy's Mother", "She can barely stand, but she won't stop calling his name."),
            ("Salt Air", "The cold wind off the sea clears your head."),
            ("Washed Ashore", "The tide leaves gifts as well as bodies."),
            ("The Keeper's Words", "\"The light always comes back, girl. Always.\""),
            ("Storm Surge", "Your heart hammers louder than the waves."),
            ("Lobster Pot", "Rope, iron and a heavy lid. It'll hold her, briefly."),
            ("Lights in the Chapel", "More of them were hiding all along."),
            ("Gulls Fall Silent", "Every bird on the harbour stops at once."),
            ("The Lamps Gutter", "Wick by wick, the village goes dark."),
            ("Wet Footprints", "They lead to wherever you're standing."),
            ("The Tide Turns", "Somewhere, the sea drinks deep."),
        ],
        TideNames =
        [
            ("The Tide Turns", "Water creeps over the slipway."),
            ("Wet Footsteps", "Bare prints, leading inland."),
            ("She Sings Below", "A hymn from under the boards."),
            ("Kelp in the Doorway", "Somebody opened the door to her."),
            ("Fog Rolls In", "You can't see your own hands."),
            ("A Straggler", "Someone runs out into the street, shouting."),
            ("Drowned Bells", "The chapel bell tolls, and she answers."),
            ("Salt Rot", "She has your scent now."),
            ("Low Water", "The sea draws back. A moment to breathe."),
            ("Grasping Hands", "Cold fingers close around your ankle."),
            ("Brine Hunger", "She is not patient tonight."),
            ("From the Wells", "Every well in the village overflows at once."),
            ("Gulls Scatter", "The gulls lift off the roofs all at once."),
            ("Cold Breath", "Your lungs fill with sea air and fear."),
            ("The Long Wait", "She stands still and lets the sea heal her."),
            ("Sea Fret", "Salt mist thick as wool."),
            ("Weeping Nets", "The nets on the quay are full of hair."),
            ("High Water", "The sea comes over the harbour wall."),
        ],
    };

    // ================================================================== Whitepine (snowbound wilderness)

    public static ScenarioDef Whitepine { get; } = new()
    {
        Id = "whitepine",
        Title = "Whitepine",
        Place = "Whitepine",
        Tagline = "The snow came early, and something came with it.",
        Blurb = "A national park, cut off by the first blizzard of the year. Campers scattered across the trails, and tracks in the snow that no animal made.",
        Intro = "Whitepine, the night the snow came early. Get the campers to the Ranger Station, and drive the Hollow Stag back into the dark.",
        Survivor = new("Dana Okafor", "Dana", "Park ranger",
            "Twelve winters on these trails. She knows every shortcut. She has never seen tracks like these."),
        Killer = new("The Hollow Stag", "Hollow Stag", "It", "it",
            "Antlers like dead branches, a body of frozen bark and bone. It walks on two legs when no one is watching.", HealthBonus: 2),
        Victim = "camper", Victims = "campers",
        RoundWord = "gust",
        DeckName = "Storm card",
        PhaseBanner = "THE STORM CLOSES IN",
        FinaleName = "Whiteout",
        FinaleFlavour = "The world goes white. It hunts you by the sound of your breath.",
        FinaleTakes = "The blizzard buries Whitepine",
        FinaleBannerSub = "The snow takes the park. Now it hunts only you.",
        ClaimText = "drags {0} off into the snow",
        MoveVerb = "stalks", LingerVerb = "waits",
        RiseText = "melts into the treeline and steps out again",
        NobodyThere = "It steps out of the trees, finds nobody, and is gone again.",
        BarredRise = "Something scrapes at your wedged door, but it cannot get in.",
        BarredDoor = "It slams against your wedged door, but the door holds.",
        BarricadeText = "You wedge the door shut.",
        LureText = "You bang your torch against a pine.",
        RepelText = "It lunges, then reels back, choking on the spray.",
        RepelUse = "It reels back from the spray.",
        WinText = "The Hollow Stag cracks apart like river ice. The storm blows itself out.",
        LoseText = "The snow closes over you. Whitepine belongs to the Hollow Stag.",
        FogName = "Snow squall",
        Locations =
        [
            new(0, "Ranger Station", "Woodstove, radio, a door that bolts. Campers brought here are safe.", 110, 470, false, true, 0, [ItemKind.FlareGun]),
            new(1, "Trailhead", "A map board, buried to the waist in snow.", 255, 330, false, false, 1, [ItemKind.Rope]),
            new(2, "Ski Lodge", "Chairs stacked, fire dead, something scratching at the shutters.", 130, 190, false, false, 2, [ItemKind.GuttingKnife, ItemKind.Bandages, ItemKind.Lantern]),
            new(3, "Cabins", "Six cabins. Five doors hanging open.", 455, 445, false, false, 2, [ItemKind.IronNails, ItemKind.BlessedSalt]),
            new(4, "Sawmill", "Blades frozen mid-turn. Sawdust, and something darker.", 445, 235, false, false, 1, [ItemKind.BoatHook, ItemKind.Bandages]),
            new(5, "Fire Tower", "Ninety steps up. The bell at the top rings by itself.", 300, 85, false, false, 1, [ItemKind.BlessedSalt, ItemKind.Bandages]),
            new(6, "Frozen Lake", "The ice is thick. Something underneath is looking up.", 655, 360, true, false, 0, [ItemKind.FlareGun, ItemKind.IronNails]),
            new(7, "Old Mine", "Timbers groan. The dark goes down a long way.", 650, 125, true, false, 1, [ItemKind.GuttingKnife]),
        ],
        Edges = [(0, 1), (0, 3), (1, 2), (1, 3), (1, 4), (2, 5), (5, 4), (5, 7), (4, 7), (4, 6), (3, 6)],
        SanctuaryId = 0,
        KillerStart = 6,
        SummonId = 5,
        BloodlustTrack =
        [
            new(1, 1),
            new(2, 1, BloodlustEffect.DreadUp),
            new(2, 1, BloodlustEffect.DreadUp),
            new(2, 2, BloodlustEffect.DreadUp),
            new(2, 2, BloodlustEffect.DreadUp),
            new(3, 2, BloodlustEffect.Frenzy),
            new(3, 2, BloodlustEffect.DreadUp),
            new(3, 2, BloodlustEffect.DreadUp),
            new(3, 3, BloodlustEffect.DreadUp),
            new(3, 3, BloodlustEffect.DreadUp),
            new(3, 3),
        ],
        ItemNames = ["Felling Axe", "Hunting Knife", "Head Torch", "Climbing Rope", "Signal Flare", "First-Aid Kit", "Bear Spray", "Door Wedge"],
        EventNames =
        [
            ("Camp Counsellor", "\"Stay behind me. I've done first aid. I can help.\""),
            ("Frostbite", "His hands are black. He can't hold onto anything."),
            ("Thermos of Coffee", "Still warm. Somebody's watching out for you."),
            ("Supply Cache", "A ranger's emergency box, half buried in snow."),
            ("Radio Crackle", "\"...Station, we hear you. Hold on. Hold on.\""),
            ("Cold Clarity", "The cold sharpens everything. You've never felt so awake."),
            ("Bear Trap", "Rusted, but the springs still bite."),
            ("Headlamps in the Trees", "A second camping party, lost in the storm."),
            ("Antler Marks", "Carved into every trunk around you."),
            ("Whiteout Squall", "You can't see your hand in front of your face."),
            ("It Has Your Scent", "It stops. It turns. It knows."),
            ("Blood on the Snow", "It found someone else first."),
        ],
        TideNames =
        [
            ("The Wind Turns", "The wind changes, and something rides it."),
            ("Cloven Tracks", "Fresh prints, leading toward the cabins."),
            ("It Calls Your Name", "Your name, in your mother's voice, from the trees."),
            ("Door Left Open", "Someone let the cold in."),
            ("Snow Squall", "White on white on white."),
            ("A Straggler", "A camper stumbles out of the treeline, shouting."),
            ("The Tower Bell", "The fire tower's bell rings once, and it answers."),
            ("Scent of Blood", "It has your scent now."),
            ("Clear Skies", "The clouds part. A moment to breathe."),
            ("Frozen Grip", "Bark-hard fingers close around your ankle."),
            ("Hunger Moon", "It is not patient tonight."),
            ("Through the Pines", "It steps out of the trees wherever the campers huddle."),
            ("Ravens Scatter", "Every raven in the valley takes flight at once."),
            ("Frostbite", "Your fingers won't work. Your breath freezes."),
            ("Antlers in the Dark", "It stands perfectly still while the frost knits its wounds."),
            ("Whiteout Gust", "The wind throws the snow sideways."),
            ("Bones in the Snow", "Old bones, half-buried, all pointing the same way."),
            ("Avalanche", "Somewhere above, the mountain lets go."),
        ],
    };

    // ================================================================== St Agnes (derelict asylum)

    public static ScenarioDef StAgnes { get; } = new()
    {
        Id = "stagnes",
        Title = "St Agnes",
        Place = "St Agnes",
        Tagline = "Closed in 1974. Something never left.",
        Blurb = "A derelict Victorian hospital on the moor. You came to take photos. The gates locked behind you, and the lights came on by themselves.",
        Intro = "St Agnes, the night the gates locked behind you. Get the explorers to the Gatehouse, and put the Matron to rest.",
        Survivor = new("Eli Marsh", "Eli", "Urban explorer",
            "Came for photos of the old wards. Brought five friends, a camera and one good torch."),
        Killer = new("The Matron", "Matron", "She", "her",
            "Starched veil, a ring of keys, a face no one has seen. She still does her rounds."),
        Victim = "explorer", Victims = "explorers",
        RoundWord = "round",
        DeckName = "Ward card",
        PhaseBanner = "THE LIGHTS FLICKER",
        FinaleName = "Lights Out",
        FinaleFlavour = "Every light in St Agnes dies at once. Her keys are coming for you.",
        FinaleTakes = "The dark swallows St Agnes",
        FinaleBannerSub = "The wards go dark. Now she hunts only you.",
        ClaimText = "takes {0} down to the basement",
        MoveVerb = "walks", LingerVerb = "waits",
        RiseText = "steps into the dark and walks out again",
        NobodyThere = "She checks the beds, finds them empty, and is gone again.",
        BarredRise = "Keys rattle at your chained door, but none of them fit.",
        BarredDoor = "She tries the chained door. It holds.",
        BarricadeText = "You chain the door shut.",
        LureText = "You knock on the pipes.",
        RepelText = "She reaches for you, then hisses away from the holy water.",
        RepelUse = "She recoils from the holy water.",
        WinText = "The Matron's keys fall to the floor. The wards are quiet at last.",
        LoseText = "The door clicks shut behind you. St Agnes has a new patient.",
        FogName = "Power cut",
        Locations =
        [
            new(0, "Gatehouse", "Rusted gates, and the only way out. Explorers brought here are safe.", 400, 488, false, true, 0, [ItemKind.FlareGun]),
            new(1, "Courtyard", "A dry fountain. Every window looks down on it.", 400, 320, false, false, 1, [ItemKind.Rope]),
            new(2, "Chapel", "Pews in rows. The organ plays when no one is there.", 150, 385, false, false, 1, [ItemKind.BlessedSalt, ItemKind.Bandages]),
            new(3, "East Ward", "Iron beds in rows. Some of the sheets are still warm.", 650, 320, false, false, 2, [ItemKind.GuttingKnife, ItemKind.Bandages, ItemKind.Lantern]),
            new(4, "West Ward", "Peeling paint, and names scratched into the walls.", 150, 165, false, false, 2, [ItemKind.IronNails, ItemKind.BlessedSalt]),
            new(5, "Records Office", "Filing cabinets to the ceiling. Every patient, every night.", 400, 130, false, false, 1, [ItemKind.Bandages, ItemKind.IronNails]),
            new(6, "Boiler House", "The boilers have been cold for fifty years. They're warm now.", 655, 140, true, false, 0, [ItemKind.BoatHook]),
            new(7, "Morgue", "Steel drawers. One of them is open.", 655, 488, true, false, 1, [ItemKind.GuttingKnife, ItemKind.FlareGun]),
        ],
        Edges = [(0, 1), (0, 2), (0, 7), (1, 2), (1, 3), (1, 5), (2, 4), (4, 5), (5, 6), (6, 3), (3, 7)],
        SanctuaryId = 0,
        KillerStart = 6,
        SummonId = 2,
        StartDread = 2,
        BloodlustTrack =
        [
            new(1, 1),
            new(1, 1, BloodlustEffect.DreadUp),
            new(1, 2, BloodlustEffect.DreadUp),
            new(1, 2, BloodlustEffect.DreadUp),
            new(1, 2, BloodlustEffect.DreadUp),
            new(2, 2, BloodlustEffect.Frenzy),
            new(2, 2, BloodlustEffect.DreadUp),
            new(2, 3, BloodlustEffect.DreadUp),
            new(2, 3, BloodlustEffect.DreadUp),
            new(2, 3, BloodlustEffect.DreadUp),
            new(2, 3),
        ],
        ItemNames = ["Fire Axe", "Crowbar", "Torch", "Master Key", "Road Flare", "First-Aid Kit", "Holy Water", "Chain & Padlock"],
        EventNames =
        [
            ("The Night Nurse", "\"I know these halls. Follow me.\""),
            ("Sedated", "She can walk, barely, if someone holds her up."),
            ("Smelling Salts", "A sharp breath and the fog in your head lifts."),
            ("Dispensary", "The locks were rusted through years ago."),
            ("Old Photograph", "Staff and patients, smiling. Some of them got out."),
            ("Night Shift", "No sleep, no rest, no stopping now."),
            ("Restraint Bed", "Leather straps that still buckle tight."),
            ("Voices in the Wards", "Another group came in through the boiler room."),
            ("The Bell Rings", "Lights out. Everyone back to their rooms."),
            ("Fuse Box Blows", "The emergency lights flicker and die."),
            ("Rounds", "Her keys jingle down the corridor, coming your way."),
            ("Admitted", "Another name added to her ledger."),
        ],
        TideNames =
        [
            ("Footsteps Above", "Heels on the floor above you. Slow. Patient."),
            ("Keys Jangling", "A ring of keys, somewhere down the corridor."),
            ("Lullaby", "She hums a lullaby through the vents."),
            ("Unlocked", "Every door on the ward clicks open."),
            ("Power Cut", "The lights die. Somewhere, a generator coughs."),
            ("A Straggler", "An explorer runs out of a side door, still filming."),
            ("The Organ", "One long note from the chapel organ, and she answers."),
            ("Visiting Hours", "She knows exactly where you are."),
            ("Generator Hum", "The lights steady. A moment to breathe."),
            ("Restraints", "A leather strap tightens round your wrist."),
            ("Night Rounds", "She is doing her rounds, and she is thorough."),
            ("Patient Records", "She knows where everyone is hiding. It's all in the files."),
            ("Pigeons Scatter", "The pigeons in the rafters burst out all at once."),
            ("Cold Hands", "A cold hand on your shoulder. Then nothing."),
            ("Quiet Ward", "She waits in the dark, and the dark mends her."),
            ("Static", "Every old speaker in the building hisses at once."),
            ("Empty Wheelchair", "A wheelchair rolls down the corridor by itself."),
            ("Down in the Basement", "Pipes knock all the way down to the basement."),
        ],
    };

    // ================================================================== Larkspur Lane (dream-stalker suburb)

    public static ScenarioDef Larkspur { get; } = new()
    {
        Id = "larkspur",
        Title = "Larkspur Lane",
        Place = "Larkspur Lane",
        Tagline = "Whatever you do, don't fall asleep.",
        Blurb = "A quiet street where the teenagers have stopped sleeping. Those who drift off don't wake up. You've been awake for three nights, and you have a bicycle.",
        Intro = "Larkspur Lane, the third night without sleep. Get the sleepers to the All-Night Diner, and silence the Lullaby Man.",
        Survivor = new("Maya Castillo", "Maya", "Insomniac",
            "Hasn't slept in three nights. Knows that's the only reason she's still alive."),
        Killer = new("The Lullaby Man", "Lullaby Man", "He", "him",
            "A porcelain moon for a face, a nightgown the colour of dust, and a voice like a music box winding down. He only comes when you close your eyes.", HealthBonus: 6),
        Victim = "sleeper", Victims = "sleepers",
        RoundWord = "dream",
        DeckName = "Dream card",
        PhaseBanner = "YOUR EYELIDS GROW HEAVY",
        FinaleName = "Deep Sleep",
        FinaleFlavour = "You can't stay awake any longer. The street melts like wax, and he is waiting.",
        FinaleTakes = "The whole street falls into the dream",
        FinaleBannerSub = "You've fallen asleep. Now he hunts only you.",
        ClaimText = "sings {0} into a sleep they won't wake from",
        MoveVerb = "strolls", LingerVerb = "hums to himself",
        RiseText = "steps out of a wardrobe",
        NobodyThere = "He tucks in empty beds, and fades.",
        BarredRise = "The wardrobe door rattles, but the wedged chair holds.",
        BarredDoor = "He knocks, softly, politely. The door stays shut.",
        BarricadeText = "You jam a chair under the handle.",
        LureText = "You hum his tune, badly.",
        RepelText = "The alarm clock shrieks and he reels away, hands over his ears.",
        RepelUse = "You set off the alarm clock; he flinches from the noise.",
        WinText = "The music box winds down to silence. Larkspur Lane wakes up.",
        LoseText = "Your eyes close. Somewhere, a music box starts to play.",
        FogName = "Drowsiness",
        Locations =
        [
            new(0, "All-Night Diner", "Neon, black coffee and a jukebox. Sleepers brought here stay awake, and safe.", 400, 490, false, true, 0, [ItemKind.FlareGun]),
            new(1, "Your House", "Every light on. Your bike leans against the garage door.", 400, 318, false, false, 1, [ItemKind.Vehicle, ItemKind.Bandages]),
            new(2, "Larkspur High", "Lockers hanging open. A school bell that rings at 3am.", 150, 385, false, false, 2, [ItemKind.BoatHook, ItemKind.Lantern]),
            new(3, "Sleep Clinic", "Monitors beeping for patients who aren't there.", 650, 318, false, false, 2, [ItemKind.Bandages, ItemKind.BlessedSalt, ItemKind.Key]),
            new(4, "Playground", "The swings move with no wind. A nursery rhyme drifts over the sandpit.", 150, 160, true, false, 1, [ItemKind.IronNails]),
            new(5, "Neighbour's House", "A music box on the mantelpiece. It's already open.", 400, 128, false, false, 1, [ItemKind.GuttingKnife, ItemKind.Rope]),
            new(6, "Drained Pool", "Cracked tiles, dead leaves, and a ladder down into the deep end.", 655, 140, true, false, 0, [ItemKind.BlessedSalt]),
            new(7, "Old Cinema", "The projector runs a film of your street, tonight.", 655, 488, false, false, 1, [ItemKind.Lantern, ItemKind.FlareGun]),
        ],
        Edges = [(0, 1), (0, 2), (0, 7), (1, 2), (1, 3), (1, 5), (2, 4), (4, 5), (5, 6), (3, 6), (3, 7)],
        LockedEdges = [(2, 4)],
        KeyName = "Janitor's Key",
        VehicleName = "Bicycle", VehicleRange = 2, VehicleVerb = "cycle", VehicleSound = "drive_bike",
        SanctuaryId = 0,
        KillerStart = 6,
        SummonId = 5,
        StartDread = 2,
        BloodlustTrack =
        [
            new(1, 1),
            new(1, 1, BloodlustEffect.DreadUp),
            new(2, 1, BloodlustEffect.DreadUp),
            new(2, 2, BloodlustEffect.DreadUp),
            new(2, 2, BloodlustEffect.DreadUp),
            new(2, 2, BloodlustEffect.Frenzy),
            new(3, 2, BloodlustEffect.DreadUp),
            new(3, 2, BloodlustEffect.DreadUp),
            new(3, 3, BloodlustEffect.DreadUp),
            new(3, 3, BloodlustEffect.DreadUp),
            new(3, 3),
        ],
        ItemNames = ["Baseball Bat", "Kitchen Knife", "Flashlight", "Garden Hose", "Road Flare", "First-Aid Kit", "Alarm Clock", "Chair Wedge"],
        EventNames =
        [
            ("Best Friend", "\"Maya, I'll keep you awake. I swear. Just don't leave me.\""),
            ("Sleepwalker", "Eyes open, seeing nothing. She has to be guided every step."),
            ("Black Coffee", "Bitter, scalding and wonderful."),
            ("Dad's Toolbox", "Somebody left the garage open."),
            ("You Remember", "It's a dream. Dreams have rules. You can break them."),
            ("Caffeine Pills", "Your heart races. Sleep feels very far away."),
            ("Tripwire", "Fishing line and an old cowbell, strung across the doorway."),
            ("Slumber Party", "Five kids in sleeping bags, two streets over, all yawning."),
            ("Microsleep", "You blink, and lose a minute. Where did it go?"),
            ("Street Lights Out", "One by one, down the whole street."),
            ("He Knows Your Name", "Sung softly, in the tune of a lullaby."),
            ("Sweet Dreams", "Somebody on the next street stopped fighting it."),
        ],
        TideNames =
        [
            ("Goodnight Kiss", "A cold breath on a sleeping cheek."),
            ("Counting Sheep", "One, two, three. He knows where every sleeper lies."),
            ("Your Name, Sung", "Your name, to the tune of a lullaby, from right behind you."),
            ("Bedtime Story", "\"Once upon a time, there were two little children...\""),
            ("Heavy Eyelids", "Everything is soft and slow and far away."),
            ("Sleepwalker", "A kid in pyjamas wanders out into the street."),
            ("The Music Box", "Next door, the music box starts to play by itself."),
            ("Sweet Dreams", "He's decided it's your turn."),
            ("Dawn Chorus", "A bird sings. For a moment, you're wide awake."),
            ("Hush Now", "A long finger presses to porcelain lips."),
            ("Nightcap", "He is hungry tonight."),
            ("Where the Children Sleep", "He steps out of the dark wherever the sleepers huddle."),
            ("Night Light", "A sleeper stumbles out, looking for the light."),
            ("Yawn", "The yawn comes from somewhere deep. You can't stop it."),
            ("Lullaby", "He sits on the end of a bed and sings, and his cracks close."),
            ("Pillow Fog", "The air is thick and soft as a pillow."),
            ("Tuck In", "Softly, softly, closer and closer."),
            ("Lights Out", "Somewhere a nursery rhyme starts again from the top."),
        ],
    };

    // ================================================================== Kestrel Motel (cursed broadcast)

    public static ScenarioDef Kestrel { get; } = new()
    {
        Id = "kestrel",
        Title = "Kestrel Motel",
        Place = "the Kestrel Motel",
        Tagline = "Please stand by. Please stand by.",
        Blurb = "A roadside motel on an empty highway, where every television picks up a channel that isn't there. You watched it. You have until sign-off. There's a station wagon at the drive-in.",
        Intro = "The Kestrel Motel, the night the signal came. Get the guests to the Gas Station, and take the Presenter off the air.",
        Survivor = new("Ruth Abernathy", "Ruth", "Local reporter",
            "Came to cover a story about a pirate TV signal. Watched the broadcast. Has until sign-off."),
        Killer = new("The Presenter", "Presenter", "He", "him",
            "A tall man in a grey suit and a wide old tie, holding a studio microphone. Where his face should be there is only static, and sometimes, for a moment, your face.", HealthBonus: 6),
        Victim = "guest", Victims = "guests",
        RoundWord = "broadcast",
        DeckName = "Signal card",
        PhaseBanner = "EVERY SCREEN SWITCHES ON",
        FinaleName = "Sign-Off",
        FinaleFlavour = "Every screen at the Kestrel shows the same picture: you.",
        FinaleTakes = "Every set in the motel switches on at once",
        FinaleBannerSub = "The broadcast is ending. Now he hunts only you.",
        ClaimText = "pulls {0} into the static",
        MoveVerb = "walks", LingerVerb = "stands perfectly still",
        RiseText = "flickers onto a screen nearby, then steps out of it",
        NobodyThere = "The screen shows an empty room, then goes dark.",
        BarredRise = "The set in your room hisses and rolls, but you pulled its plug.",
        BarredDoor = "He knocks. The TV in your room hisses in time.",
        BarricadeText = "You yank the aerial cable and chain the door.",
        LureText = "You switch on a TV and turn up the volume.",
        RepelText = "His picture warps and rolls around the magnet, and he backs away.",
        RepelUse = "You shove the magnet at his screen; the picture smears and he staggers back.",
        WinText = "The screen cracks, the picture shrinks to a white dot, and the Kestrel goes quiet.",
        LoseText = "The picture fades in on a new face. Yours.",
        FogName = "Interference",
        Locations =
        [
            new(0, "Gas Station", "One working payphone and a sheriff on the way. Guests brought here are safe.", 400, 490, false, true, 0, [ItemKind.FlareGun]),
            new(1, "Motel Office", "A bell on the desk, a wall of keys, and a TV playing to nobody.", 400, 318, false, false, 2, [ItemKind.Rope, ItemKind.Bandages]),
            new(2, "Honeymoon Suite", "Heart-shaped bed, mirrored ceiling, and a TV that was off when you left.", 150, 385, false, false, 2, [ItemKind.GuttingKnife, ItemKind.BlessedSalt]),
            new(3, "Back Rooms", "A row of cheap rooms. One door hangs open, snow on the screen, someone crying in the bathroom.", 650, 318, false, false, 1, [ItemKind.Bandages, ItemKind.Lantern]),
            new(4, "Swimming Pool", "Green water, a rusty diving board and a deckchair facing the sky.", 150, 160, false, false, 1, [ItemKind.IronNails, ItemKind.BoatHook]),
            new(5, "Laundromat", "A row of tumbling machines. In every round window, static.", 400, 128, false, false, 1, [ItemKind.Key, ItemKind.BlessedSalt]),
            new(6, "Broadcast Tower", "Red lights blink at the top. The hum makes your teeth ache.", 655, 140, true, false, 0, [ItemKind.FlareGun]),
            new(7, "Drive-In", "One car left in the lot. The giant screen plays the test card.", 655, 488, true, false, 1, [ItemKind.Vehicle, ItemKind.Key]),
        ],
        Edges = [(0, 1), (0, 2), (0, 7), (1, 2), (1, 3), (1, 5), (2, 4), (4, 5), (5, 6), (3, 6), (3, 7)],
        LockedEdges = [(1, 2), (1, 3)],
        KeyName = "Room Key",
        VehicleName = "Station Wagon", VehicleRange = 3, VehicleVerb = "drive", VehicleSound = "drive_car",
        SanctuaryId = 0,
        KillerStart = 6,
        SummonId = 6,
        StartDread = 2,
        BloodlustTrack =
        [
            new(1, 1),
            new(1, 1, BloodlustEffect.DreadUp),
            new(1, 2, BloodlustEffect.DreadUp),
            new(2, 2, BloodlustEffect.DreadUp),
            new(2, 2, BloodlustEffect.DreadUp),
            new(2, 2, BloodlustEffect.Frenzy),
            new(2, 2, BloodlustEffect.DreadUp),
            new(2, 3, BloodlustEffect.DreadUp),
            new(3, 3, BloodlustEffect.DreadUp),
            new(3, 3, BloodlustEffect.DreadUp),
            new(3, 3),
        ],
        ItemNames = ["Tyre Iron", "Steak Knife", "Flashlight", "Extension Cord", "Road Flare", "First-Aid Kit", "Horseshoe Magnet", "Door Chain"],
        EventNames =
        [
            ("The Night Clerk", "\"I've seen it too. I know where it goes when it's not on screen.\""),
            ("Watched It Twice", "He can't stop shaking. He can barely stand."),
            ("Vending Machine", "It drops a can of soda and a candy bar, free."),
            ("Lost Property", "A box behind the front desk, full of other people's things."),
            ("Dead Air", "For one whole minute, every screen shows nothing."),
            ("Station Coffee", "Burnt, bitter and very, very strong."),
            ("Rewired Socket", "Bare wires, and a puddle in front of the door."),
            ("Tour Bus", "A coach pulls in for the night. Its passengers have no idea."),
            ("Your Face on Screen", "On every set in the lot, for one long second."),
            ("Power Cut", "The neon sign dies. Only the TVs keep glowing."),
            ("He's Tuned In", "Your picture, on his screen, getting closer."),
            ("Channel Change", "A guest in another room just changed the channel."),
        ],
        TideNames =
        [
            ("Test Card", "A tone, a pattern of colour bars, and footsteps."),
            ("Now Showing", "Every set in the motel shows the same room."),
            ("Your Face", "His screen shows you, from behind."),
            ("Late Night Movie", "The film is about the guests. It doesn't end well."),
            ("Snow", "Static fills the air like falling snow."),
            ("Check-In", "A car pulls in. A tired family heads for the office."),
            ("Tower Light", "The red light on the tower stops blinking and stays on."),
            ("Tuning In", "He has found your frequency."),
            ("Commercial Break", "Thirty seconds of a jingle about orange juice. Breathe."),
            ("Close-Up", "His screen fills with your eye."),
            ("Prime Time", "Ratings are everything tonight."),
            ("Channel Surfing", "He steps out of whichever screen has the most people round it."),
            ("Vacancy", "Someone wanders over from the highway, looking for a room."),
            ("Bad Reception", "Your watch, your radio, your heartbeat, all wrong."),
            ("Rerun", "His cracked screen flickers, and the picture comes back whole."),
            ("Rolling Picture", "Everything slides up and up and up."),
            ("Broadcast", "The signal is stronger now."),
            ("Off the Air", "Every screen goes black at once."),
        ],
    };

    // ================================================================== Ashgrove House (the collector's puzzle house)

    public static ScenarioDef Ashgrove { get; } = new()
    {
        Id = "ashgrove",
        Title = "Ashgrove House",
        Place = "Ashgrove House",
        Tagline = "Some things should stay locked.",
        Blurb = "A sealed country house full of locked doors. You were valuing the contents when you opened a little lacquered cabinet. Every door in the house locked itself, and somebody started collecting.",
        Intro = "Ashgrove House, the night the cabinet opened. Get the heirs to the Foyer, find the keys, and close the Curator's collection for good.",
        Survivor = new("Clara Voss", "Clara", "Estate valuer",
            "Hired to value the contents of Ashgrove. Opened a lacquered cabinet of seven tiny drawers. Really wishes she hadn't."),
        Killer = new("The Curator", "Curator", "He", "him",
            "Tall and gaunt, in a velvet coat the colour of old wine. His face is porcelain, cracked and mended with gold. He collects people.", HealthBonus: 4),
        Victim = "heir", Victims = "heirs",
        RoundWord = "chime",
        DeckName = "Exhibit card",
        PhaseBanner = "EVERY CLOCK STRIKES",
        FinaleName = "Final Exhibit",
        FinaleFlavour = "The house folds in on itself. Every corridor leads to his gallery.",
        FinaleTakes = "The house swallows the rest of the family",
        FinaleBannerSub = "The house has closed. Now he hunts only you.",
        ClaimText = "adds {0} to his collection",
        MoveVerb = "walks", LingerVerb = "admires the room",
        RiseText = "steps out from behind a portrait",
        NobodyThere = "He finds only dust sheets, and is gone.",
        BarredRise = "A portrait shudders on the wall, but the wedged door holds.",
        BarredDoor = "He tries the handle, once, and smiles.",
        BarricadeText = "You wedge the door with a candlestick.",
        LureText = "You slide open one of the cabinet's tiny drawers.",
        RepelText = "The silver bell rings, clear and bright, and he cannot abide it.",
        RepelUse = "You ring the silver bell; he flinches from the sound.",
        WinText = "The last tiny drawer clicks shut. Ashgrove is only a house again.",
        LoseText = "A new portrait hangs in the gallery. It looks a lot like you.",
        FogName = "Dust",
        Locations =
        [
            new(0, "Foyer", "The front doors stand open on the night. Heirs brought here can run for the gates.", 400, 490, false, true, 0, [ItemKind.Key]),
            new(1, "Grand Hall", "A cold fireplace and a staircase that has too many steps.", 400, 318, false, false, 2, [ItemKind.Rope, ItemKind.Bandages]),
            new(2, "Library", "Ladders on rails, and a book that keeps falling open at the same page.", 150, 385, false, false, 2, [ItemKind.Lantern, ItemKind.IronNails]),
            new(3, "Dining Room", "Places laid for thirteen. Twelve chairs.", 650, 318, false, false, 2, [ItemKind.GuttingKnife, ItemKind.Key]),
            new(4, "Conservatory", "Glass, dead orchids and a fountain full of black water.", 150, 160, false, false, 0, [ItemKind.BlessedSalt, ItemKind.Bandages]),
            new(5, "Gallery", "Portraits of the family. Some of them are new.", 400, 128, false, false, 0, [ItemKind.FlareGun, ItemKind.BoatHook]),
            new(6, "Attic", "Dust sheets over shapes that are almost people.", 655, 140, true, false, 0, [ItemKind.BlessedSalt, ItemKind.FlareGun]),
            new(7, "Cellar", "Wine racks, a furnace, and a door that wasn't there yesterday.", 655, 488, true, false, 1, [ItemKind.Bandages, ItemKind.Key]),
        ],
        Edges = [(0, 1), (0, 2), (0, 7), (1, 2), (1, 3), (1, 5), (2, 4), (4, 5), (5, 6), (3, 6), (3, 7)],
        LockedEdges = [(1, 5), (2, 4), (3, 6)],
        KeyName = "Brass Key",
        SanctuaryId = 0,
        KillerStart = 7,
        SummonId = 5,
        StartDread = 2,
        BloodlustTrack =
        [
            new(1, 1),
            new(1, 2, BloodlustEffect.DreadUp),
            new(1, 2, BloodlustEffect.DreadUp),
            new(2, 2, BloodlustEffect.DreadUp),
            new(2, 2, BloodlustEffect.DreadUp),
            new(2, 2, BloodlustEffect.Frenzy),
            new(2, 3, BloodlustEffect.DreadUp),
            new(2, 3, BloodlustEffect.DreadUp),
            new(2, 3, BloodlustEffect.DreadUp),
            new(3, 3, BloodlustEffect.DreadUp),
            new(3, 3),
        ],
        ItemNames = ["Fire Poker", "Letter Opener", "Candelabra", "Bell Rope", "Flintlock Pistol", "Smelling Salts", "Silver Bell", "Door Wedge"],
        EventNames =
        [
            ("The Youngest Heir", "\"I used to play hide and seek here. I know every passage.\""),
            ("Twisted Ankle", "She fell on the stairs. They moved."),
            ("Brandy", "A crystal decanter, still half full."),
            ("Hidden Drawer", "A click, and the panelling slides aside."),
            ("Grandfather's Letter", "\"If you are reading this, it has opened. Ring the bell.\""),
            ("Second Wind", "Fear turns to anger. Anger turns to speed."),
            ("Mantrap", "An antique, but it still works."),
            ("Late Arrivals", "Three more cousins, come for the reading of the will."),
            ("The Clock Stops", "Every clock in the house stops at the same second."),
            ("Candles Gutter", "The draught comes from inside the walls."),
            ("He Has Your Measure", "He has seen you. He wants you for the collection."),
            ("A New Exhibit", "Another portrait appears in the gallery."),
        ],
        TideNames =
        [
            ("Acquisition", "Polished shoes on polished boards."),
            ("The Catalogue", "He turns a page. Every heir's name is written there."),
            ("Your Portrait", "Someone has begun painting your portrait."),
            ("The Reading of the Will", "\"To my family, I leave... myself.\""),
            ("Dust Sheets", "The air is grey with dust."),
            ("Unexpected Guest", "A cousin wanders in, lost in the corridors."),
            ("The Gallery", "A new frame on the gallery wall. It's empty, for now."),
            ("Rare Specimen", "He's decided you would complete the set."),
            ("Morning Room", "Pale light through the curtains. A moment's peace."),
            ("Glass Case", "Your reflection in the glass isn't alone."),
            ("Private Collection", "He is not a patient collector tonight."),
            ("Viewing", "He steps out of a portrait wherever the family gathers."),
            ("Lost in the House", "A heir stumbles out of a corridor that wasn't there before."),
            ("Draught", "Cold air from the walls. Your fingers stiffen."),
            ("Restoration", "He sits, and mends the gold seam in his face."),
            ("Candlelight", "The candles all gutter at once."),
            ("Provenance", "He knows where every heir came from, and where they'll go."),
            ("Into the Walls", "He steps back into the panelling and is gone."),
        ],
    };

    // ================================================================== Cedar Hollow (phone-stalker slasher)

    public static ScenarioDef CedarHollow { get; } = new()
    {
        Id = "cedarhollow",
        Title = "Cedar Hollow",
        Place = "Cedar Hollow",
        Tagline = "Don't answer the phone.",
        Blurb = "A sleepy town in October. Every one of your friends has had the same call tonight, and a masked caller who knows all the rules of a scary movie is working through the list. Somebody left the keys in a pickup.",
        Intro = "Cedar Hollow, the night of the calls. Get your friends to the Sheriff's Station, and unmask the Caller.",
        Survivor = new("Jess Morrow", "Jess", "High-school senior",
            "Got the first call. Hung up. It rang straight back."),
        Killer = new("The Caller", "Caller", "He", "him",
            "A letterman jacket, a smooth white mask with two thin eye slits and no other features, a cordless phone in one hand and a kitchen knife in the other. He loves to talk.", HealthBonus: 9),
        Victim = "friend", Victims = "friends",
        RoundWord = "call",
        DeckName = "Call card",
        PhaseBanner = "THE PHONE IS RINGING",
        FinaleName = "Final Act",
        FinaleFlavour = "Every phone in Cedar Hollow rings at once. It's for you.",
        FinaleTakes = "The lines go dead all over town",
        FinaleBannerSub = "The last act. Now he hunts only you.",
        ClaimText = "drags {0} out of sight",
        MoveVerb = "walks", LingerVerb = "waits, breathing down the line",
        RiseText = "steps out of a closet",
        NobodyThere = "He checks an empty house, and hangs up.",
        BarredRise = "The closet door bangs, but the chair under the handle holds.",
        BarredDoor = "He tries the door, then calls you to say he'll wait.",
        BarricadeText = "You shove a dresser against the door.",
        LureText = "You call his number back.",
        RepelText = "The air horn blasts in his face and he reels away.",
        RepelUse = "You let off the air horn; he staggers back, clutching his ears.",
        WinText = "The mask comes off. Sirens, at last, all over Cedar Hollow.",
        LoseText = "The phone rings one last time. Nobody answers.",
        FogName = "Power cut",
        Locations =
        [
            new(0, "Sheriff's Station", "A deputy, a radio and a door that locks. Friends brought here are safe.", 400, 490, false, true, 0, [ItemKind.FlareGun]),
            new(1, "Main Street", "Diner shut, cinema dark, a payphone ringing on the corner.", 400, 318, false, false, 2, [ItemKind.Rope, ItemKind.Bandages]),
            new(2, "Video Store", "Rows of horror tapes. The clerk knows how these films end.", 150, 385, false, false, 1, [ItemKind.Lantern, ItemKind.BlessedSalt]),
            new(3, "High School", "Lockers, a gym, and one classroom light left on.", 650, 318, false, false, 2, [ItemKind.BoatHook, ItemKind.Key]),
            new(4, "Party House", "Music still playing, red cups everywhere, nobody in sight.", 150, 160, false, false, 2, [ItemKind.Bandages, ItemKind.GuttingKnife]),
            new(5, "Your House", "Every door locked. The phone in the hall keeps ringing.", 400, 128, false, false, 0, [ItemKind.FlareGun, ItemKind.IronNails]),
            new(6, "Old Barn", "Hay, a rope swing and tyre tracks in the mud.", 655, 140, true, false, 0, [ItemKind.BlessedSalt]),
            new(7, "Football Field", "Floodlights humming over an empty pitch.", 655, 488, true, false, 1, [ItemKind.Vehicle, ItemKind.Lantern]),
        ],
        Edges = [(0, 1), (0, 2), (0, 7), (1, 2), (1, 3), (1, 4), (2, 4), (4, 5), (5, 6), (3, 6), (3, 7)],
        LockedEdges = [(4, 5)],
        KeyName = "Spare Key",
        VehicleName = "Pickup Truck", VehicleRange = 3, VehicleVerb = "drive", VehicleSound = "drive_car",
        SanctuaryId = 0,
        KillerStart = 6,
        SummonId = 5,
        StartDread = 2,
        BloodlustTrack =
        [
            new(1, 1),
            new(1, 1, BloodlustEffect.DreadUp),
            new(2, 1, BloodlustEffect.DreadUp),
            new(2, 2, BloodlustEffect.DreadUp),
            new(2, 2, BloodlustEffect.DreadUp),
            new(2, 2, BloodlustEffect.Frenzy),
            new(2, 2, BloodlustEffect.DreadUp),
            new(3, 2, BloodlustEffect.DreadUp),
            new(3, 3, BloodlustEffect.DreadUp),
            new(3, 3, BloodlustEffect.DreadUp),
            new(3, 3),
        ],
        ItemNames = ["Fire Poker", "Kitchen Knife", "Flashlight", "Extension Cord", "Flare Gun", "First-Aid Kit", "Air Horn", "Dresser"],
        EventNames =
        [
            ("Best Friend", "\"Jess, I'm right behind you. Don't you dare die before the sequel.\""),
            ("Twisted Ankle", "She tripped running from the party. She can't put weight on it."),
            ("Pizza Delivery", "Somebody ordered hours ago. Still warm. Still free."),
            ("Deputy's Bag", "A patrol car with its door open and its trunk unlocked."),
            ("The Rules", "You know how these films go. You know what not to do."),
            ("Adrenaline", "Heart pounding, legs burning, wide awake."),
            ("Trip Wire", "Fishing line across the hallway, and a lot of pots and pans."),
            ("Second Party", "Half the senior class, two streets over, music up loud."),
            ("Caller ID: Unknown", "The phone rings. You know who it is."),
            ("Storm Knocks the Lines Out", "The street lights die, one by one."),
            ("He's Inside the House", "The call is coming from somewhere close. Very close."),
            ("Another Name Crossed Off", "The list is getting shorter."),
        ],
        TideNames =
        [
            ("Ring Ring", "A phone rings in an empty house."),
            ("Favourite Scary Movie", "\"Tell me your favourite scary movie.\""),
            ("I Can See You", "\"Nice jacket. Turn around.\""),
            ("Wrong Answer", "A trivia question, and a wrong answer."),
            ("Power Cut", "The whole block goes dark."),
            ("Party Crasher", "A kid in a costume stumbles out into the street."),
            ("Call From Your House", "Your own number lights up on the screen."),
            ("Behind You", "Not on the line any more. Behind you."),
            ("Dial Tone", "He's hung up. For now."),
            ("Hold the Line", "\"Don't hang up. I'm almost there.\""),
            ("Body Count", "He's having fun tonight."),
            ("Crowd Pleaser", "He steps out wherever the most friends have gathered."),
            ("Last Bus Home", "Someone climbs off the night bus, alone."),
            ("Heavy Breathing", "Your hands won't stop shaking."),
            ("Commercial Break", "He stops, catches his breath, laughs at his own joke."),
            ("Sprinklers", "Every lawn sprinkler comes on at once in the dark."),
            ("Sequel", "There's always one more."),
            ("Hang Up", "The line goes dead."),
        ],
    };

    // ================================================================== Holloway Funfair (killer clown, Halloween)

    public static ScenarioDef Holloway { get; } = new()
    {
        Id = "holloway",
        Title = "Holloway Funfair",
        Place = "Holloway Funfair",
        Tagline = "The fun never stops. Ever.",
        Blurb = "Halloween at a travelling funfair after closing. The rides start by themselves, and a silent clown with a mallet wants everyone to stay for one more turn. The maintenance golf cart still has some charge.",
        Intro = "Holloway Funfair, Halloween, after closing. Get the stragglers to the Front Gate, and stop the Merrymaker's last show.",
        Survivor = new("Tasha Greene", "Tasha", "Ride operator",
            "Was closing up the waltzer. Saw a clown that wasn't on the staff list."),
        Killer = new("The Merrymaker", "Merrymaker", "He", "him",
            "A tall, silent clown in a mustard-and-teal patchwork suit and a green bowler hat, face painted ash-grey with a yellow star round one eye. He drags a fairground strength-test mallet.", HealthBonus: -2),
        Victim = "straggler", Victims = "stragglers",
        RoundWord = "show",
        DeckName = "Showtime card",
        PhaseBanner = "THE CALLIOPE STARTS PLAYING",
        FinaleName = "Grand Finale",
        FinaleFlavour = "Every ride lights up at once and the music won't stop.",
        FinaleTakes = "The rides spin up with nobody left to ride them",
        FinaleBannerSub = "The last show. Now he hunts only you.",
        ClaimText = "takes {0} for one last ride",
        MoveVerb = "skips", LingerVerb = "juggles, silently",
        RiseText = "pops out of a prize booth",
        NobodyThere = "He finds an empty queue, and honks his horn sadly.",
        BarredRise = "The booth shutter rattles, but the padlock you snapped on holds.",
        BarredDoor = "He knocks to a jaunty rhythm. The door holds.",
        BarricadeText = "You pull down the shutter and padlock it.",
        LureText = "You honk a bike horn.",
        RepelText = "The fire extinguisher blasts him in a white cloud and he stumbles off.",
        RepelUse = "You blast him with the extinguisher; he reels away, coughing silently.",
        WinText = "The music winds down. The lights go out, ride by ride. The show is over.",
        LoseText = "The calliope plays on. There's a new face on the poster.",
        FogName = "Smoke machine",
        Locations =
        [
            new(0, "Front Gate", "The turnstiles, a ticket hut and the car park beyond. Stragglers brought here are safe.", 400, 490, false, true, 0, [ItemKind.Bandages]),
            new(1, "Midway", "Stalls, prizes and a cotton-candy machine still spinning.", 400, 318, false, false, 2, [ItemKind.Rope, ItemKind.FlareGun]),
            new(2, "Hall of Mirrors", "A hundred of you. Some of them move a little late.", 150, 385, false, false, 2, [ItemKind.Lantern, ItemKind.BlessedSalt]),
            new(3, "Ghost Train", "The cars rattle round on their own. Someone's screaming inside.", 650, 318, false, false, 2, [ItemKind.Bandages, ItemKind.Key]),
            new(4, "Big Top", "Sawdust, a spotlight and a single chair in the ring.", 150, 160, false, false, 1, [ItemKind.BoatHook, ItemKind.Vehicle]),
            new(5, "Ferris Wheel", "Turning slowly in the dark, a car swinging at the very top.", 400, 128, false, false, 1, [ItemKind.IronNails, ItemKind.GuttingKnife]),
            new(6, "Clown Caravans", "Behind the staff gate. Grease paint, wigs and a mirror with lights.", 655, 140, true, false, 0, [ItemKind.FlareGun, ItemKind.BlessedSalt]),
            new(7, "Funhouse", "A grinning mouth for a door, and floors that tilt.", 655, 488, true, false, 0, [ItemKind.GuttingKnife]),
        ],
        Edges = [(0, 1), (0, 2), (0, 7), (1, 2), (1, 3), (1, 5), (1, 4), (2, 4), (4, 5), (5, 6), (3, 6), (3, 7)],
        LockedEdges = [(3, 6), (5, 6)],
        KeyName = "Staff Key",
        VehicleName = "Golf Cart", VehicleRange = 2, VehicleVerb = "drive", VehicleSound = "drive_cart",
        SanctuaryId = 0,
        KillerStart = 7,
        SummonId = 5,
        StartDread = 2,
        BloodlustTrack =
        [
            new(1, 1),
            new(1, 2, BloodlustEffect.DreadUp),
            new(1, 2, BloodlustEffect.DreadUp),
            new(2, 2, BloodlustEffect.DreadUp),
            new(2, 2, BloodlustEffect.DreadUp),
            new(2, 2, BloodlustEffect.Frenzy),
            new(2, 3, BloodlustEffect.DreadUp),
            new(2, 3, BloodlustEffect.DreadUp),
            new(2, 3, BloodlustEffect.DreadUp),
            new(3, 3, BloodlustEffect.DreadUp),
            new(3, 3),
        ],
        ItemNames = ["Tent Mallet", "Box Cutter", "Head Torch", "Bunting Rope", "Firework Rocket", "First-Aid Kit", "Fire Extinguisher", "Ride Shutter"],
        EventNames =
        [
            ("The Fortune Teller", "\"I saw this in the cards, love. Stay close and I'll see us out.\""),
            ("Stuck on the Ride", "He jumped from the waltzer and landed badly. He needs an arm to lean on."),
            ("Hot Doughnuts", "The fryer's still on. Sugar and grease and a moment's warmth."),
            ("Lost and Found", "A crate behind the ticket hut, full of odd and useful things."),
            ("Last Ride Home", "You've closed this fair a hundred times. You know every shortcut."),
            ("Sugar Rush", "Candy floss and cola. Your heart is racing."),
            ("Rigged Game", "You loosen the bolts on the coconut shy. Something heavy will fall."),
            ("Trick or Treaters", "A gang of kids in costumes, sneaking in over the fence."),
            ("Honk Honk", "A horn sounds in the dark, close by. Then another, closer."),
            ("Generator Trips", "Half the fair goes black. The music doesn't stop."),
            ("You're in the Show", "Your name, chalked on the big top's board, top of the bill."),
            ("Encore", "Somewhere, the crowd applauds. There is no crowd."),
        ],
        TideNames =
        [
            ("Roll Up, Roll Up", "The calliope wheezes into life."),
            ("Pick a Prize", "He knows where every straggler is hiding."),
            ("Front Row Seat", "He waves at you. Only at you."),
            ("Double Act", "He mimes something awful, very slowly."),
            ("Smoke Machine", "Thick white smoke rolls across the Midway."),
            ("Lost Child", "A kid in a witch costume, crying for her mum."),
            ("Ferris Wheel Stops", "The wheel jerks to a halt with someone at the top."),
            ("Pie in the Face", "He's chosen his next volunteer: you."),
            ("Intermission", "The music stops. Just for a moment."),
            ("Strength Test", "The mallet swings and the bell rings at the top."),
            ("Showstopper", "He is enjoying himself enormously."),
            ("Crowd Favourite", "He pops up wherever the most stragglers huddle."),
            ("Ticket Holder", "Someone climbs back in over the fence for one more go."),
            ("Cold Night", "Your breath clouds and your fingers stiffen."),
            ("Fresh Greasepaint", "He touches up his star in a hand mirror, and his cuts close."),
            ("Dry Ice", "Fog pours off the Ghost Train."),
            ("Encore", "One more turn. Just one more."),
            ("Exit Through the Gift Shop", "He takes a bow and is gone."),
        ],
    };

    // ================================================================== Crowhurst Farm (the devoted reader, snowbound)

    public static ScenarioDef Crowhurst { get; } = new()
    {
        Id = "crowhurst",
        Title = "Crowhurst Farm",
        Place = "Crowhurst Farm",
        Tagline = "She only wants the ending to be perfect.",
        Blurb = "You crashed in a blizzard and woke up in a farmhouse, nursed by your most devoted reader. Tonight you finally got out of the spare room. Anyone she's 'looking after' needs to reach the lane before the snowplough passes. Every door in the house locks.",
        Intro = "Crowhurst Farm, the night of the blizzard. Get everyone she's keeping down to the Lane, and close the last chapter on the Devotee.",
        Survivor = new("Daniel Reyes", "Daniel", "Crime novelist",
            "Walks with a stick since the crash. Knows exactly how stories like this end, and is trying very hard to write a different one."),
        Killer = new("The Devotee", "Devotee", "She", "her",
            "Your most devoted reader. Neat grey bun, a long knitted cardigan and a sweet, patient smile. She keeps every key in the house, and a hatchet by the stove.", HealthBonus: -4),
        Victim = "visitor", Victims = "visitors",
        RoundWord = "chapter",
        DeckName = "Chapter card",
        PhaseBanner = "SHE'S COMING UP THE STAIRS",
        FinaleName = "The Last Chapter",
        FinaleFlavour = "The snowplough has gone by. Nobody is coming. She's very disappointed in you.",
        FinaleTakes = "The blizzard closes the lane",
        FinaleBannerSub = "No more visitors. Now she hunts only you.",
        ClaimText = "locks {0} away upstairs",
        MoveVerb = "walks", LingerVerb = "hums a hymn",
        RiseText = "is simply there, by the door, smiling",
        NobodyThere = "She tidies an empty room, and is gone.",
        BarredRise = "The door handle turns, but the chair under it holds.",
        BarredDoor = "She tries the door, then sighs through the keyhole.",
        BarricadeText = "You jam a chair under the handle.",
        LureText = "You knock over a vase. She hates mess.",
        RepelText = "You rip a page out of the manuscript and she freezes, horrified.",
        RepelUse = "You hold the manuscript over the fire; she backs away, pleading.",
        WinText = "The last chapter is yours. Blue lights at the end of the lane, at last.",
        LoseText = "She tucks you back into bed. You'll write it her way now.",
        FogName = "Whiteout",
        Locations =
        [
            new(0, "The Lane", "The snowplough comes by here. Visitors brought here can flag it down and get away.", 400, 490, false, true, 0, [ItemKind.FlareGun]),
            new(1, "Farmyard", "Knee-deep snow, a frozen water pump and her car with no tyres.", 400, 318, false, false, 2, [ItemKind.Rope, ItemKind.Key]),
            new(2, "Barn", "Hay, a tractor and the cows she lets nobody touch.", 150, 385, false, false, 2, [ItemKind.BoatHook, ItemKind.Lantern]),
            new(3, "Kitchen", "A warm stove, a pie cooling, and the hatchet missing from its hook.", 650, 318, false, false, 2, [ItemKind.Bandages, ItemKind.GuttingKnife]),
            new(4, "Chicken Coop", "Feathers everywhere, and fresh footprints in the snow.", 150, 160, true, false, 1, [ItemKind.IronNails]),
            new(5, "Parlour", "Your books on every shelf. Your photo on the mantelpiece.", 400, 128, false, false, 0, [ItemKind.BlessedSalt, ItemKind.Bandages, ItemKind.Key]),
            new(6, "Spare Room", "Where she kept you. The typewriter is still warm.", 655, 140, true, false, 0, [ItemKind.FlareGun, ItemKind.BlessedSalt]),
            new(7, "Woodshed", "Logs stacked to the roof, and a chopping block.", 655, 488, true, false, 1, [ItemKind.Lantern, ItemKind.Bandages]),
        ],
        Edges = [(0, 1), (0, 2), (0, 7), (1, 2), (1, 3), (1, 5), (2, 4), (4, 5), (5, 6), (3, 6), (3, 7)],
        LockedEdges = [(1, 5), (3, 6), (5, 6)],
        KeyName = "Iron Key",
        SanctuaryId = 0,
        KillerStart = 3,
        SummonId = 6,
        StartDread = 2,
        BloodlustTrack =
        [
            new(1, 1),
            new(1, 2, BloodlustEffect.DreadUp),
            new(1, 2, BloodlustEffect.DreadUp),
            new(1, 2, BloodlustEffect.DreadUp),
            new(2, 2, BloodlustEffect.DreadUp),
            new(2, 2, BloodlustEffect.Frenzy),
            new(2, 3, BloodlustEffect.DreadUp),
            new(2, 3, BloodlustEffect.DreadUp),
            new(2, 3, BloodlustEffect.DreadUp),
            new(2, 3, BloodlustEffect.DreadUp),
            new(2, 3),
        ],
        ItemNames = ["Walking Stick", "Carving Knife", "Storm Lantern", "Baling Twine", "Shotgun Shell", "Painkillers", "The Manuscript", "Chair"],
        EventNames =
        [
            ("The Deputy", "\"Sir? You're the writer, aren't you? Stay behind me.\""),
            ("Frostbitten", "She's been out in the snow for hours. She can barely walk."),
            ("Pot of Tea", "Still hot, and for once nobody has put anything in it."),
            ("Her Sewing Box", "Scissors, needles, thread, and something useful underneath."),
            ("Your Own Words", "You remember a chapter you wrote. Your hero got out. So can you."),
            ("Cold Air", "The freezing wind wakes you right up."),
            ("Loose Floorboard", "Prise it up, and leave a hole she won't see coming."),
            ("Stranded Drivers", "Two cars in the ditch at the bottom of the lane, headlights on."),
            ("She Read Ahead", "She knows what you're planning. She's read it before."),
            ("Power Lines Down", "The farmhouse goes dark. Only the stove glows."),
            ("Her Favourite Character", "She'd like you to stay. Forever, ideally."),
            ("A Rewrite", "She didn't like how that one ended."),
        ],
        TideNames =
        [
            ("Tea Time", "The kettle whistles in the kitchen."),
            ("Bed Check", "She counts the visitors, one by one."),
            ("Dear Author", "\"I'm your biggest admirer. You know that, don't you?\""),
            ("House Rules", "No mess. No noise. No leaving."),
            ("Whiteout", "Snow blows sideways across the yard."),
            ("Knock at the Door", "Someone stumbles up the lane out of the snow."),
            ("The Spare Room", "The typewriter starts clacking, all by itself."),
            ("Your Chapter", "She's decided it's your turn."),
            ("Snowplough", "Headlights, far off. A moment's hope."),
            ("Housekeeping", "She's very good at keeping house."),
            ("Disappointed", "She is so, so disappointed."),
            ("Visiting Hours", "She appears wherever the most visitors are."),
            ("Lost in the Snow", "Someone wanders in from the blizzard."),
            ("Frostbite", "Your fingers won't close."),
            ("Mending", "She sits by the stove and bandages her hands."),
            ("Snowfall", "Thick, soft, silent snow."),
            ("Second Draft", "She goes back to do it properly this time."),
            ("Upstairs", "Her footsteps creak away up the stairs."),
        ],
    };

    // ================================================================== Merriwick (a small town, Christmas Eve, a multiplying brood)

    public static ScenarioDef Merriwick { get; } = new()
    {
        Id = "merriwick",
        Title = "Merriwick",
        Place = "Merriwick",
        Tagline = "Something in the toy shop is multiplying.",
        Blurb = "Christmas Eve in a little market town. A crate from a house clearance was opened in the toy-shop cellar, and something was nesting in it. Now the snow is full of small, quick, soot-black things, and their mother is still growing. Get the late shoppers into the church, where the bells keep them back.",
        Intro = "Merriwick, Christmas Eve. Lead the shoppers to St Hilda's Church, keep the sootlings off you, and put the Hatchmother down for good.",
        Survivor = new("Billie Hart", "Billie", "Toy-shop assistant",
            "Seventeen Christmases behind the counter. Knows every shortcut, every shopkeeper and every alley in town. Opened the crate."),
        Killer = new("The Hatchmother", "Hatchmother", "She", "her",
            "Wardrobe-tall and hunched, matted with soot-black fur, with a ring of round, tin-bright eyes. She drags her swollen nest behind her through the snow, and every time the town clock chimes, more of her little ones climb out.", HealthBonus: -3),
        Victim = "shopper", Victims = "shoppers",
        RoundWord = "chime",
        DeckName = "Chime card",
        PhaseBanner = "THE CLOCK CHIMES",
        FinaleName = "Midnight Mass",
        FinaleFlavour = "The church doors close for midnight. The streets belong to the brood now.",
        FinaleTakes = "The church doors bolt for midnight",
        FinaleBannerSub = "No more shoppers. Now the whole brood hunts only you.",
        ClaimText = "drags {0} down into the cellar",
        MoveVerb = "scuttles", LingerVerb = "broods over her nest",
        RiseText = "bursts up out of a drain in a shower of soot",
        NobodyThere = "She snuffles through an empty shop, and is gone.",
        BarredRise = "Claws scrape at the shutter, but it holds.",
        BarredDoor = "She throws herself at the shutters, and they hold.",
        BarricadeText = "You haul the shop shutters down with a crash.",
        LureText = "You set off a shop alarm. Every eye in the brood turns towards it.",
        RepelText = "The camera flash goes off in her eyes and she shrieks, blinded.",
        RepelUse = "You fire the flash right in her face; she reels away, clawing at her eyes.",
        WinText = "She bursts into soot and cinders, and the whole brood with her. The church bells ring in Christmas morning.",
        LoseText = "The brood closes over you, and the snow covers everything.",
        FogName = "Snowstorm",
        SwarmName = "sootling",
        SwarmNames = "sootlings",
        SwarmStart = 1,
        SwarmSpawn = 1,
        SwarmMax = 4,
        SwarmBite = "nip and scratch at you",
        Locations =
        [
            new(0, "St Hilda's Church", "Candles, carols and bells. The brood won't come in while the bells ring.", 400, 490, false, true, 0, [ItemKind.FlareGun]),
            new(1, "Market Square", "A tall tree strung with lights, and a milk float abandoned mid-round.", 400, 318, false, false, 2, [ItemKind.Rope, ItemKind.Vehicle]),
            new(2, "Toy Shop", "Where it started. Wrapping paper everywhere, and a broken crate in the cellar.", 650, 140, true, false, 0, [ItemKind.FlareGun, ItemKind.BlessedSalt]),
            new(3, "Picture House", "The Christmas matinee is still running to an empty, scratching audience.", 150, 160, true, false, 1, [ItemKind.IronNails, ItemKind.Lantern]),
            new(4, "The Holly Bush", "A pub with a roaring fire, and a darts board nobody's using.", 150, 385, false, false, 2, [ItemKind.BoatHook, ItemKind.Bandages]),
            new(5, "Bakery", "Mince pies on the racks, and flour footprints leading out the back.", 650, 318, false, false, 2, [ItemKind.GuttingKnife, ItemKind.Bandages]),
            new(6, "Department Store", "Four floors of Christmas, and the escalators are still running.", 400, 128, false, false, 1, [ItemKind.BlessedSalt, ItemKind.Bandages, ItemKind.Lantern]),
            new(7, "Ice Rink", "Fairy lights over the ice, and long, thin scratches across it.", 655, 488, false, false, 0, [ItemKind.IronNails, ItemKind.Bandages]),
        ],
        Edges = [(0, 1), (0, 4), (0, 7), (1, 4), (1, 5), (1, 6), (3, 4), (3, 6), (2, 6), (2, 5), (5, 7)],
        VehicleName = "Milk Float",
        VehicleRange = 2,
        VehicleVerb = "drive",
        VehicleSound = "drive_cart",
        SanctuaryId = 0,
        KillerStart = 2,
        SummonId = 3,
        BloodlustTrack =
        [
            new(1, 1),
            new(1, 1),
            new(1, 1, BloodlustEffect.DreadUp),
            new(1, 2, BloodlustEffect.DreadUp),
            new(2, 2, BloodlustEffect.DreadUp),
            new(2, 2, BloodlustEffect.Frenzy),
            new(2, 2, BloodlustEffect.DreadUp),
            new(2, 3, BloodlustEffect.DreadUp),
            new(2, 3, BloodlustEffect.DreadUp),
            new(2, 3, BloodlustEffect.DreadUp),
            new(2, 3),
        ],
        ItemNames = ["Cricket Bat", "Cake Knife", "Bike Lamp", "Fairy Lights", "Firework Rocket", "First Aid Kit", "Camera Flash", "Shop Shutters"],
        EventNames =
        [
            ("PC Okafor", "\"I've seen some things on Christmas Eve, but never this. Stay close.\""),
            ("Bitten Badly", "The sootlings got to him first. He can hardly walk."),
            ("Hot Chocolate", "A flask from the market stall, still steaming."),
            ("Lost Property", "The station's lost-property box: you never know what's in there."),
            ("Carol Singers", "Somewhere, a choir is still singing. You can do this."),
            ("Sugar Rush", "Three mince pies and a can of fizzy pop. You're wide awake."),
            ("Mousetrap", "A big old spring trap from the bakery. Set it where you stand."),
            ("Last Bus In", "The last bus from the city pulls in, full of shoppers."),
            ("Scratching in the Walls", "They're in the walls. They're everywhere."),
            ("Power Cut", "Every light on the high street goes out at once."),
            ("She's Picked Up Your Scent", "Every eye in the brood turns towards you."),
            ("Feeding Time", "She's hungry, and her nest is growing."),
        ],
        TideNames =
        [
            ("Window Shopping", "She presses her eyes to the glass, one shop after another."),
            ("Unwrapped", "A present on a doorstep is torn open from the inside."),
            ("Tin Eyes", "A ring of bright little eyes, all turned your way."),
            ("Last Orders", "The pub door swings in the wind."),
            ("Snowstorm", "The snow comes down so thick you can't see the tree."),
            ("Stragglers", "A family runs out of the department store, laughing, unaware."),
            ("Matinee", "The film flickers on in the Picture House. Something is watching it."),
            ("Hide and Seek", "She's playing a game, and you're it."),
            ("Gritter Lorry", "The gritter rumbles past, and she has to wait for it."),
            ("Up the Chimney", "Soot falls from the chimney beside you."),
            ("Wrapping Paper", "Ribbons and shredded paper skitter along the pavement."),
            ("Late-Night Opening", "She goes where the crowd is thickest."),
            ("Lost Child", "A little boy in a bobble hat, looking for his mum."),
            ("Cold Snap", "The cold bites through your gloves."),
            ("Nesting", "She curls up over her nest and licks her wounds."),
            ("Frost Fog", "Freezing fog rolls down the high street."),
            ("Twelve Days", "On the twelfth chime, something else wakes up."),
            ("Back to the Nest", "She scuttles home to her nest."),
        ],
    };
}
