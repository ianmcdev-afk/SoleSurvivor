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
        foreach (var block in Descendants(this).OfType<TextBlock>())
        {
            if (!block.Text.Contains('{')) continue;
            block.Text = scenario.Fill(block.Text
                    .Replace("{intro}", intro)
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
