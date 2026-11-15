using Ognia.Frontend.Enums;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ognia.Frontend.Helpers;

public class AppSettings
{
    /// <summary>
    /// Persisted as a string so the file stays readable; see AppModeConverter.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public AppMode? Mode { get; set; }

    public static AppSettings Current => _current ??= Load();
    public string MusicRootFolder { get; set; } = string.Empty;
    public string ActiveTheme { get; set; } = "Default.json";

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
        catch
        {
            // Corrupt settings file — fall through to defaults.
        }
        return new AppSettings();
    }

    public static void Save()
    {
        Directory.CreateDirectory(AppPaths.AppDataFolder);
        var json = JsonSerializer.Serialize(Current, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(SettingsPath, json);
    }
}