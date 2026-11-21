using System.IO;
using System.Text.Json;

namespace Ognia.Wpf.Helpers;

public sealed class PlaybackSessionState
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private static PlaybackSessionState? _instance;

    public static PlaybackSessionState Instance => _instance ??= Load();

    public int? LastTrackId { get; set; }
    public double LastTrackPosition { get; set; }
    public int? LastOpenedPlaylistId { get; set; }

    private static string FilePath =>
        Path.Combine(Path.GetDirectoryName(AppPaths.SettingsFilePath)!, "session.json");

    private static PlaybackSessionState Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath);
                return JsonSerializer.Deserialize<PlaybackSessionState>(json) ?? new();
            }
        }
        catch { }

        return new();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, JsonOptions));
        }
        catch { }
    }
}