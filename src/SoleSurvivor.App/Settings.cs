using System.IO;
using System.Text.Json;
using SoleSurvivor.Core;

namespace SoleSurvivor.App;

/// <summary>Player options, saved to %AppData%\SoleSurvivor\settings.json.</summary>
public sealed class Settings
{
    public Difficulty Difficulty { get; set; } = Difficulty.Normal;
    public bool FastAnimations { get; set; }
    public bool AmbientEffects { get; set; } = true;
    public bool ShowHints { get; set; } = true;
    public bool Fullscreen { get; set; }
    /// <summary>Windowed size in screen pixels, "WIDTHxHEIGHT". The game scales to fit whatever size the window is.</summary>
    public string WindowSize { get; set; } = "1600x900";

    /// <summary>The window sizes offered in Options (screen pixels).</summary>
    public static readonly (int W, int H)[] WindowSizes = [(1280, 720), (1366, 768), (1600, 900), (1920, 1080), (2560, 1440)];

    public static string SizeKey(int w, int h) => $"{w}x{h}";

    /// <summary>The saved window size, or 1600x900 if it is missing or malformed.</summary>
    public (int W, int H) ParsedWindowSize
    {
        get
        {
            var parts = (WindowSize ?? "").Split('x');
            if (parts.Length == 2 && int.TryParse(parts[0], out int w) && int.TryParse(parts[1], out int h)
                && w is >= 640 and <= 7680 && h is >= 360 and <= 4320)
                return (w, h);
            return (1600, 900);
        }
    }
    public string LastScenario { get; set; } = "gallowmouth";
    public bool Muted { get; set; }
    /// <summary>Master volume, 0 to 1.</summary>
    public double Volume { get; set; } = 0.8;
    public bool AmbientSound { get; set; } = true;

    private static string AppData => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

    private static string FilePath => Path.Combine(AppData, "SoleSurvivor", "settings.json");

    /// <summary>Where settings lived before the game was renamed from Low Tide (read once, if the new file doesn't exist).</summary>
    private static string LegacyPath => Path.Combine(AppData, "LowTide", "settings.json");

    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    public static Settings Load()
    {
        try
        {
            string? path = File.Exists(FilePath) ? FilePath : File.Exists(LegacyPath) ? LegacyPath : null;
            if (path is not null)
                return JsonSerializer.Deserialize<Settings>(File.ReadAllText(path)) ?? new Settings();
        }
        catch (Exception)
        {
            // A corrupt or unreadable settings file just means defaults.
        }
        return new Settings();
    }

    /// <summary>Best effort: returns false if the file couldn't be written.</summary>
    public bool Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, Json));
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public static string Location => FilePath;
}
