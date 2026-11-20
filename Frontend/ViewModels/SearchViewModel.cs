using Ognia.Frontend.Helpers;
using Ognia.Frontend.Local;
using Ognia.Frontend.Models.Playlist;
using Ognia.Frontend.Models.Track;
using Ognia.Frontend.Session;
using System.Collections.ObjectModel;
using System.Windows;

namespace Ognia.Frontend.ViewModels;

public sealed class SearchViewModel : ViewModelBase
{
    private readonly AppSession _session;
    private readonly YouTubeSearch _youtube = new();

    public SearchViewModel(AppSession session)
    {
        _session = session;
        _session.Playback.ErrorOccurred += msg => _session.SetError(msg);

        SearchCommand = new RelayCommand(
            async _ => await SearchAsync(),
            _ => !string.IsNullOrWhiteSpace(Query) && !IsSearching);

        AddToPlaylistCommand = new RelayCommand(
            async p => await AddToPlaylistAsync((SearchResultRow)p!),
            p => p is SearchResultRow row && row.SelectedPlaylist != null && !_session.IsBusy);

        PreviewCommand = new RelayCommand(
            async p => await PreviewAsync((SearchResultRow)p!),
            p => p is SearchResultRow && !IsPreviewing);

        _session.Playback.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != nameof(PlaybackViewModel.NowPlaying)) return;
            var np = _session.Playback.NowPlaying;
            if (string.IsNullOrEmpty(np)) SetPlaying(null);
        };

        _session.Playback.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != nameof(PlaybackViewModel.PlaybackState)) return;
            if (_session.Playback.PlaybackState == NAudio.Wave.PlaybackState.Stopped)
                SetPlaying(null);
        };

        _session.Playlists.CollectionChanged += (_, _) =>
        {
            foreach (var row in Results)
                SyncRowPlaylists(row);
        };

        _ = _session.LoadPlaylistsAsync();
    }

    private string _query = string.Empty;
    public string Query
    {
        get => _query;
        set
        {
            if (!SetProperty(ref _query, value)) return;
            System.Windows.Input.CommandManager.InvalidateRequerySuggested();
        }
    }

    private bool _isSearching;
    public bool IsSearching
    {
        get => _isSearching;
        private set
        {
            if (!SetProperty(ref _isSearching, value)) return;
            System.Windows.Input.CommandManager.InvalidateRequerySuggested();
        }
    }

    private bool _isPreviewing;
    public bool IsPreviewing
    {
        get => _isPreviewing;
        private set
        {
            if (!SetProperty(ref _isPreviewing, value)) return;
            System.Windows.Input.CommandManager.InvalidateRequerySuggested();
        }
    }

    private string? _status;
    public string? Status
    {
        get => _status;
        private set => SetProperty(ref _status, value);
    }

    public ObservableCollection<SearchResultRow> Results { get; } = new();

    public ObservableCollection<PlaylistResponse> Playlists => _session.Playlists;

    public RelayCommand SearchCommand { get; }
    public RelayCommand PreviewCommand { get; }
    public RelayCommand AddToPlaylistCommand { get; }

    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(Query)) return;

        IsSearching = true;
        Status = "Searching…";
        Results.Clear();
        _session.SetStatus($"Searching for '{Query.Trim()}'…");

        try
        {
            var hits = await _youtube.SearchAsync(Query.Trim());
            foreach (var t in hits)
                Results.Add(new SearchResultRow(t, Playlists));

            Status = Results.Count == 0
                ? "No results."
                : $"{Results.Count} result(s).";

            _session.SetStatus(Results.Count == 0
                ? $"No results for '{Query.Trim()}'."
                : $"Found {Results.Count} result(s) for '{Query.Trim()}'.");
        }
        catch (Exception ex)
        {
            Status = $"Search failed: {ex.Message}";
            _session.SetError($"Search failed: {ex.Message}");
        }
        finally
        {
            IsSearching = false;
        }
    }

    private async Task PreviewAsync(SearchResultRow row)
    {
        IsPreviewing = true;
        _session.SetStatus($"Preparing preview of '{row.Name}'…");

        try
        {
            var downloader = new PreviewDownloader();
            var progress = new Progress<double>(p =>
                _session.SetStatus($"Downloading '{row.Name}' — {p:P0}"));

            var path = await downloader.DownloadAsync(row.Track, progress);
            var url = TrackSourceUrlResolver.BuildUrl(row.SourceName, row.SourceId);
            _session.Playback.PlayLocalFile(path, row.Name, row.ThumbnailUrl, url);
            SetPlaying(row.SourceId);
            _session.SetStatus($"Previewing '{row.Name}'.");
        }
        catch (Exception ex)
        {
            _session.SetError($"Preview failed: {ex.Message}");
        }
        finally
        {
            IsPreviewing = false;
        }
    }

    private string? _playingSourceId;
    private void SetPlaying(string? sourceId)
    {
        _playingSourceId = sourceId;
        foreach (var row in Results)
            row.IsPlaying = string.Equals(row.SourceId, sourceId, StringComparison.Ordinal);
    }

    private async Task AddToPlaylistAsync(SearchResultRow row)
    {
        var playlist = row.SelectedPlaylist;
        if (playlist == null) return;

        var playlistId = playlist.Id;
        var playlistName = playlist.Name;

        var url = TrackSourceUrlResolver.BuildUrl(row.SourceName, row.SourceId);
        if (url == null)
        {
            _session.SetError("Could not build URL for this track.");
            return;
        }

        _session.SetStatus($"Adding '{row.Name}' to {playlistName}…");

        await _session.RunBusyAsync(async () =>
        {
            var result = await _session.Repo.AddTracksAsync(playlistId, url);

            if (!result.Success)
            {
                _session.SetError(result.Error ?? "Failed to add track.");
                return;
            }

            if (result.Added == 0)
            {
                _session.SetStatus($"'{row.Name}' already in this playlist.");
                return;
            }

            PlaylistCoverCache.Invalidate(playlistId);
            await _session.LoadPlaylistsAsync();
            _session.SetStatus($"Added '{row.Name}' to {playlistName}.");
        });
    }

    private void SyncRowPlaylists(SearchResultRow row)
    {
        row.Playlists.Clear();
        foreach (var p in _session.Playlists)
            row.Playlists.Add(p);
    }
}

public sealed class SearchResultRow : ViewModelBase
{
    private PlaylistResponse? _selectedPlaylist;

    public SearchResultRow(TrackResponse track, IEnumerable<PlaylistResponse> playlists)
    {
        Track = track;
        Playlists = new ObservableCollection<PlaylistResponse>(playlists);
    }

    public TrackResponse Track { get; }
    public ObservableCollection<PlaylistResponse> Playlists { get; }

    private bool _isPlaying;
    public bool IsPlaying
    {
        get => _isPlaying;
        set => SetProperty(ref _isPlaying, value);
    }

    public PlaylistResponse? SelectedPlaylist
    {
        get => _selectedPlaylist;
        set
        {
            if (!SetProperty(ref _selectedPlaylist, value)) return;
            System.Windows.Input.CommandManager.InvalidateRequerySuggested();
        }
    }

    public string? PreviewThumbnail => ThumbnailUrl;   // local path after download
    public string? SourceUrl => TrackSourceUrlResolver.BuildUrl(SourceName, SourceId);
    public string Name => Track.Name;
    public string SourceName => Track.SourceName;
    public string SourceId => Track.SourceId;
    public string ThumbnailUrl => Track.ThumbnailUrl;
    public TimeSpan Duration => Track.Duration;
    public string DurationLabel => Duration.TotalHours >= 1
        ? $"{(int)Duration.TotalHours}:{Duration.Minutes:00}:{Duration.Seconds:00}"
        : Duration.ToString(@"mm\:ss");
}