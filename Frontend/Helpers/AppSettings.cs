using Ognia.Frontend.Enums;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;

namespace Ognia.Frontend.Helpers;

public class AppSettings
{
    // Singleton instance of the current settings.
    public static AppSettings Current => _current ??= Load();

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public AppMode? Mode { get; set; }
    public string MusicRootFolder { get; set; } = string.Empty;
    public string ActiveTheme { get; set; } = "Default.json";

    public ApiSettings Api { get; set; } = new();
    public PlaybackSettings Playback { get; set; } = new();
    public WindowSettings Window { get; set; } = new();
    public UxSettings Ux { get; set; } = new();

    private static readonly string SettingsPath = Path.Combine(AppPaths.AppDataFolder, "settings.json");
    private static AppSettings? _current;

    private static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch { }
        return new AppSettings();
    }

    public static void Save()
    {
        Directory.CreateDirectory(AppPaths.AppDataFolder);
        var json = JsonSerializer.Serialize(Current, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(SettingsPath, json);
    }
}

public class ApiSettings
{
    public string BaseUrl { get; set; } = "https://localhost:7235/";
}

public class PlaybackSettings
{
    public double Volume { get; set; } = 1.0;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public RepeatMode RepeatMode { get; set; } = RepeatMode.Off;

    public bool Shuffle { get; set; } = false;

    public int? LastTrackId { get; set; }

    /// <summary>Seconds into the last track when it was paused/closed.</summary>
    public double LastTrackPosition { get; set; } = 0;
}

public class WindowSettings
{
    public double Width { get; set; } = 1200;
    public double Height { get; set; } = 800;
    public double? Left { get; set; }
    public double? Top { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public WindowState State { get; set; } = WindowState.Normal;
}

public class UxSettings
{
    public int? LastOpenedPlaylistId { get; set; }
}