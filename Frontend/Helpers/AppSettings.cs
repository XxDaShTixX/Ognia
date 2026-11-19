using Ognia.Frontend.Enums;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;

namespace Ognia.Frontend.Helpers;

public class AppSettings
{
    /// <summary>
    /// Singleton instance of the current settings.
    /// </summary>
    public static AppSettings Instance => _instance ??= Load();

    /// <summary>
    /// The account mode currently in use.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public AppMode? Mode { get; set; }

    /// <summary>
    /// Currently active theme. 
    /// Defaults to Default.json.
    /// </summary>
    public string ActiveTheme { get; set; } = "Default.json";

    /// <summary>
    /// The current version of the application
    /// </summary>
    public string Version { get; set; } = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? string.Empty;

    #region Settings File Structure
    public UpdateSettings Update { get; set; } = new();
    public ApiSettings Api { get; set; } = new();
    public PlaybackSettings Playback { get; set; } = new();
    public WindowSettings Window { get; set; } = new();
    public UxSettings Ux { get; set; } = new();
    #endregion

    /// <summary>
    /// The singleton instance.
    /// </summary>
    private static AppSettings? _instance;

    /// <summary>
    /// Initializes the AppSettings.
    /// </summary>
    public void Initialize()
    {
        
    }

    /// <summary>
    /// Load settings from file into instance.
    /// </summary>
    /// <returns></returns>
    private static AppSettings Load()
    {
        string SettingsPath = AppPaths.SettingsFilePath;

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

    /// <summary>
    /// Save settings back into file.
    /// </summary>
    public void Save()
    {
        string SettingsPath = AppPaths.SettingsFilePath;
        Directory.CreateDirectory(AppPaths.AppDataFolder);
        var json = JsonSerializer.Serialize(Instance, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(SettingsPath, json);
    }
}

public class UpdateSettings
{
    public bool AutoUpdateEnabled { get; set; } = true;

    /// <summary>
    /// "stable" (default) or "prerelease". Controls whether pre-releases are considered updates.
    /// </summary>
    public string Channel { get; set; } = "stable";
}

public class ApiSettings
{
    public string BaseUrl { get; set; } = "https://localhost:7235/";
}

public class PlaybackSettings
{
    /// <summary>
    /// Current volume.
    /// </summary>
    public double Volume { get; set; } = 1.0;

    /// <summary>
    /// Current repeat mode.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public RepeatMode RepeatMode { get; set; } = RepeatMode.Off;

    /// <summary>
    /// Currnet shuffle toggle.
    /// </summary>
    public bool Shuffle { get; set; } = false;

    /// <summary>
    /// Last track played.
    /// </summary>
    public int? LastTrackId { get; set; }

    /// <summary>
    /// Seconds into the last track when it was paused/closed.
    /// </summary>
    public double LastTrackPosition { get; set; } = 0;
}

public class WindowSettings
{
    public double Width { get; set; } = 1200;
    public double Height { get; set; } = 800;
    public double MiniRestingOpacity { get; set; } = 0.2;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public WindowState State { get; set; } = WindowState.Normal;
}

public class UxSettings
{
    public int? LastOpenedPlaylistId { get; set; }
}