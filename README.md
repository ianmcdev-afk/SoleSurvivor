# Sole Survivor

A solo, turn-based horror board game for Windows (WPF / .NET 8). Every action comes from a card: each turn you play cards (Action phase), then spend Time buying cards for next turn from the Action Tableau (Planning phase). You also have dice, a Horror track, a killer deck with a Bloodlust killer board, event cards, and a finale, across ten original scenarios:

| Scenario | Survivor | Killer | Rescue | Safe place |
|---|---|---|---|---|
| **Gallowmouth** (a coastal village) | Wren Calloway, the keeper's daughter | The Tidewife | villagers | Lighthouse |
| **Whitepine** (a snowbound national park) | Dana Okafor, park ranger | The Hollow Stag | campers | Ranger Station |
| **St Agnes** (a derelict asylum) | Eli Marsh, urban explorer | The Matron | explorers | Gatehouse |
| **Larkspur Lane** (a suburb where sleep kills) | Maya Castillo, insomniac | The Lullaby Man | sleepers | All-Night Diner |
| **Kestrel Motel** (a roadside motel and a cursed broadcast) | Ruth Abernathy, local reporter | The Presenter | guests | Gas Station |
| **Ashgrove House** (a sealed country house) | Clara Voss, estate valuer | The Curator | heirs | Foyer |
| **Cedar Hollow** (a small town and a masked caller) | Jess Morrow, high-school senior | The Caller | friends | Sheriff's Station |
| **Holloway Funfair** (a funfair on Halloween night) | Tasha Greene, ride operator | The Merrymaker | stragglers | Front Gate |
| **Crowhurst Farm** (a snowbound farmhouse) | Daniel Reyes, crime novelist | The Devotee | visitors | The Lane |
| **Merriwick** (a market town on Christmas Eve) | Billie Hart, toy-shop assistant | The Hatchmother, and her swarm of sootlings | shoppers | St Hilda's Church |

The six newer scenarios add **locked doors** (which need a key and block everyone, including the killer, until the finale) and most have a **vehicle** (bicycle, station wagon, pickup truck, golf cart or milk float) that moves you 2 or 3 spaces at a time. In Merriwick the killer also has a **swarm**: small sootlings that spawn from her, creep towards you each turn and nip for small damage once two or more reach you.

The game opens on a title screen with Continue, New game, Options, How to play and Quit. New game leads to a scenario-select screen, then an objective briefing with a map of where to bring people to safety (reopen it any time with the Objective button), and Esc pauses during play. There's sound throughout: effects for dice, cards, footsteps, the killer and the finale, plus an ambient loop per scenario (sea, wind, rain, a distant music box, TV hum, a ticking clock, autumn wind, a funfair calliope, a blizzard, church bells in the snow) and a killer-turn sting for each of the newer places. Mute with the header button or M. Options (default difficulty, animation speed, volume, ambient sound, weather and ambient effects, hints, full screen, window size) are saved to `%AppData%\SoleSurvivor\settings.json`. The full rules are in the in-game **How to play** window, worded for the scenario you're playing.

## Run it

**From Visual Studio:** open `SoleSurvivor.sln`. `SoleSurvivor.App` is the startup project. Press F5.

**As a standalone exe:**

```powershell
.\publish.ps1
# -> publish\win-x64\SoleSurvivor.exe  (single self-contained file, no .NET install needed)
```

In Visual Studio, you can instead right-click SoleSurvivor.App, choose **Publish**, and pick the `win-x64` profile.

**Installer:** install [Inno Setup 6](https://jrsoftware.org/isinfo.php) (or `winget install JRSoftware.InnoSetup`), then run:

```powershell
.\publish.ps1 -Installer
# -> installer\Output\SoleSurvivor-Setup-1.0.32.exe
```

If Sole Survivor is already installed on the PC, every publish (Visual Studio's Publish button or `publish.ps1`) also copies the new exe over the installed copy, so you don't need to run the new Setup. It won't touch a running game or an install it has no permission to write to, and it tells you why in the build output. Turn it off with `.\publish.ps1 -NoInstallUpdate` or `-p:UpdateInstall=false`.

The installer is per-user by default (no admin needed). It adds a Start-menu shortcut, an optional desktop icon and an uninstaller. The version comes from `Directory.Build.props`.

## Layout

| Path | What |
|---|---|
| `src/SoleSurvivor.Core` | The rules engine (net8.0, no UI). `Scenarios.cs` holds each scenario's map, characters, item names, killer deck and wording. `Content.cs` holds the shared mechanics: card templates and the 18 tide mechanics. `Game.cs` holds the rules. |
| `src/SoleSurvivor.App` | The WPF front end. `Art.cs` (Gallowmouth) and `ArtScenarios.cs` (themes, Whitepine, St Agnes, weather) hold the vector art. `Anim.cs` has awaitable animation helpers. `MainWindow.*.cs` is split into the core, the board, the tide sequence, the sidebar and the menus. `Settings.cs` holds the persisted options. |
| `tools/SoleSurvivor.Sim` | A headless harness. It plays thousands of games with a heuristic player and a random fuzzer, checks invariants, and prints win rates per difficulty. |
| `tools/scenery/gen_scenery.py` | Generates `Scenery.cs`, the baked background for all three maps. It places everything with collision checks against locations, roads and tokens. Re-run it with `python3 tools/scenery/gen_scenery.py` after moving locations. |
| `tools/sounds/gen_sounds.py` | Synthesises every sound effect and ambient loop as WAVs in `src/SoleSurvivor.App/Sounds` (needs numpy and scipy). There are no samples, so nothing needs a licence. The WAVs are embedded in the exe. |
| `installer/SoleSurvivor.iss` | The Inno Setup script. |

Balance check: run `dotnet run --project tools/SoleSurvivor.Sim -- 3000` to see win rates per scenario and difficulty. To check one scenario, add its id, for example `-- 3000 whitepine`. To print one game's log, run `dotnet run --project tools/SoleSurvivor.Sim -- dump 42`.
