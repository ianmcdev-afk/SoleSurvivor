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
    public IReadOnlyDictionary<ItemKind, ItemDef> Items => _items ??= new Dictionary<ItemKind, ItemDef>
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

    public static IReadOnlyList<ScenarioDef> All => [Gallowmouth, Whitepine, StAgnes];

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
            "Antlers like dead branches, a body of frozen bark and bone. It walks on two legs when no one is watching."),
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
}
