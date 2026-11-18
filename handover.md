# Sole Survivor: handover

## Status: v1.0.30 (Version in the window title)
- The window title now includes the version ("Sole Survivor 1.0.30: Merriwick (Normal)"), as well as the title screen. A small change, made to test that Setup and publishing upgrade the existing install in place.

## v1.0.29 (Publishing updates the installed game)
- New `tools/update-install.ps1`, run by the `UpdateInstalledCopy` MSBuild target (`AfterTargets="Publish"`, single-file publishes on Windows only) in `SoleSurvivor.App.csproj`. So it runs from Visual Studio's Publish button and from `publish.ps1`.
- It finds the install through the installer's AppId uninstall key (`{91792052-73D7-4F65-A185-2A01BD78DE90}_is1`, in HKCU or HKLM) and copies the new `SoleSurvivor.exe` over the installed one. It also updates `DisplayVersion` and `DisplayName` so Settings > Apps shows the new version.
- **Safety:** it only writes to the registered install folder, and only if `SoleSurvivor.exe` is already there. It skips the copy if the exe is unchanged (hash match) or the game is running from that folder. It never elevates: an all-users install in Program Files needs VS or PowerShell run as admin, or the new Setup. It always exits 0 (`ContinueOnError`), so it never breaks a build.
- **Opting out:** `-p:UpdateInstall=false`, or `.\publish.ps1 -NoInstallUpdate`.
- The installer itself already upgrades in place (fixed AppId); Setup is still how you install on a new PC.

## v1.0.28 (Build fix)
- MC3074 in `RulesWindow.xaml`: `Text="{swarm}"` was parsed as a markup extension. It's now escaped as `Text="{}{swarm}"`, like the other placeholders. Any XAML attribute that starts with `{` needs the `{}` prefix.

## v1.0.27 (Tenth scenario: Merriwick, and the swarm)
- **Merriwick** (`merriwick`): an original take on a Christmas-creature-feature (inspired by Gremlins, no film names, rules or looks). A market town on Christmas Eve. Survivor **Billie Hart** (toy-shop assistant). Killer **the Hatchmother** (wardrobe-tall, soot-black fur, a ring of tin-bright eyes, drags her nest). Swarm: **sootlings** (small soot-black furballs with two glowing eyes). Victims: shoppers; safe place: St Hilda's Church. Vehicle: Milk Float (range 2, `drive_cart`). No locked doors. Weather `Festive`: soft snow plus twinkling fairy-light strings top and bottom. Pub chimney smoke.
- **Swarm mechanic (engine, scenario-driven):** `ScenarioDef.SwarmName/SwarmNames/SwarmStart/SwarmSpawn/SwarmMax/SwarmBite`, `HasSwarm`. Null `SwarmName` = no swarm, so the other nine scenarios are unaffected (sim numbers identical).
  - `Game.Swarm` (per location), `SwarmTotal`, `SwarmHere`, `LastSwarmKilled`, `SwarmDamage(n)` = 0 for 1, 1 for 2–3, 2 for 4+ ("brave in numbers").
  - `ResolveSwarm` runs after the killer's attack: every group moves one step toward you (killer pathing; a barred door stops them), then on cards where the killer attacks she spawns `SwarmSpawn` where she stands (+1 in the finale, capped at `SwarmMax`; new ones don't move until next turn), then any on you bite. Holding Breath −1, Repelled (salt/flash) blocks. New tide events `SwarmMoved`, `SwarmSpawned`, `SwarmBit`.
  - Fighting: Weak Attack / Wild Swing / Desperate Plan are playable when the swarm is in your space. Each success kills one first (they shield her), the rest wound the killer if she's there. The flare also burns up to 2 in your space and is usable with only the swarm present.
  - Starts spread over the lairs.
- **UI:** sootlings drawn bottom-right of each location (up to 6 + ×N badge, bobbing); during the killer's turn `_swarmShown` replays moves (scuttle animation), spawns (ripple, "+1 sootling") and bites (flash, "−1 (sootlings)"). Killing them floats "−N sootlings". Killer panel shows the count; hints, briefing note and legend, and a "The swarm" rules section (hidden for other scenarios).
- **Art/scenery/sound:** `gen_art.py` adds 8 Merriwick icons (church, market square with tree and milk float, toy shop, picture house, pub, bakery, department store, ice rink) and figures `Billie`, `Hatchmother`, `Sootling`. `gen_scenery.py` `merriwick()` (drifts, lamp posts with glow, decorated firs, snowmen, post boxes, snowed-in cars). `gen_sounds.py` adds `amb_merriwick` (wind, distant church peal, sleigh-bell jingles), `tide_merriwick` (town clock strikes, chittering) and `swarm`. All older art, scenery and WAVs verified unchanged.
- **Balance (1500 games):** HealthBonus −3, SwarmStart 1, SwarmSpawn 1, SwarmMax 4. Easy 86.6%, Normal 68.9%, Hard 50.3%. Fuzz errors 0 across all ten.
- The semantic checker baseline is now 15: the extra one is the same RulesWindow false positive (one more `.Replace` in the chain).

## v1.0.26 (Rules tips wording)
- The Bloodlust tip read as if {victims} never raise Bloodlust. It now says rescued {victims} can't be killed, so they can't feed it, and that each kill raises it by 1. No logic change: `KillVictims` → `RaiseBloodlust(taken)` was already correct.
- The tips now cover keeping a move card (take Walk back in Planning) and E ending each phase.

## v1.0.25 (Card-driven turns: Action phase, then Planning)
- **Rule change:** every action needs a card. A turn is **1 ACTION** (play cards from hand: free) then **2 PLANNING** (buy from the Action Tableau with Time), then the killer's turn. This follows Final Girl: Time is spent when *buying*, not when playing.
- **Engine (`Game.cs`):**
  - `enum TurnStep { Action, Planning }`, `Step`, `InAction`, `InPlanning`. `EndActions()` (Action → Planning, clears unused move/escort tokens) and `EndTurn()` (Planning only: spent cards return to the tableau, then `ResolveTide`, then `StartRound`). `CanEndActions` / `CanEndTurn`.
  - No free actions: public `Search`/`Fight`/`Rest`/`CanSearch`/`CanFight`/`CanRest` and all `Cost*` constants are gone. `Move` needs a `MoveToken`, `Escort` needs `EscortCost` escort tokens (2 with the Wounded special victim). No Time fallback.
  - **Tableau:** `_tableau` holds every copy of `ActionCards.All`; `_spent` holds cards played, discarded or traded in for partials this turn and goes back to the tableau when Planning ends. `Tableau`, `SpentThisTurn`, `TableauCount(card)`, `CanBuy(card)` / `Buy(card)` / `BuyBlockedReason(card)` (Planning only; hand < 7; Time ≥ cost). The market (`_market`, `RefillMarket`, `MarketSize`, deck/discard) is removed.
  - **Starting hand:** the six zero-cost basics (Walk ×2, Lead, Search, Weak Attack, Rest). `AwaitingStartPick` / `ChooseStartingCards` → `AwaitingStart` / `Begin()` (reveals the opening event).
  - **Time:** resets to `TimePerRound` (+ event modifiers) each turn; doesn't carry over. `MaxTime` 12 (discards can push it over the per-turn amount). `StartTime` removed. Discard (+1 Time) works in both phases.
  - Card changes in `Content.cs`: starters renamed (Walk, Lead, Search, Weak Attack, Rest) and they're buyable piles in the tableau. Sprint = 3 moves (cost 2, ×3), Shepherd = 2 escorts (2, ×3), Wild Swing ×3, Desperate Plan ×2. Scramble/Peek/Holler removed from the deck (enum values kept). Search (GlanceAround) rolls the full Horror dice.
  - `UseBlockedReason` adds "only in your Action phase".
- **UI:**
  - Left panel: a phase tracker (1 ACTION › 2 PLANNING › 3 KILLER) with a line explaining the current phase replaces the Search/Fight/Rest tiles. Walk/Escort modes show moves/escorts ready from played cards and auto-switch when you play one. The big button reads END ACTIONS then END TURN; E does the same.
  - **Planning overlay** (`PlanningOverlay`, replaces `StartPickOverlay`) opens on the board: one pile per card, sorted by cost, with stacked backs for count, "N left · N used · N in hand", and Buy / the reason it can't be bought. "See the board" tucks it away; reopen from the strip. During the Action phase the same overlay opens read-only ("Look at the tableau").
  - The strip under the board: a narrow ACTION TABLEAU summary (cards and piles left, "used this turn", open button) and the hand gets the rest.
  - The briefing's Continue reads "Got it: begin the night" and calls `BeginNight()` (opening event, ROUND 1 banner). Hints and the rules window are rewritten for the new turn.
- **Sim:** `Bot.Act` / `Bot.Plan` (plays cards for moves/escorts/search/fight; buys movement, escorts, attacks when armed, healing when hurt) and `RandomBot`. New invariants: no buying in Action, no playing in Planning, tokens cleared at Planning, used cards return exactly, card count conserved across hand/tableau/spent, `BuyBlockedReason` agrees with `CanBuy`.
- **Balance (1500 games):** Time per turn Easy 6 / Normal 5 / Hard 4; killer base health 20 / 23 / 25. HealthBonus changes: Whitepine +2 (was 0), Kestrel +4 (+2), Cedar Hollow +9 (+5), Crowhurst −4 (−3).

  | Scenario | Easy | Normal | Hard |
  |---|---|---|---|
  | Gallowmouth | 88.9% | 73.3% | 47.7% |
  | Whitepine | 88.6% | 67.7% | 51.0% |
  | St Agnes | 88.0% | 73.8% | 49.5% |
  | Larkspur Lane | 88.9% | 73.8% | 53.2% |
  | Kestrel Motel | 89.5% | 74.9% | 55.3% |
  | Ashgrove House | 83.2% | 67.6% | 47.2% |
  | Cedar Hollow | 87.3% | 75.7% | 56.5% |
  | Holloway Funfair | 85.4% | 69.6% | 47.3% |
  | Crowhurst Farm | 81.8% | 64.3% | 46.5% |

  Fuzz errors: 0. Easy is a little softer and Hard a little kinder than before (~35%); further tuning is a knob turn (`TimePerRound` / killer health).
- **Note:** the 7-card hand limit is tight with six starting basics (Final Girl uses 10). It's kept at 7 as requested.

## v1.0.24 (Three more scenarios: Cedar Hollow, Holloway Funfair, Crowhurst Farm)
- **New scenarios:** originals inspired by Scream, Terrifier and Misery. No film characters, names or signature looks are used.

  | Scenario (id) | Setting | Survivor | Killer | Doors and keys | Vehicle | Weather |
  |---|---|---|---|---|---|---|
  | **Cedar Hollow** (`cedarhollow`) | phone-stalker slasher | Jess Morrow | the Caller (letterman jacket, smooth featureless white mask with eye slits, cordless phone and knife) | Party House–Your House (Spare Key) | Pickup Truck (range 3) | Leaves; the sheriff's light bar blinks red/blue |
  | **Holloway Funfair** (`holloway`) | Halloween funfair | Tasha Greene | the Merrymaker (silent clown, mustard/teal patchwork suit, green bowler, ash-grey face with a yellow star, strength-test mallet) | 2 locked doors into the Clown Caravans (Staff Key) | Golf Cart (range 2, `drive_cart`) | Carnival: chasing bulb strings and fog |
  | **Crowhurst Farm** (`crowhurst`) | snowbound farmhouse | Daniel Reyes | the Devotee ("your most devoted reader": grey bun, knitted cardigan, hatchet, keys) | 3 locked doors around the Parlour and Spare Room (Iron Keys) | none | Blizzard: sideways snow and white gusts; light snow theme |

- **Rule fix:** `KillerCanPass` means locked doors stop the killer only until the finale.
  - Before this, a killer teleported into a locked-off lair could never reach you in the finale. The game stalled forever: 100+ sim games hit the 200-round guard on Holloway and Crowhurst.
  - Killer pathing and `TidewifeNear` now use `KillerCanPass`. Survivor movement still uses `CanPass`.
  - The rules and the briefing mention it. The fuzz invariant now allows crossing in the finale.
- **Art:** `gen_art.py` has 24 more icons and 6 more figures (`Jess`, `Caller`, `Tasha`, `Merrymaker`, `Daniel`, `Devotee`). Themes and dispatch are in `ArtScenarios.cs`, the new weather is `AddLeaves`/`AddCarnival`/`AddBlizzard`, and `AddBlink` has a phase offset. The Crowhurst chimney sits on the Kitchen.
- **Scenery:** generator functions `cedarhollow()` (autumn trees, leaf piles, jack-o'-lanterns, fences, lamps, cars), `holloway()` (sawdust, striped tents with pennants, hay bales, bins, popcorn carts, bulb poles) and `crowhurst()` (snowdrifts, snow-capped rail fences, hay, dead trees, pines, log piles). The six older maps' layers are verified unchanged.
- **Sounds:**
  - Ambient loops: `amb_cedarhollow` (gusting wind in leaves, crickets, a distant dog), `amb_holloway` (a far-off detuned calliope waltz) and `amb_crowhurst` (blizzard howl and fire crackle).
  - Killer-turn stings: `tide_cedarhollow` (an old two-tone phone bell), `tide_holloway` (horn honks and a slide whistle) and `tide_crowhurst` (typewriter clacks and the carriage bell).
  - Effect: `drive_cart`.
  - The older WAVs are byte-identical.
- **Scenario select:** `RenderScenarioCards` lays the cards out in two rows of ⌈n/2⌉ columns (card width ≤ 360), with a compact art height when there are more than 6. The title line now reads "Nine nights…" automatically.
- **Balance (4000 games):**

  | Scenario | HealthBonus | Easy | Normal | Hard |
  |---|---|---|---|---|
  | Cedar Hollow | +5 | 78.5% | 67.3% | 34.4% |
  | Holloway Funfair | −2 | 82.7% | 66.9% | 37.4% |
  | Crowhurst Farm | −3 | 75.2% | 64.0% | 41.3% |

  The other six are essentially unchanged. The fuzzer reported 0 errors.

## v1.0.23 (Hand limit 7)
- **Rule:** `Game.HandLimit` is now 7 (was 4), and the rules text says so.
- **Balance:** win rates moved by at most a few points (Normal 64–68%, Hard 30–35%), so health was left as it was.
- **Hand strip:**
  - Up to 4 cards sit side by side. From 5 cards the hand fans out at a 60 px step (negative right margin), keeping each card's Play button visible.
  - Hovering a card sets its ZIndex to 100, bringing it to the front so its +1◆ discard is reachable.
  - `MarketColumn` and `HandColumn` (named in XAML) take star widths proportional to what each side needs, so both shrink equally (via their DownOnly Viewboxes) when crowded.
- Checker whitelist gained `Panel` and `GridUnitType`.

## v1.0.22 (Horror track dice fit)
- **Problem:** the Horror-track zone headers put the dice and the band name on one line, so the two-cell Uneasy band (~69 px) clipped its third die.
- **Fix:** headers now stack the dice (16 px for the current band, 14 px for the others, tight margins) above the band name (10 px), both centred.

## v1.0.21 (Item tiles explain themselves)
- **`Game.UseBlockedReason(i)`:** gives a few words on why a one-use item can't be used ("needs {killer} here or next door", "you're at full health", "door already barred", "finish your roll first", ...), or null if it can be used.
- **Item tiles:**
  - Kept items say "always on: …".
  - Usable one-use items are gold-bordered buttons, "▶ Click: …".
  - One-use items that can't be used yet are no longer disabled (faded to 35%). They're plain tiles that show the reason in red-brown.
- **Fuzzer:** checks that `CanUse` agrees with `UseBlockedReason` for every consumable.

## v1.0.20 (Readable items)
- **Problem:** the items list (`Viewbox DownOnly`) was being scaled down heavily, because the left column ran out of height.
- **Items:** now a 2-column `UniformGrid` of 42 px tiles: an icon, the name (ellipsis-trimmed) and a short effect from `ItemShort` (e.g. "+1 fight die", "▶ Use: heal 2"). One-use items are click-to-use buttons with a gold border when usable. The full text is in tooltips, which also show on disabled tiles. 4 items fit at full size; beyond that the list shrinks gently.
- **Height freed:**
  - The dice tray text is one line, with the rules in a tooltip.
  - The action tiles' second line is short ("up to 2 finds", "1 wound each", "heal 1"), with detail in tooltips that now use the killer's name and pronoun rather than a hard-coded "she/her".

## v1.0.19 (Title screen line)
- The title screen's tagline (`TitleTagline`) is now set by `TitleLine()` in `ShowMenu` from `Scenarios.All.Count`. It reads "Six nights. Six horrors. Only one of you walks out." and updates itself if scenarios are added.

## v1.0.18 (Objective briefing)
- **When it appears:** `MainWindow.Briefing.cs` adds a `BriefingOverlay` (full design surface, above the game-over panel, below the menu). It's shown by `StartNewGame` (before the starting-card picker) and by a new header button, "◎ Objective".
- **Left column:**
  - Title, tagline and blurb.
  - Three objectives: ★ rescue the victims at the sanctuary (count, escort capacity, Hope); ⚔ defeat the killer (the win condition, current health); ♥ stay alive (finale warning).
  - A note about locked doors and vehicles, where the scenario has them.
- **Map (`DrawBriefingMap`):** an 800×580 canvas in a Viewbox, showing:
  - the theme ground and roads;
  - animated green dashed routes from every location with victims to the sanctuary, using `Board.Path` that avoids locked doors;
  - padlocks;
  - the sanctuary with a pulsing green ring, a glow and a "★ SAFE: bring the {victims} here" banner;
  - ♟ count badges, a "● You" badge and a "☠ {Killer}" badge;
  - a legend underneath.
- **Closing it:** the Continue button reads "Got it: choose your starting cards", or "Back to the night" mid-game. Esc, Enter or Space close it, and other keys are swallowed while it's open. `RenderStartPick` waits while `BriefingOpen`.

## v1.0.17 (Card icons on dice: a 3 costs 2 cards, a 4 costs 1)
- **Dice faces:** `DiceRoll.CardsFor(v)` is 2 for a 3 (two-card icon), 1 for a 4 (one-card icon) and 0 otherwise. `CardsPerConversion` is gone.
- **Conversion API:** `DiceRoll.ConvertedDice` (indices) replaces the old int `Converted` field, which is now computed from it, and `IsConverted(i)` is new.
- **Offer:** `PendingRoll.MaxConversions` is the number of partials you could afford cheapest-first with your hand, capped by the useful successes.
- **`ConvertPartials(dieIndices, handIndices)`:** each chosen die must be a partial, the number of dice must be at most Max, and the card count must equal the sum of the chosen dice's costs. Pass two empty lists to keep the roll.
- **UI:**
  - The die art draws one or two cards by value.
  - The trade-in panel makes partial dice clickable (with chosen/affordable states and a cost label under each die). Card picks are limited to the chosen dice's total cost.
  - The button reads "Pick N more card(s)" or "Discard N: X wounds".
  - Tooltips, tray text, the Horror-track line and the rules are updated.
- **Sim bots:** the heuristic bot picks the cheapest dice first (the 4s) and pays with its cheapest cards; the random bot makes random affordable choices. The fuzzer checks that cards without dice are refused and that a 3 can't be paid with one card.
- **Balance:** cheaper conversions made every scenario about 10 points easier, so killer health is now Easy 16 / Normal 19 / Hard 20 (was 14/16/17). Scenario bonuses are unchanged. 4000 games:

  | Scenario | Easy | Normal | Hard |
  |---|---|---|---|
  | Gallowmouth | 81.3% | 66.2% | 31.0% |
  | Whitepine | 82.1% | 67.7% | 35.3% |
  | St Agnes | 80.8% | 67.9% | 37.9% |
  | Larkspur Lane | 80.8% | 67.5% | 35.0% |
  | Kestrel Motel | 81.1% | 68.3% | 36.4% |
  | Ashgrove House | 77.3% | 64.6% | 34.4% |

  The fuzzer reported 0 errors.

## v1.0.16 (Three new scenarios, locked doors, keys, vehicles)
- **New scenarios:** originals inspired by the user's three film references. No film characters, names or signature looks are used.

  | Scenario (id) | Setting | Survivor | Killer | Map features |
  |---|---|---|---|---|
  | **Larkspur Lane** (`larkspur`) | dream-stalker suburb | Maya Castillo | the Lullaby Man (porcelain moon mask, nightgown, nightcap, brass winding key) | 1 locked door (School–Playground, Janitor's Key), Bicycle (range 2) |
  | **Kestrel Motel** (`kestrel`) | cursed broadcast | Ruth Abernathy | the Presenter (grey suit, face of static, studio microphone) | 2 locked doors (Office–Honeymoon Suite, Office–Back Rooms, Room Keys), Station Wagon (range 3) |
  | **Ashgrove House** (`ashgrove`) | indoor puzzle house | Clara Voss | the Curator (velvet coat, porcelain face mended with gold, pocket watch) | 3 locked doors sealing off Gallery/Conservatory/Attic, Brass Keys; indoor theme with floorboards, no fog |

  All three use the St Agnes map topology with their own coordinates, items, events, killer cards and Bloodlust tracks.

- **Doors:**
  - `ScenarioDef.LockedEdges` and `KeyName`. `Game.IsLocked`, `LockedDoors` and `_unlocked`.
  - Locked doors block *all walking*, including killer pathing: `Board.Path`/`Distance` now take an optional `canPass` predicate, and Game passes `CanPass` everywhere the killer paths. Teleport cards still work.
  - Walking or escorting through a locked door with a `Key` uses up the key and unlocks the door permanently (`Travel`).
  - `Validate` → `CheckKeysReachEverything`: from the sanctuary, keys found in reachable places must open every area, and every starting victim must have a lock-free way home.
  - `TidewifeNear` ignores adjacency through a locked door.
- **Vehicles:**
  - `ScenarioDef.VehicleName`, `VehicleRange`, `VehicleVerb` and `VehicleSound`; `ItemKind.Vehicle` sits in a stash.
  - `Game.RouteTo(to)`:
    - On foot it's 1 step, through a locked door only if you hold a key.
    - With a vehicle it's a breadth-first search up to the range over open roads, not passing *through* the killer's space (stopping there is allowed).
  - `CanMove` and `CanEscort` use `RouteTo`, and the cost is the same as a walk or escort.
  - `LastRoute` records the trip for the animation.
  - `ItemKind.Key` and `ItemKind.Vehicle` items exist only in scenarios that name them (`BuildItems`).
- **Art:**
  - `tools/art/gen_art.py` generates `ArtExtra.cs` (24 location icons and 6 figures) plus SVG previews in `/tmp/preview`.
  - `ArtScenarios.cs` has the themes, the dispatch, the new `Weather` values (Motes, Static, Embers) via `AddMotes`, `AddStatic` and `AddEmbers`, plus `Padlock` and `AddBlink` (the Kestrel tower light).
  - `Theme.Indoor` draws floorboards instead of grass and skips fog.
  - The scenery generator has `larkspur()` (lawns, fences, trees, street lamps, mailboxes, parked cars), `kestrel()` (sand drifts, cacti, scrub, tumbleweeds, rocks, telephone poles and wires, drums) and `ashgrove()` (perimeter and interior walls with doorways, rugs, armchairs, candle tables, clocks, ferns, chests). Old scenarios' layers are verified unchanged.
- **Sound:**
  - New ambient loops: `amb_larkspur` (crickets and a distant detuned music box), `amb_kestrel` (static hiss, mains hum, neon buzz, a passing truck) and `amb_ashgrove` (grandfather-clock tick-tock and creaks).
  - Killer-turn stings: `tide_larkspur` (a winding-down music box), `tide_kestrel` (a static burst and 1 kHz tone) and `tide_ashgrove` (clock chimes). They're picked with `Sound.Has`.
  - Effects: `drive_car`, `drive_bike` and `unlock`. Old WAVs are byte-identical.
- **UI:**
  - The route overlay shows every reachable destination; vehicle routes are drawn as accent-coloured polylines.
  - Padlocks sit on locked roads with tooltips; once opened they show as faint open padlocks.
  - Location tooltips say walk/cycle/drive and when a key will be used.
  - Travel animates leg by leg (faster when driving, with the vehicle sound), with an "Unlocked" float and sound. `Snapshot.Locked` is new.
  - The Walk tile shows the vehicle range. Item glyphs are ⚿ for keys and » for vehicles.
  - Scenario select is now a 3-wide `WrapPanel` of compact cards (the blurb moved to the tooltip) with a features line, e.g. "Indoors · 3 locked doors".
  - Rules have a new "Locked doors, keys and vehicles" section.
- **Balance:**
  - New scenarios use StartDread 2.
  - Killer `HealthBonus`: Lullaby Man +3, Presenter +2, Curator +4. The Curator also has a harder-hitting track, because his doors hem him in.
  - The sim bot paths respect locks (passable with a key) and moves as far along as one move allows.
  - Win rates (4000 games):

    | Scenario | Easy | Normal | Hard |
    |---|---|---|---|
    | Larkspur Lane | 78.9% | 68.5% | 35.6% |
    | Kestrel Motel | 79.7% | 68.8% | 35.9% |
    | Ashgrove House | 75.5% | 65.4% | 35.5% |

    The original three are unchanged. The fuzzer reported 0 errors, with a new invariant that the killer never walks through a locked door.
- **Sim:** `dump <seed> <scenarioId>` now takes a scenario.

## v1.0.15 (Board-game Horror dice, partial successes, free cards)
- **Dice faces:** these follow the board game.
  - `DieFace` comes from `DiceRoll.Face(v)`: 1-2 Blank, 3-4 Partial (the cards icon), 5-6 Success (the star).
  - `SuccessThreshold` is now 5.
  - `DiceRoll` gained `Converted` and computed `Stars` and `Partials`.
- **Partial conversion:**
  - `DoSearch` and `DoFight` now go through `Offer(roll, usefulSuccesses, purpose, resolve)`.
  - If the roll has partials, you hold at least 2 cards, and more successes would matter (search: up to min(2, stash); fight: up to killer health), `Game.Pending` = `PendingRoll(Roll, MaxConversions, Purpose)` and the result waits.
  - While a roll is pending, `Acting` is false, so nothing else can happen and you can't end the turn.
  - `ConvertPartials(handIndices)` takes an even count, at most 2×Max. It discards the cards (starters vanish, others go to the discard pile with no Time refund), adds the conversions and resolves. An empty list keeps the roll.
  - `LastRoll` is replaced using `with`, so it keeps the same `Values` reference. The UI uses that to skip the dice tumble on conversion.
- **Free market cards:** new effects placed before QuickStep, so they aren't starters. All cost 0 with 2 copies each: Scramble (1 free walk), Peek (search with Horror dice −1) and Holler (1 free escort). The market deck is now 28 cards.
- **UI:**
  - The dice are red with white icons (`Die(value, DieLook, angle, size)`; `DieLook` is Rolling/Blank/Partial/Success/Converted). Converted dice have a gold glow and a small ▮▮ badge.
  - New `RollChoiceOverlay` on the board, rendered by `RenderRollChoice` from `RenderCards`. It shows the dice, your hand as selectable cards, a "Discard N: X wounds" button and "Keep the roll".
  - `AnimateActionAsync` returns early while a roll is pending, and on conversion shows a "+N ★ for 2N cards" float.
  - The tray text, the Horror-track line, the market hint and the rules (dice icons, free cards) are all updated.
- **Sim bots:**
  - The heuristic bot trades its cheapest cards: always on a fight, and on a search only when it found nothing. It always takes free cards (hand limit permitting).
  - The random bot makes random conversions.
  - The fuzzer checks that no actions are allowed while a roll is pending and that an odd card count is refused.
- **Balance:** Hard killer health is now 17 (Easy 14, Normal 16). The Tidewife's `HealthBonus` is −1, because Gallowmouth ran about 5 points harder than the other two.

  | Scenario | Easy | Normal | Hard |
  |---|---|---|---|
  | Gallowmouth | 81.2% | 68.7% | 33.4% |
  | Whitepine | 80.3% | 69.7% | 37.1% |
  | St Agnes | 80.4% | 70.3% | 39.5% |

  The fuzzer reported 0 errors.

## v1.0.14 (Fit-to-window layout, no sidebar scroll)
- **Scaling:** the window content is `Viewbox x:Name="RootScale"` (Uniform) around `Grid x:Name="DesignSurface"`, a fixed 1600×900 layout.
  - Everything (game, menus, overlays) scales to the window. Other aspect ratios get letterbox bars in the background colour.
  - Window MinWidth/MinHeight are now 800×450.
- **Layout:** the main area has three columns: left 330 (you), board in the middle, right 340 (the killer).
  - **Left column:** survivor, Horror track, actions, dice, items, and END TURN docked at the bottom.
  - **Middle column:** board, then market and hand in a 196 px row.
  - **Right column:** killer, killer board, events, last killer card, and the journal, which fills what's left.
  - The old single scrolling sidebar is gone.
- **No scroll bars:** market and hand are in `Viewbox StretchDirection=DownOnly` and shrink instead of scrolling. Items (Width 306), events (MaxHeight 210) and last-card lines (MaxHeight 120) shrink the same way. The journal's ScrollViewer has a hidden scroll bar, so the mouse wheel still works.
- **Side-column titles:** new `SideTitle` style in App.xaml (14 pt, tighter margins).
  - Dropped the "Pick a mode…" line; the hint bar covers it.
  - Shortened the Steel checkbox text.
  - Made the dice box more compact.
- **Window size option:** `Settings.WindowSize` ("1600x900" default; choices 1280×720, 1366×768, 1600×900, 1920×1080, 2560×1440 in `Settings.WindowSizes`).
  - The sizes are **screen pixels of the client area**, converted with `VisualTreeHelper.GetDpi`.
  - `ApplyDisplay()` sets RootScale to the size in DIPs and uses `SizeToContent` so WPF adds the chrome. On the next dispatcher tick it switches back to Manual, clears the size and centres the window.
  - If the size is bigger than the work area, the window maximises instead. Sizes too big for the screen are disabled in Options.
  - In full screen the sizes are disabled, because full screen just fills the screen.
  - Runs on `SourceInitialized`, F11, and changes in Options.
- Static checker whitelist (`/tmp/scheck/wpfnames.txt`) gained SourceInitialized, SystemParameters, VisualTreeHelper, SizeToContent, Dispatcher, Left, Top, ActualWidth and ActualHeight.
- **Not yet confirmed on a real WPF build:**
  - The left column's height budget at 1600×900 is tight. If items look tiny, trim the Actions tiles.
  - That `SizeToContent` gives an exact client size at your display scaling.

## v1.0.13 (Event cards)
- **Event deck:** 12 event mechanics (`EventCards.Templates`, `EventKind`). Each scenario names them with flavour (`ScenarioDef.EventNames`) and builds `Scenario.EventCards`. The deck is shuffled per game.
- **When events are revealed:**
  - One opening event, inside `ChooseStartingCards`. The UI shows it after the starter pick, before the ROUND 1 banner.
  - Killer cards with the ▣ icon (`TideCardDef.Event`; mechanics indices 1, 7, 11 and 16) reveal one after the card's effect, unless the game has already ended.
  - An event that can't take effect is ignored, with no redraw, as in the board game.
- **One-time events:**

  | Event | Effect |
  |---|---|
  | SecondWind | heal 2 |
  | Supplies | random item to inventory |
  | Resolve | Dread −1, +1 Hope |
  | MoreVictims | +2 victims at the place farthest from the killer |
  | BadOmen | Dread +1 |
  | Bloodscent | Bloodlust +1 |

- **Ongoing events:** kept in `_activeEvents` as `ActiveEvent` with `RoundsLeft` and `Location`.

  | Event | Effect | Discarded |
  |---|---|---|
  | Adrenaline | +1 Time per round | after 3 rounds (`TickEvents` in `StartRound`) |
  | Blackout | −1 search die | after 3 rounds |
  | Hunted | killer +1 move | when you wound the killer (`WoundTidewife`) |
  | Trap | 2 wounds when the killer ends a move on its location | when it springs (`TideEventKind.TrapSprung`, before the attack) |

- **Special victims:** at most one at a time (`Special`, `SpecialVictimAt`, `SpecialVictimKind`). They are also counted in `_villagers`.
  - **Movement:** they always come along on Escort and LanternSignal (`ArriveVillagers(..., special)`).
  - **Dying:** when the killer kills k of n victims at their location, they die with probability k/n.
  - **Finale:** they are lost.
  - **Companion (blue):**
    - Saved: +2 Hope.
    - Killed in your space: +3 Time next round.
    - Killed elsewhere: Dread +1.
  - **Wounded (orange):**
    - Escorting them costs +1 (`EscortCost`).
    - Saved: heal 2 and +1 Hope.
- **New TideEventKinds:** `EventRevealed` (Amount = index into `Scenario.EventCards`), `TrapSprung` and `EventNote`. `TideReport.RevealsEvent` is new, and so is `Game.LastEvent` (an `EventReveal`).
- **UI:**
  - Event cards reuse the killer-card overlay in blue (`RevealEventCardAsync`, `SetCardStyle`, `ShowCardAndWaitAsync`) and wait for a click.
  - New sidebar panel, "EVENTS IN PLAY" (`RenderEvents`), with tone colours: ✚ good, ✖ bad, ◆ mixed.
  - The special victim is drawn as a glowing blue or orange figure. A trap shows as ⊗ at its location.
  - Killer cards show the "▣ EVENT" row, and the escort tile shows the real cost.
  - Rules have a new Events section.
- **Balance:** events pushed win rates down about 8 points, so killer health is now Easy 14 / Normal 16 / Hard 15.

  | Scenario | Easy | Normal | Hard |
  |---|---|---|---|
  | Gallowmouth | 76.2% | 67.0% | 33.4% |
  | Whitepine | 78.6% | 69.5% | 39.7% |
  | St Agnes | 75.8% | 67.5% | 37.5% |

  The fuzzer reported 0 errors, including the new invariants: a special victim's location is never empty, and no event timer goes negative.

## v1.0.12 (Killer board / Bloodlust)
- **Killer board:** each scenario has a `BloodlustTrack` (`ScenarioDef.BloodlustTrack`, 11 `BloodlustLevel(Move, Attack, Effect)` rows, bottom to top). This follows Final Girl's killer board.
  - `Game.Bloodlust` starts at 0 and rises by 1 per victim killed (`RaiseBloodlust`).
  - Level effects fire once on reaching a level: `DreadUp` (☠, Dread +1) or `Frenzy` (♥, killer heals 2). Past the top, the final effect repeats: each kill heals 1 (`Bloodlust.FinalEffectHeal`).
  - `KillerMove` and `KillerAttack` come from the current level. In the finale both get +1 (`FinaleMoveBonus`, `FinaleDamageBonus`).
  - The three tracks:
    - Tidewife: balanced, 1/1 up to 3/3.
    - Hollow Stag: fast, moves 2 from level 1 and 3 from level 5, but hits lighter.
    - Matron: slow, never moves more than 2, but reaches attack 3 at level 7.
- **Killer cards no longer carry numbers.** `TideCardDef(Name, Flavour, Move, Attacks, Effect)`: who to go for and how (`TideMove`), whether to attack, and an effect. Five cards don't attack: Fog #5, both Stragglers, LowWater and the ToLair card.
  - **Attacks are flat damage, with no dice.** An attack hits everyone in the killer's space: up to `damage` victims die (`KillVictims`, raising Bloodlust), and if you're there you take `damage` (`StrikeSurvivor`). Holding Breath takes 1 off; Salt (Repelled) blocks it. The card effect now applies after the attack.
- **Removed:**
  - Killer healing per kill.
  - Dread per kill; Dread now rises via ☠ levels and DreadUp cards.
  - `KillerBonusDice`, `KillerHitOn`, `FinaleAttackDice`.
  - The "+1 attack die" mark at Dread 6 on the Horror track.
- `TideReport` now has `Steps, Attacks, Damage, BloodlustBefore, ..., Hits` and no longer has `AttackDice`. There is a new `TideEventKind.Bloodlust`, where Amount is the new level.
- **UI:**
  - New sidebar panel, "KILLER BOARD · BLOODLUST" (`RenderKillerBoard`): ➤ and ⚔ rows per level, ☠/♥/↺ effect icons, a marker on the current level, and a line saying what the next kill does.
  - On a Bloodlust event the panel's border pulses and floating text appears ("Bloodlust 3: moves 2, attacks 2 / Your Dread rises by 1").
  - Killer cards read "Target the nearest victims ► move N (Bloodlust)" and "► Attack for N: …".
  - The killer's strike no longer rolls dice. The phase bar says "ATTACKS", and the hint uses the killer's own pronoun.
  - The rules window has been rewritten for all of this.
- **Balance (4000 games):** killer health is now Easy 16 / Normal 18 / Hard 15, closer to the board game's killer health. Whitepine and St Agnes `HealthBonus` are removed (0); St Agnes keeps StartDread 2.

  | Scenario | Easy | Normal | Hard |
  |---|---|---|---|
  | Gallowmouth | 79.5% | 67.3% | 33.4% |
  | Whitepine | 78.4% | 66.6% | 40.6% |
  | St Agnes | 76.2% | 67.3% | 37.0% |

  The fuzzer reported 0 errors.
- Not done: Dark Power and Minor Dark Power cards. Frenzy (♥) stands in at the board's Dark Power slot.

## v1.0.11 (Horror track)
- **Horror track:** Dread now sets how many dice you roll, like the player board in the board game.
  - `Game.HorrorDiceFor(dread, finale)`: Uneasy (Dread 1–2) gives 3 dice, Afraid (3–5) gives 2, Terror (6+) and the finale give 1.
  - Fight and Search use `HorrorDice` plus a card modifier plus gear: `FightDice(mod)`, `SearchDice(mod)`. The modifiers are Wild Swing +1, Desperate Plan +2, Rummage +1, Shove −1 and Glance Around −1, with a minimum of 1 die. Card text now says "Horror dice +1" and so on.
  - The success target is a flat **4+**. Dread no longer raises it to 5+; fear now shows up as fewer dice. The killer still gets +1 attack die from Dread 6.
- **Starting Dread:** `ScenarioDef.StartDread`, default 1. St Agnes starts at 2, so the asylum is unnerving from the start and the first loss drops you to 2 dice.
- **UI:**
  - The sidebar gauge is now the **Horror track**: three band headers showing their dice (⚄⚄⚄ Uneasy / ⚄⚄ Afraid / ⚄ Terror), with the current band highlighted, Dread cells 1–8 and a white ▼ marker on the current Dread.
  - A line under the track reads "Horror roll: N dice, plus cards and gear; 4+ succeeds".
  - When a killer turn drops you a band, floating text says "Your nerve slips: N Horror dice", with a thud.
  - Fixed two leftover Gallowmouth phrases under the killer's health bar; they now come from the scenario.
- **Balance:** a flat 4+ on 3 dice made early fights much stronger, so I retuned, keeping killer health at 8/11/11 by difficulty.
  - Starting Dread 1 (2 for St Agnes) with health bonuses Whitepine +1 and St Agnes +1 gives Easy 80–82%, Normal 67.5–69% and Hard 25–33% across all three scenarios.
  - The random fuzzer ran with 0 errors.

## v1.0.10
- `publish.ps1` now finds Inno Setup's `ISCC.exe` wherever it is installed. It checks, in order:
  - PATH
  - Program Files (x86 and x64)
  - `%LOCALAPPDATA%\Programs\Inno Setup 6`, where "Install for me only" puts it
  - the uninstall registry entries (HKLM, WOW6432Node and HKCU)
- If none of those work, pass the path directly: `-Iscc "<path>\ISCC.exe"`.
- The script prints which compiler it used and the path of the finished installer.

## v1.0.9 (sound)
- **Sounds:** 20 effects and 3 ambient loops, all synthesised by `tools/sounds/gen_sounds.py` (numpy/scipy, no samples). They're written to `src/SoleSurvivor.App/Sounds/*.wav` (about 3.2 MB) and embedded via `<EmbeddedResource LogicalName="Sounds.%(Filename)%(Extension)">`.
- **`Sound.cs`:** extracts the WAVs once to `%LocalAppData%\SoleSurvivor\Sounds\<version>`, because WPF's MediaPlayer needs a file, not a stream. Each effect has a pool of 3 MediaPlayers so sounds can overlap. One looping MediaPlayer plays the scenario ambience (`amb_<scenarioId>`, chosen in `BuildScene`). Every audio call is wrapped in try/catch, so missing media components (for example Windows N without the Media Feature Pack) just means silence.
- **What makes a sound:**
  - Every button, toggle and checkbox clicks (one class handler on ButtonBase).
  - Dice, footsteps while walking, card buy/play/discard, search found/nothing.
  - The killer's movement and rising, strike/miss, a person taken, a person saved, healing, wounding the killer.
  - The tide or finale stinger, the round bell, and win/lose.
- **Controls:** the header has a **♪ Sound on/off** button and the **M** key toggles mute. Options adds Mute, a Volume slider (plays a test sound when you let go) and an Ambient sound toggle. These are saved as `Muted`, `Volume` and `AmbientSound` in `settings.json`.
- Generated loops aren't faded at the ends (they crossfade internally), so they repeat without a dip. Short effects get a 1–2 ms fade so their attack isn't lost.

## v1.0.8 (bigger market, discard for Time)
- The market offers **5 cards** a round (`Game.MarketSize`), up from 3.
- **Discard:** any card in your hand can be discarded for **+1 Time** (`Game.DiscardRefund`, `CanDiscard`/`Discard`). This isn't allowed at the 8 Time limit, where the Time would be wasted. Discarded market cards go back into the deck; discarded starting cards are gone.
- Hand cards now have their own layout: the card face, then a **▶ Play** button and a **+1◆** discard button. The market and hand strip under the board is wider (5:4 split, 98 px cards, scrolls if the window is narrow). Discarding shows a floating "+1 Time". When you're out of Time, the hint suggests discarding.
- Rebalanced. The sim bot was overspending: with a bigger market it bought every useful card it saw. It now buys at most one card a round, which is how a sensible player plays. Measured with that bot, the earlier tuning was too easy (Normal about 82%). Killer health is now Easy 8 / Normal 11 / Hard 11, and St Agnes's bonus is +4.
  - Current results: Easy 84–88%, Normal 69–75%, Hard 28–32% across the three scenarios. The random fuzzer, with discard included, ran with 0 errors.

## v1.0.7 (renamed: Sole Survivor)
- The game is renamed from Low Tide to **Sole Survivor**, since it now spans three scenarios. Everything is renamed:
  - The solution, projects and folders: `SoleSurvivor.sln`, `src/SoleSurvivor.Core`, `src/SoleSurvivor.App`, `tools/SoleSurvivor.Sim`.
  - The namespaces (`SoleSurvivor.*`), the exe (`SoleSurvivor.exe`), the product name, window titles, the title screen, message boxes, `publish.ps1` and the docs.
  - The installer (`installer/SoleSurvivor.iss`, with a new AppId) and the settings folder (`%AppData%\SoleSurvivor`).
- The coastal scenario's title is now **Gallowmouth**, matching Whitepine and St Agnes. Its scenario id is unchanged.
- Settings are migrated: if `%AppData%\SoleSurvivor\settings.json` doesn't exist, the game reads the old `%AppData%\LowTide\settings.json` once.
- New app icon: a lone figure with a lantern in a lit doorway, under a red moon. The lighthouse icon only suited Gallowmouth.
- Title-screen tagline: "Three nights. Three horrors. Only one of you walks out."

## v1.0.6
- Build fix: `Settings.cs` needed `using System.IO;`. WPF projects don't get System.IO as an implicit using, so `Path` can't clash with `System.Windows.Shapes.Path`. The sandbox checker now uses the same implicit usings as WPF.
- `Scenery.cs` (generated) now starts with `#nullable enable`, which fixes CS8669.

## v1.0.5 (front end + scenarios)
- **Front end** (`MainWindow.Menu.cs`, `MenuOverlay` in XAML):
  - Title screen with Continue, New game, Options, How to play and Quit. A live, blurred board animates behind it.
  - Scenario-select screen with three cards (portraits, blurb, survivor and killer) and a difficulty choice. Selecting a card swaps the board behind it.
  - Options: default difficulty, animation speed, weather and ambient effects, hint bar and full screen (also F11). They're saved to `%AppData%\SoleSurvivor\settings.json`, along with the last scenario played.
  - Pause menu on Esc or the header's Menu button: Resume, Restart, Options, Main menu, Quit.
  - Quitting or abandoning a game in progress asks for confirmation. The game-over screen has a Main menu button.
- **Scenario layer** (`Scenarios.cs`):
  - A `ScenarioDef` holds the map, start positions, summon point, lairs, survivor, killer, item names, 18 tide-card names, and every themed word or sentence the engine prints.
  - `Board`, `Items`, `TideCards` and `ActionCards` read from `Scenarios.Current`. `new Game(seed, difficulty, scenario)` selects the scenario. `Validate()` checks the map is connected, has exactly one sanctuary, and so on.
  - Tide mechanics are shared (`TideCards.Mechanics`). `TideMove.ToChapel` and `ToRandomShore` are now `ToSummonPoint` and `ToLair`.
  - Card and item text use tokens: `{she}`, `{victims}`, `{round}` and so on.
- **New scenarios:**
  - **Whitepine:** a snowbound park. Ranger Dana Okafor against the Hollow Stag; campers; the Ranger Station. Snowfall, an aurora, an ice sheet, a ridge, hoofprints and woodstove smoke.
  - **St Agnes:** a derelict asylum. Explorer Eli Marsh against the Matron; explorers; the Gatehouse. Rain with puddle rings, lightning, a boundary wall, graves, an angel statue, hedges and boiler-house smoke.
  - Each has 8 new location icons, two new figures and its own theme colours (roads, ripples, footprints).
- **Balance:** killer health bonus is Whitepine +1 and St Agnes +3 (its gatehouse has 3 connections). Heuristic win rates on Normal are about 70% for all three (Easy ~82–84%, Hard ~29–33%). Gallowmouth's numbers and scenery are byte-identical to v1.0.4.
- How to Play is worded for the scenario in play: `RulesWindow(scenario)` fills the tokens.

## v1.0.4
- The tide card stays on screen until the player clicks the mouse. The 2.6s auto-dismiss is gone, and so is the Space/Enter skip. The "CLICK TO CONTINUE" prompt pulses, and the cursor becomes a hand while the game waits.

## v1.0.3 (cards visible + starting hand)
- Bug fixed: the Market and Hand were rendered at the bottom of the scrolling sidebar, about 700px down, so they were off-screen on normal displays. They now sit in a permanent card strip under the board, with the Market on the left and your Hand on the right. The last-tide report and the Journal moved to the bottom of the sidebar.
- Starting cards: there are 5 free (cost 0) starter cards: Quick Step, Guide, Shove, Glance Around and Catch Your Breath. A picker overlay at game start asks you to choose 3. They're single-use and don't recycle into the market. `Game.AwaitingStartPick` blocks every other action until `ChooseStartingCards` is called.
- Cost-0 cards show FREE on the coin.
- Balance check: the starters are roughly neutral. While testing them I found the sim bot was wasting Time on market cards it never used. The bot now buys only cards it will use, and the rates are Easy ~83%, Normal ~70%, Hard ~30%, in line with the earlier tuning. The sim also asserts that no action is allowed before the pick and that duplicate picks are rejected.

## v1.0.2 (background scenery)
- The map is now a full scene:
  - Patchwork fields with furrows and dry-stone walls, and a pine wood across the north-east and down the east edge.
  - Hedgerow oaks and bushes, heather and gorse.
  - A shingle beach and a rocky cliff edge on the west coast.
  - A dead tree with crows, a stone circle, and a mill pond with reeds.
  - Out at sea: rocks with foam, a wreck, and two boats rocking at anchor. Moonlight glints twinkle on the water.
  - Warm light pools around every building and a darkened vignette at the edges.
- The stone circle's eerie glow rises with Dread and is full during the Spring Tide.
- The scenery is baked into `Scenery.cs` by `tools/scenery/gen_scenery.py`, one path per colour layer, so it costs WPF very little. The generator refuses to place anything on a location, token spot, road or the sea.

## v1.0.1 (graphical overhaul)
- Illustrated board drawn entirely in vector paths:
  - Coast with animated waves and drifting fog.
  - A sweeping lighthouse beam and cannery chimney smoke.
  - An icon for each of the 8 locations.
  - Character figures for Wren, the Tidewife and the villagers.
- Animations:
  - Wren walks with leg swing and bob, and escorted villagers trail behind her.
  - The Tidewife glides and leaves wet footprints. When a card says she "rises", she sinks and resurfaces somewhere else.
  - She lunges to drown villagers or to strike.
  - Dice tumble, with hit and miss floating text and a red damage vignette.
  - A flood wash for the Spring Tide, plus a death animation for Wren and a banishing animation for the Tidewife.
- Phase clarity:
  - A tracker across the top: YOUR TURN › TIDE CARD › SHE MOVES › SHE STRIKES, with a round badge and the tide cards left.
  - A context hint next to the tracker.
  - Banners for each new round and each tide.
  - The tide card flips up in the middle of the board and waits for a click.
  - Places you can reach get gold rings and moving dotted paths.
- Engine: `TideReport.Events` is an ordered stream (Moved/Rose/Claimed/Struck/…) that the UI replays. The sim verifies the stream always ends where she actually is.
- Header has a 'Fast animations' toggle (`Anim.Speed` 0.4).

## Previous: v1.0.0
- This is a playable first slice: one survivor (Wren), one killer (the Tidewife), one map (Gallowmouth, 8 locations) and three difficulties.
- Mechanics are modelled on solo slasher board games, with an original theme, names and card text:
  - Time-based action economy, with a market of action cards you buy into your hand.
  - d6 dice pools with a success threshold that worsens as Dread rises.
  - A killer deck (tide cards) that moves the Tidewife, drowns villagers and makes her attack.
  - Villager rescue, items and Hope (spend 1 Hope for +1 die).
  - A finale (the Spring Tide) when the tide deck runs out or Dread reaches 8.
- Key design hook: every villager she drowns heals her for 1 and raises her max health by 1, which ties rescuing to killing.

## Verification
- `SoleSurvivor.Core` and `SoleSurvivor.Sim` compile cleanly (.NET 8 SDK on Linux, 0 warnings).
- 20,000 simulated games: 0 exceptions and 0 invariant violations. Heuristic-player win rates: Easy ~80%, Normal ~68%, Hard ~28%. Random player ~0%.
- The v1.0.5 UI passed the same checks: 12 C# files, and XAML well-formed with every handler and resource matched. Text values that start with `{` are escaped as `{}`.
- `SoleSurvivor.App` (WPF) could not be compiled here because the WPF reference pack is unavailable, so it was checked three other ways:
  - Roslyn syntax check of every .cs file, at C# 12.
  - A stubbed semantic compile against the real `SoleSurvivor.Core.dll`. WPF types are missing, so it filters out errors caused by that. This catches mistakes in calls into the engine and between the partial files. I injected test errors to confirm it detects them.
  - XAML checks: well-formed, every StaticResource and FindResource key exists, every handler signature matches.
- Board art was previewed by rendering the same path data as SVG.
- The first build in Visual Studio is still the real compile check.

## Difficulty knobs (Game.cs constructor)
| | Time/round | Survivor HP | Tidewife HP |
|---|---|---|---|
| Easy | 5 | 6 | 7 |
| Normal | 5 | 5 | 9 |
| Hard | 4 | 5 | 9 |
Time per round is by far the most sensitive lever.

## Extending
- To add a location: append it to `Board.Locations` (with X/Y on the 800×580 canvas) and connect it in `Board.Edges`.
- To add a card: append it to `ActionCards.All` or `TideCards.All`. New effects need a case in `Game.Play` / `Game.ApplyEffect`.
- For more killers or survivors, the next step is to turn the Tidewife constants and `TideCards.All` into a `KillerDef`, and do the same for the survivor.

## Packaging
- `publish.ps1` publishes a single-file, self-contained win-x64 exe to `publish\win-x64`.
- `publish.ps1 -Installer` also runs Inno Setup 6.
- Targets net8.0-windows, which is within the VS ≤ .NET 9 constraint.
