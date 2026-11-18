using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SoleSurvivor.Core;

namespace SoleSurvivor.App;

/// <summary>The tide phase played out step by step, plus the phase tracker, banners and hints.</summary>
public partial class MainWindow
{
    private TaskCompletionSource? _cardWait;

    private void SkipCard() => _cardWait?.TrySetResult();

    // ------------------------------------------------------------------ the tide sequence

    private async Task PlayTideAsync(Snapshot before)
    {
        var g = _game;
        var tide = g.LastTide;
        if (tide is null) return;

        // The board still shows the pre-tide state; animate from there.
        Place(_tidewife, HerFeet(before.Tidewife));

        SetStage(Stage.TideCard);
        var sc = g.Scenario;
        // Each place has its own sting for the killer's turn where one exists (a music box, a TV tone, a clock).
        Sound.Play(tide.IsFinaleTide ? "finale" : Sound.Has($"tide_{sc.Id}") ? $"tide_{sc.Id}" : "tide", tide.IsFinaleTide ? 0.6 : 0.8);
        await ShowBannerAsync(tide.IsFinaleTide ? $"THE {sc.FinaleName.ToUpperInvariant()}" : sc.PhaseBanner,
            tide.IsFinaleTide ? $"{sc.Killer.She} hunts only you." : $"{sc.Killer.Name}'s turn", Art.Danger);

        // The swarm is drawn as it was until its own events play out.
        _swarmShown = g.Scenario.HasSwarm ? before.Swarm.ToArray() : null;
        DrawSwarm();
        foreach (var ev in tide.Events)
        {
            switch (ev.Kind)
            {
                case TideEventKind.SwarmMoved:
                    _swarmShown![ev.From] -= ev.Amount;
                    DrawSwarm();
                    await SwarmMoveAsync(ev.From, ev.To, ev.Amount);
                    _swarmShown[ev.To] += ev.Amount;
                    DrawSwarm();
                    break;

                case TideEventKind.SwarmSpawned:
                {
                    Sound.Play("swarm", 0.8);
                    _swarmShown![ev.To] += ev.Amount;
                    DrawSwarm();
                    var at = SwarmFeet(ev.To, 0);
                    Ripple(at, Art.C("#BFD8FF"), 1.6);
                    Float(at + new Vector(0, -34), ev.Amount == 1 ? $"+1 {g.Scenario.SwarmName}" : $"+{ev.Amount} {g.Scenario.SwarmNames}", Art.Hex("#BFD8FF"), 13);
                    await Anim.Wait(500);
                    break;
                }

                case TideEventKind.SwarmBit:
                {
                    SetStage(Stage.SheStrikes);
                    var you = FeetOf(_wren);
                    if (ev.Amount > 0)
                    {
                        Sound.Play("swarm");
                        Sound.Play("strike", 0.6);
                        Ripple(you, Art.C("#C8553D"), 1.4);
                        Float(you + new Vector(0, -64), $"−{ev.Amount} ({g.Scenario.SwarmNames})", Art.Danger, 18);
                        await FlashAsync(_wren);
                    }
                    else
                    {
                        Sound.Play("miss", 0.6);
                        Float(you + new Vector(0, -64), ev.Text.Contains("lone") ? "It hisses, alone" : "They hold back", Art.Accent, 14);
                        await Anim.Wait(500);
                    }
                    RenderStatus();
                    break;
                }

                case TideEventKind.CardDrawn:
                    await RevealTideCardAsync(tide);
                    SetStage(Stage.SheMoves);
                    break;

                case TideEventKind.FinaleBegins:
                    await FinaleBeginsAsync();
                    break;

                case TideEventKind.Moved:
                    await GlideAsync(ev.From, ev.To);
                    break;

                case TideEventKind.Rose:
                    await SinkRiseAsync(ev.From, ev.To);
                    break;

                case TideEventKind.Blocked:
                    await LungeTowardAsync(ev.To);
                    Float(WrenFeet(ev.To) + new Vector(0, -70), "The door holds!", Art.Accent, 16);
                    await Anim.Wait(500);
                    break;

                case TideEventKind.Claimed:
                {
                    SetStage(Stage.SheStrikes);
                    var at = new Point(Board.Locations[ev.To].X, Board.Locations[ev.To].Y + 60);
                    await LungeTowardPointAsync(at);
                    Sound.Play("taken");
                    Ripple(at, Art.C("#C8553D"), 2.4);
                    Float(at + new Vector(0, -20), ev.Amount == 1 ? $"Taken: 1 {g.Scenario.Victim}" : $"Taken: {ev.Amount} {g.Scenario.Victims}", Art.Danger, 15);
                    await FadeVillagerFiguresAsync(ev.To, ev.Amount);
                    RenderStatus();
                    await Anim.Wait(300);
                    break;
                }

                case TideEventKind.Bloodlust:
                {
                    // The killer board: the marker climbs one level per kill.
                    Sound.Play("tide", 0.45);
                    RenderStatus();
                    _ = PulseAsync(BloodlustGlow, 1, 120, 600);
                    var lvl = g.Scenario.BloodlustTrack[Math.Min(ev.Amount, g.BloodlustMax)];
                    string fx = Bloodlust.Describe(lvl.Effect, g.Scenario);
                    Float(FeetOf(_tidewife) + new Vector(0, -78),
                        ev.Text.StartsWith("Bloodlust is full") ? $"Bloodlust full: +{Bloodlust.FinalEffectHeal} health"
                            : $"Bloodlust {ev.Amount}: moves {lvl.Move}, attacks {lvl.Attack}" + (fx.Length > 0 ? $"\n{fx}" : ""),
                        Art.Danger, 14);
                    await Anim.Wait(fx.Length > 0 ? 1100 : 750);
                    break;
                }

                case TideEventKind.EventRevealed:
                {
                    var card = ev.Amount >= 0 && ev.Amount < g.Scenario.EventCards.Count ? g.Scenario.EventCards[ev.Amount] : null;
                    if (card is null) break;
                    await RevealEventCardAsync(card, null);
                    // Events come after the attack, so the board can catch up to the final state here.
                    RenderBoard();
                    RenderStatus();
                    RenderItems();
                    var at = ev.To >= 0 ? new Point(Board.Locations[ev.To].X, Board.Locations[ev.To].Y + 40) : FeetOf(_wren);
                    Ripple(at, Art.C(card.Tone > 0 ? "#7FD8C4" : card.Tone < 0 ? "#C8553D" : "#E8C26A"));
                    Float(at + new Vector(0, -50), ev.Text.StartsWith("Event: ") ? ev.Text[7..] : ev.Text, EventTone(card), 13);
                    await Anim.Wait(1000);
                    break;
                }

                case TideEventKind.TrapSprung:
                {
                    Sound.Play("hit");
                    var her = FeetOf(_tidewife);
                    Ripple(her, Art.C("#7FD8C4"), 2);
                    Float(her + new Vector(0, -70), $"Trap! −{ev.Amount}", Art.Accent, 22);
                    await FlashAsync(_tidewife);
                    RenderStatus();
                    await Anim.Wait(500);
                    break;
                }

                case TideEventKind.EventNote:
                    Float(FeetOf(_wren) + new Vector(0, -64), ev.Text, Art.Warm, 13);
                    RenderStatus();
                    await Anim.Wait(1100);
                    break;

                case TideEventKind.Straggler:
                {
                    var at = new Point(Board.Locations[ev.To].X, Board.Locations[ev.To].Y + 60);
                    Ripple(at, Art.C("#E8C26A"));
                    Float(at + new Vector(0, -20), "A straggler!", Art.Warm, 14);
                    await Anim.Wait(600);
                    break;
                }

                case TideEventKind.Effect:
                    Float(FeetOf(_tidewife) + new Vector(0, -78), ev.Text, Art.Warm, 13);
                    await Anim.Wait(900);
                    break;

                case TideEventKind.Struck:
                case TideEventKind.Missed:
                    SetStage(Stage.SheStrikes);
                    await StrikeAsync(ev);
                    break;

                case TideEventKind.Repelled:
                    SetStage(Stage.SheStrikes);
                    Sound.Play("miss");
                    await LungeAsync(0.3);
                    Float(FeetOf(_wren) + new Vector(0, -64), "The salt holds her back", Art.Accent, 15);
                    await Anim.Wait(600);
                    break;

                case TideEventKind.SurvivorDied:
                    await WrenFallsAsync();
                    await Anim.Wait(600);
                    break;
            }
        }

        _swarmShown = null;
        DrawSwarm();

        // The Horror track: did the Dread knock you into a band with fewer dice?
        if (!g.IsOver && g.HorrorDice < before.HorrorDice)
        {
            Sound.Play("nothing");
            RenderStatus();
            Float(FeetOf(_wren) + new Vector(0, -64),
                $"Your nerve slips: {g.HorrorDice} Horror {(g.HorrorDice == 1 ? "die" : "dice")}", Art.Hex("#C9893E"), 15);
            await Anim.Wait(900);
        }
    }

    private async Task StrikeAsync(TideEvent ev)
    {
        // No dice: the killer board says how hard the attack lands.
        await LungeAsync();

        if (ev.Kind == TideEventKind.Struck)
        {
            Sound.Play("strike");
            _ = PulseAsync(DamageVignette, 1, 120, 500);
            Float(FeetOf(_wren) + new Vector(0, -60), $"−{ev.Amount} ♥", Art.Danger, 22);
            await ShakeAsync(_wren);
            RenderStatus();
        }
        else
        {
            Sound.Play("miss");
            Float(FeetOf(_wren) + new Vector(0, -60), "Dodged!", Art.Accent, 18);
        }
        await Anim.Wait(500);
    }

    private Task LungeTowardAsync(int location) => LungeTowardPointAsync(WrenFeet(location));

    private async Task LungeTowardPointAsync(Point target)
    {
        var her = FeetOf(_tidewife);
        double baseX = _tidewife.Position.X, baseY = _tidewife.Position.Y;
        Face(_tidewife, her.X, target.X);
        var d = (target - her) * 0.35;
        await Task.WhenAll(
            Anim.To(_tidewife.Position, TranslateTransform.XProperty, baseX + d.X, 180, Anim.EaseOut),
            Anim.To(_tidewife.Position, TranslateTransform.YProperty, baseY + d.Y, 180, Anim.EaseOut));
        await Task.WhenAll(
            Anim.To(_tidewife.Position, TranslateTransform.XProperty, baseX, 320, Anim.EaseInOut),
            Anim.To(_tidewife.Position, TranslateTransform.YProperty, baseY, 320, Anim.EaseInOut));
    }

    private async Task FinaleBeginsAsync()
    {
        Sound.Play("finale");
        await PulseAsync(FloodOverlay, 1, 600, 900);
        var fades = new List<Task>();
        foreach (var (id, list) in _villagerFigures)
            fades.Add(FadeVillagerFiguresAsync(id, list.Count));
        await Task.WhenAll(fades);
        Anim.Set(FinaleTint, OpacityProperty, 1);
        await ShowBannerAsync($"THE {_game.Scenario.FinaleName.ToUpperInvariant()}", _game.Scenario.FinaleBannerSub, Art.Danger, 1600);
    }

    // ------------------------------------------------------------------ tide card

    private async Task RevealTideCardAsync(TideReport tide)
    {
        TideCardName.Text = tide.CardName;
        TideCardFlavour.Text = tide.Flavour;
        TideCardRows.Children.Clear();

        var sc = _game.Scenario;
        TideCardLabel.Text = $"≋  {sc.DeckName.ToUpperInvariant()}  ≋";
        string lairs = string.Join(" or the ", Board.Locations.Where(l => l.IsLair).Select(l => l.Name));
        // Cards only say who and whether to attack; the numbers come from the killer board (Bloodlust).
        string move = tide.Move switch
        {
            _ when tide.IsFinaleTide => $"Hunts you: moves {Steps(tide.Steps)} (Bloodlust +{Game.FinaleMoveBonus})",
            TideMove.TowardVillagers => $"Target the nearest {sc.Victims} ► move {Steps(tide.Steps)} (Bloodlust)",
            TideMove.TowardSurvivor => $"Target YOU ► move {Steps(tide.Steps)} (Bloodlust)",
            TideMove.ToSummonPoint => $"Appears at the {Board.Locations[sc.SummonId].Name}",
            TideMove.ToMostVillagers => $"Appears where most {sc.Victims} are",
            TideMove.ToLair => $"Appears at the {lairs}",
            _ => $"Stays where {sc.Killer.SheLower} is"
        };
        AddCardRow("➤", move);
        if (tide.Attacks)
            AddCardRow("⚔", $"► Attack for {tide.Damage}: kills up to {tide.Damage} {(tide.Damage == 1 ? sc.Victim : sc.Victims)} in {sc.Killer.Her} space, and deals {tide.Damage} damage to you if you are there");
        else
            AddCardRow("·", "No attack");
        var def = TideCards.All.FirstOrDefault(c => c.Name == tide.CardName);
        if (def is not null && def.Effect != TideEffect.None)
            AddCardRow("✷", TideCards.DescribeEffect(def.Effect));
        if (def is not null && def.Event)
            AddCardRow("▣", "EVENT: reveal the next event card");
        SetCardStyle(Art.Danger, "#FF3A1F1C", "#FF160E0E");

        await ShowCardAndWaitAsync();
    }

    /// <summary>An event card, shown in the same frame as killer cards but in event blue.
    /// <paramref name="reveal"/> adds what actually happened (opening event).</summary>
    private async Task RevealEventCardAsync(EventCardDef card, EventReveal? reveal)
    {
        TideCardLabel.Text = card.Ongoing ? "▣  EVENT · ONGOING  ▣" : "▣  EVENT  ▣";
        TideCardName.Text = card.Name;
        TideCardFlavour.Text = card.Flavour;
        TideCardRows.Children.Clear();
        AddCardRow(EventCards.Icon(card), card.Text, EventTone(card));
        if (card.Discard is not null)
            AddCardRow("↺", card.Discard, Art.Dim);
        if (reveal is { Applied: false })
            AddCardRow("·", "It can't take effect right now, so it is ignored.", Art.Dim);
        SetCardStyle(Art.Hex("#5B8FD6"), "#FF1B2A44", "#FF0C1220");
        await ShowCardAndWaitAsync();
    }

    private void SetCardStyle(Brush accent, string top, string bottom)
    {
        TideCardOverlay.BorderBrush = accent;
        TideCardLabel.Foreground = accent;
        TideCardOverlay.Background = new LinearGradientBrush(Art.C(top), Art.C(bottom), 90);
        if (TideCardOverlay.Effect is System.Windows.Media.Effects.DropShadowEffect glow && accent is SolidColorBrush sb)
            glow.Color = sb.Color;
    }

    private async Task ShowCardAndWaitAsync()
    {
        TideCardOverlay.Opacity = 0;
        TideCardOverlay.Visibility = Visibility.Visible;
        Sound.Play("card_play");
        Anim.Set(TideCardShift, TranslateTransform.YProperty, 0);
        Anim.Set(TideCardScale, ScaleTransform.ScaleXProperty, 0);
        Anim.Set(TideCardScale, ScaleTransform.ScaleYProperty, 0.9);
        await Task.WhenAll(
            Anim.To(TideCardOverlay, OpacityProperty, 1, 200),
            Anim.To(TideCardScale, ScaleTransform.ScaleXProperty, 1, 340, Anim.Back),
            Anim.To(TideCardScale, ScaleTransform.ScaleYProperty, 1, 340, Anim.Back));

        // Stay up until the player clicks: no timeout, unaffected by Fast animations.
        _cardWait = new TaskCompletionSource();
        InputBlocker.Cursor = System.Windows.Input.Cursors.Hand;
        HintText.Text = "Read the card, then click anywhere to continue.";
        Anim.Loop(TideCardPrompt, OpacityProperty, 0.35, 1, 700);
        await _cardWait.Task;
        _cardWait = null;
        InputBlocker.Cursor = null;
        Anim.Set(TideCardPrompt, OpacityProperty, 1);
        RenderHint();

        await Task.WhenAll(
            Anim.To(TideCardOverlay, OpacityProperty, 0, 260),
            Anim.To(TideCardShift, TranslateTransform.YProperty, -40, 260, Anim.EaseOut));
        TideCardOverlay.Visibility = Visibility.Collapsed;
    }

    private static string Steps(int n) => n == 1 ? "1 step" : $"{n} steps";

    private void AddCardRow(string glyph, string text, Brush? colour = null)
    {
        var row = new DockPanel { Margin = new Thickness(0, 3, 0, 3) };
        var icon = new TextBlock { Text = glyph, Width = 24, FontSize = 15, Foreground = colour ?? Art.Danger, VerticalAlignment = VerticalAlignment.Top };
        DockPanel.SetDock(icon, Dock.Left);
        row.Children.Add(icon);
        row.Children.Add(new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap, FontSize = 12.5 });
        TideCardRows.Children.Add(row);
    }

    // ------------------------------------------------------------------ banner

    private int _bannerToken;

    private async Task ShowBannerAsync(string title, string sub, Brush colour, double holdMs = 1000)
    {
        int token = ++_bannerToken;
        BannerTitle.Text = title;
        BannerTitle.Foreground = colour;
        BannerSub.Text = sub;
        PhaseBanner.BorderBrush = colour;
        Anim.Set(BannerScale, ScaleTransform.ScaleXProperty, 0.85);
        Anim.Set(BannerScale, ScaleTransform.ScaleYProperty, 0.85);
        await Task.WhenAll(
            Anim.To(PhaseBanner, OpacityProperty, 1, 220),
            Anim.To(BannerScale, ScaleTransform.ScaleXProperty, 1, 320, Anim.Back),
            Anim.To(BannerScale, ScaleTransform.ScaleYProperty, 1, 320, Anim.Back));
        await Anim.Wait(holdMs);
        if (token != _bannerToken) return; // a newer banner took over
        await Anim.To(PhaseBanner, OpacityProperty, 0, 320);
    }

    // ------------------------------------------------------------------ phase tracker + hint

    private void SetStage(Stage stage)
    {
        _stage = stage;
        RenderStage();
    }

    private void RenderStage()
    {
        var g = _game;
        RoundNumber.Text = $"ROUND {g.Round}";
        var sc = g.Scenario;
        TideLeftText.Text = g.InFinale ? sc.FinaleName.ToUpperInvariant() : $"{g.TideCardsLeft} {sc.DeckName.ToLowerInvariant()}{(g.TideCardsLeft == 1 ? "" : "s")} left";
        EndTurnSub.Text = $"{sc.Killer.NameLower}'s turn  ·  press E";
        HeaderDifficulty.Text = _inProgress ? $"{g.Difficulty}" : "";
        TideLeftText.Foreground = g.InFinale ? Art.Danger : g.TideCardsLeft <= 3 ? Art.Warm : Art.Dim;
        RoundBadge.BorderBrush = g.InFinale ? Art.Danger : Art.Warm;

        string she = sc.Killer.She.ToUpperInvariant();
        string[] labels = ["YOUR TURN", sc.DeckName.ToUpperInvariant(), $"{she} MOVES", $"{she} ATTACKS"];
        string[] glyphs = ["☀", "▤", "≋", "⚔"];
        PhaseChips.Children.Clear();
        for (int i = 0; i < labels.Length; i++)
        {
            var s = (Stage)i;
            bool active = s == _stage;
            bool done = (int)s < (int)_stage;
            bool tide = i > 0;
            Brush accent = tide ? Art.Danger : Art.Warm;

            if (i > 0)
                PhaseChips.Children.Add(new TextBlock { Text = "›", FontSize = 20, Foreground = Art.Dim, Margin = new Thickness(6, -3, 6, 0), VerticalAlignment = VerticalAlignment.Center });

            var chip = new Border
            {
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(12, 4, 12, 4),
                BorderThickness = new Thickness(active ? 2 : 1),
                BorderBrush = active ? accent : Art.Edge,
                Background = active ? (tide ? Art.Hex("#3A1C17") : Art.Hex("#3A3018")) : Art.Hex("#0F1C21"),
                Child = new TextBlock
                {
                    Text = (done ? "✓ " : glyphs[i] + "  ") + labels[i],
                    FontWeight = active ? FontWeights.Bold : FontWeights.Normal,
                    FontSize = 12.5,
                    Foreground = active ? accent : done ? Art.Dim : Art.Hex("#5F7676")
                }
            };
            if (active)
            {
                chip.Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = ((SolidColorBrush)accent).Color,
                    BlurRadius = 14,
                    ShadowDepth = 0,
                    Opacity = 0.7
                };
            }
            PhaseChips.Children.Add(chip);
        }
        RenderHint();
    }

    private void RenderHint()
    {
        var g = _game;
        string text;
        Brush brush = Art.Danger;
        var sc = g.Scenario;
        if (_stage == Stage.TideCard) text = $"A {sc.DeckName.ToLowerInvariant()} decides what {sc.Killer.NameLower} does next.";
        else if (_stage == Stage.SheMoves) text = $"{sc.Killer.Name} is moving…";
        else if (_stage == Stage.SheStrikes) text = $"{_game.Scenario.Killer.She} attacks!";
        else (text, brush) = YourTurnHint(g);
        HintText.Text = text;
        HintText.Foreground = brush;
    }

    private (string, Brush) YourTurnHint(Game g)
    {
        var sc = g.Scenario;
        string she = sc.Killer.She;
        if (g.AwaitingStart) return ("Read the objective, then begin the night.", Art.Warm);
        if (g.Phase == Phase.Won) return ($"You did it. {sc.Place} is quiet again.", Art.Accent);
        if (g.Phase == Phase.Lost) return ($"{sc.Killer.Name} has taken you.", Art.Danger);

        if (g.InPlanning)
            return (g.Hand.Count >= Game.HandLimit
                ? $"Planning: your hand is full ({Game.HandLimit}). Discard for Time, or END TURN (E)."
                : $"Planning: buy cards for next turn with your {g.Time} Time. Unspent Time is lost. Then END TURN (E).", Art.Warm);

        bool fightCard = g.Hand.Any(c => c.Effect is CardEffect.Shove or CardEffect.WildSwing or CardEffect.DesperatePlan);
        if (g.TidewifeHere)
            return (fightCard
                ? $"{she} is right beside you! Play an attack card, use an item, or get away before the {sc.RoundWord}."
                : $"{she} is right beside you and you hold no attack card. Use an item, move away, or brace yourself.", Art.Danger);
        if (g.InFinale)
            return ($"The {sc.FinaleName}: {sc.Killer.SheLower} hunts only you. Arm yourself and finish it.", Art.Danger);
        if (g.Villagers[g.SurvivorLocation] > 0 && g.EscortTokens >= g.EscortCost)
            return ($"{Plural(g.Villagers[g.SurvivorLocation], sc.Victim + " is", sc.Victims + " are")} here. Choose Escort, then click toward the {sc.SanctuaryName} (glowing paths).", Art.Warm);
        if (g.Villagers[g.SurvivorLocation] > 0 && g.Hand.Any(c => c.Effect is CardEffect.Guide or CardEffect.Shepherd))
            return ($"{Plural(g.Villagers[g.SurvivorLocation], sc.Victim + " is", sc.Victims + " are")} here. Play Lead or Shepherd to escort them.", Art.Warm);
        if (g.MoveTokens > 0)
            return ($"{Plural(g.MoveTokens, "move", "moves")} ready. Click a glowing place to go there.", Art.TextColor);
        if (g.SwarmHere > 0)
            return (g.SwarmHere == 1
                ? $"A {sc.SwarmName} is here. Alone it only hisses, but more will come. An attack card kills one per success."
                : $"{g.SwarmHere} {sc.SwarmNames} are on you and will nip for {Game.SwarmDamage(g.SwarmHere)}. Play an attack card (one dies per success), or get away.",
                g.SwarmHere == 1 ? Art.Warm : Art.Danger);
        if (g.TidewifeNear)
            return ($"{she} is one step away. Bar the door, get clear, or get ready to fight.", Art.Warm);
        if (!Enumerable.Range(0, g.Hand.Count).Any(g.CanPlay) && g.MoveTokens == 0 && g.EscortTokens == 0)
            return ("Nothing left to play. END ACTIONS (E) to buy cards in Planning.", Art.Warm);
        return ("Action phase: play cards from your hand (Walk to move, Search, Attack...), then END ACTIONS (E).", Art.TextColor);
    }

    private static string Plural(int n, string one, string many) => n == 1 ? $"1 {one}" : $"{n} {many}";
}
