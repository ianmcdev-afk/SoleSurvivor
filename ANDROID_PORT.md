# Sole Survivor: Android port brief

Start here in the Android chat, then read `handover.md` (full history and decisions) and `README.md`.

## Split between chats
- **Windows chat:** owns the WPF app (`src/SoleSurvivor.App`), the Windows installer and `publish.ps1`, and **game rules and content in `src/SoleSurvivor.Core`** (scenarios, balance, cards, the swarm).
- **Android chat:** owns the new Avalonia/Android projects and the Android packaging.
- **Treat `SoleSurvivor.Core` as read-only in the Android chat.** If the port needs a Core change (a new event, a UI-friendly property), make it small, note it at the top of `handover.md` under "Core changes made in the Android chat", and tell Ian, so it can be merged back into the Windows line. Otherwise the two copies drift.
- To pick up newer rules or scenarios later, take the latest Windows zip and copy its `src/SoleSurvivor.Core` (and the `tools/` generators) over.

## Starting point
- Repo as of **v1.0.30** (Windows): ten scenarios, Action/Planning card turns, the Merriwick swarm, and publishing that updates the installed game.
- `SoleSurvivor.Core` (about 3,100 lines) is plain `net8.0` with no Windows dependencies. Reference it as is.
- `tools/SoleSurvivor.Sim` is a headless bot simulator and fuzzer for Core. Use it for any balance question: `dotnet bin/Release/net8.0/SoleSurvivor.Sim.dll 1500 [scenarioId]`. Build it with `dotnet build -c Release`.

## Plan
1. **Framework: Avalonia 11.** It's the closest to WPF and runs on Android. Not MAUI: its drawing and animation model is further from WPF.
2. **Projects (added to `SoleSurvivor.sln`; WPF stays):**
   - `src/SoleSurvivor.Avalonia`: the shared Avalonia UI (views, art, animation, sound interface).
   - `src/SoleSurvivor.Android`: the Android app (`net8.0-android`), packaging, signing and the Android audio implementation.
   - Optionally `src/SoleSurvivor.Desktop`: an Avalonia desktop app, useful for quick testing without an emulator.
3. **Port the UI in this order:**
   1. Board drawing.
   2. Cards and hand.
   3. Sidebar panels.
   4. Overlays: briefing, killer card, roll choice, planning, menu and game over.
   5. Animations.
   6. Sound.
4. **Phone layout:**
   - The board fills the screen in landscape.
   - The hand sits in a bottom sheet.
   - Your status, the killer panel, events, items and the journal go in slide-out drawers or tabs.
   - The planning tableau is full-screen.
   - Tablet landscape can use something close to the current three-column layout.

## How the WPF app maps across
| WPF file | What it does | Porting notes |
|---|---|---|
| `Art.cs`, `ArtScenarios.cs` | Themes, `Figure` (root, body, position, facing, bob, tilt, legs), weather, glows | Uses `Path` geometry strings, `RadialGradientBrush`, `DropShadowEffect` and transforms. Avalonia has equivalents for all of these. `DropShadowEffect` → `DropShadowEffect` or `BoxShadow`. |
| `ArtExtra.cs` | **Generated** by `tools/art/gen_art.py` (icons and figures as path data) | Add an Avalonia emitter to the generator rather than hand-porting. The path strings are the same mini-language. |
| `Scenery.cs` | **Generated** by `tools/scenery/gen_scenery.py`: path-data layers per scenario | Portable as data. Reuse the file or emit a copy. |
| `Anim.cs` | Small animation helper (`To`, `Loop`, `Set`, `Wait`, easing) over `BeginAnimation` | Main animation rework point. Re-implement the same API on Avalonia animations (or a `DispatcherTimer` tween) and most call sites survive. About 50 animation calls in total. |
| `MainWindow.xaml` (+ `.Board/.Sidebar/.Tide/.Menu/.Briefing.cs`) | One 1600×900 design surface in a `Viewbox`, three columns, overlays | Rebuild as Avalonia views. Keep the logic in the code-behind partials and change the markup. |
| `Sound.cs` | Embedded WAVs (`Sounds.*.wav`), `MediaPlayer` for loops, `Sound.Has(name)`, per-scenario `amb_{id}`/`tide_{id}` | Put an interface in the shared project and implement it per platform (Android: `SoundPool` for effects, `MediaPlayer` for loops). The 42 WAVs are synthesised by `tools/sounds/gen_sounds.py`, so there's nothing to license. |
| `Settings.cs` | JSON in `%AppData%\SoleSurvivor\settings.json` | Use the app's private storage on Android. |
| `RulesWindow.xaml` | Rules text with `{placeholders}` filled per scenario | Make it a scrollable page. XAML attributes starting with `{` need the `{}` escape (as in WPF). |

**Input to replace for touch:**
- About 30 hover, tooltip and keyboard uses:
  - E ends the phase, M mutes, Esc opens the menu, F11 toggles full screen.
  - Hovering lifts a card out of the hand fan.
  - Tooltips appear on the board, items, piles and the Bloodlust track.
- Replace them with buttons, tap-and-hold for detail, and a hand that scrolls or expands on tap.

## Conventions (Ian)
- Terse communication; diagnose before fixing; read the existing code before assuming anything.
- **Bump the patch version on every package:** `Directory.Build.props` `<Version>`, plus Android `ApplicationDisplayVersion`, and `ApplicationVersion` as an integer derived from it (for example 1.0.31 → 10031). Update `handover.md` each time. Ship as a zip named after the version.
- Visual Studio can't target .NET 10: stay on .NET 8.
- **Security is a standing priority.**
- **Original characters only:** the scenarios are inspired by films but use no film names, characters or signature looks.
- **No Android SDK in the Claude sandbox, and no NuGet access.** Verify statically (syntax checks, XAML well-formed, Core builds, the sim runs), and Ian builds and tests on his PC.

## Packaging and updates
- **App ID:** `com.ianmcdev.solesurvivor` (confirm with Ian before the first release; it can never change afterwards).
- **Output:** `dotnet publish -f net8.0-android -c Release` gives an **APK** (sideload) and an **AAB** (Google Play). Add a `publish-android.ps1` like `publish.ps1`.
- **Updates install over the top** when the app ID is the same, `ApplicationVersion` is higher, and the build is signed with the **same key**.
- **Signing:**
  - Use one release keystore, created once.
  - Never commit it, and add it to `.gitignore`.
  - Read the path and password from environment variables (for example `SOLESURVIVOR_KEYSTORE`, `SOLESURVIVOR_KEYSTORE_PASS`).
  - If the key is lost, existing installs can't be updated, so Ian should back it up.
- **Prerequisites on Ian's PC:** the Visual Studio ".NET Multi-platform App UI development" workload (or `dotnet workload install android`) and an emulator or a phone with USB debugging.

## Prompt to paste into the new chat
> Port Sole Survivor to Android using Avalonia 11, sharing the existing SoleSurvivor.Core project. The attached zip is the repo at v1.0.30. Read ANDROID_PORT.md first, then handover.md. Start with the shared Avalonia project and an Android head that shows the board for one scenario.
