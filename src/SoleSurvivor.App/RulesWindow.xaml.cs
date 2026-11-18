using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SoleSurvivor.Core;

namespace SoleSurvivor.App;

public partial class RulesWindow : Window
{
    /// <summary>The rules, worded for the scenario being played.</summary>
    public RulesWindow(ScenarioDef scenario)
    {
        InitializeComponent();
        Title = $"How to play: {scenario.Title}";
        string intro = $"{scenario.Blurb} You are {scenario.Survivor.Name}, {scenario.Survivor.Role.ToLowerInvariant()}. {scenario.Killer.Name}: {scenario.Killer.Bio}";
        string swarm = scenario.HasSwarm
            ? $"{{Killer}} has a brood of {scenario.SwarmNames}. {scenario.SwarmStart} start on the board. On each {{deck}} where {{she}} attacks, another crawls out where {{she}} stands, up to {scenario.SwarmMax} at once (and one more every turn of the {{finale}}). "
              + $"After {{her}} move, every group of {scenario.SwarmNames} creeps one step towards you. They're brave in numbers: one on its own only hisses, two or three nip you for 1, and four or more for 2. Holding your breath takes 1 off, and the {{salt}} keeps them back too. A barred door keeps them out. "
              + $"Weak Attack, Wild Swing and Desperate Plan can be played when {scenario.SwarmNames} are in your space, even without {{killer}}: each success kills one. They shield {{her}}: in {{her}} space, successes kill {scenario.SwarmNames} first, and the rest wound {{her}}. The {{flare}} also burns up to 2 {scenario.SwarmNames} in your space. When {{killer}} dies, the whole brood dies with {{her}}."
            : "";
        if (!scenario.HasSwarm)
        {
            SwarmHeading.Visibility = Visibility.Collapsed;
            SwarmRules.Visibility = Visibility.Collapsed;
        }
        foreach (var block in Descendants(this).OfType<TextBlock>())
        {
            if (!block.Text.Contains('{')) continue;
            block.Text = scenario.Fill(block.Text
                    .Replace("{intro}", intro)
                    .Replace("{swarm}", swarm)
                    .Replace("{TITLE}", scenario.Title.ToUpperInvariant())
                    .Replace("{DECK}", scenario.DeckName.ToUpperInvariant())
                    .Replace("{SHE}", scenario.Killer.She.ToUpperInvariant())
                    .Replace("{deck}", scenario.DeckName.ToLowerInvariant())
                    .Replace("{finale}", scenario.FinaleName)
                    .Replace("{rope}", Items.All[ItemKind.Rope].Name)
                    .Replace("{flare}", Items.All[ItemKind.FlareGun].Name)
                    .Replace("{salt}", Items.All[ItemKind.BlessedSalt].Name));
        }
    }

    private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
    {
        // Logical tree: the window isn't rendered yet, so the visual tree is empty.
        foreach (var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())
        {
            yield return child;
            foreach (var d in Descendants(child)) yield return d;
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
