using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Ognia.Wpf.Models.Playlist;

public class PlaylistResponse : INotifyPropertyChanged
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int PlaylistTracksCount { get; set; }
    public List<string> ThumbnailUrls { get; set; } = new();
    public List<CoverThumbnail> CoverThumbnails { get; set; } = new();

    private string? _cachedCoverPath;
    public string? CachedCoverPath
    {
        get => _cachedCoverPath;
        set
        {
            if (_cachedCoverPath == value) return;
            _cachedCoverPath = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public class CoverThumbnail
{
    public string Url { get; set; } = string.Empty;
    public string SourceName { get; set; } = string.Empty;
    public string SourceId { get; set; } = string.Empty;
}