using Ognia.Frontend.Helpers;
using Ognia.Frontend.Session;
using System.Diagnostics;
using System.Windows;

namespace Ognia.Frontend.ViewModels;

/// <summary>
/// Right-pane data context. Shows only the selected track's details and actions.
/// Playlist details and actions live in the playlist header (center pane).
/// </summary>
public sealed class TrackDetailsViewModel : ViewModelBase
{
    private readonly AppSession _session;

    private string _renameInput = string.Empty;
    private bool _isRenameOpen;

    public TrackDetailsViewModel(AppSession session)
    {
        _session = session;

        RenameTrackCommand = new RelayCommand(
            _ => OpenRename(),
            _ => SelectedTrack != null);

        ConfirmRenameCommand = new RelayCommand(
            async _ => await ConfirmRenameAsync(),
            _ => _isRenameOpen
                 && !string.IsNullOrWhiteSpace(_renameInput)
                 && _renameInput.Trim() != (SelectedTrack?.Name ?? string.Empty));

        CancelRenameCommand = new RelayCommand(_ => CloseRename(), _ => _isRenameOpen);

        RemoveTrackCommand = new RelayCommand(
            async _ => await RemoveTrackAsync(),
            _ => SelectedTrack != null 
                && SelectedPlaylist != null
                && !_session.IsBusy);

        DownloadCommand = new RelayCommand(
            _ => _session.Download.DownloadTrackCommand.Execute(null),
            _ => SelectedTrack != null && !SelectedTrack.IsDownloaded && !_session.Download.IsDownloading);

        RemoveDownloadCommand = new RelayCommand(
            _ => RemoveDownload(),
            _ => SelectedTrack != null
                 && LocalLibrary.FindLocalFile(SelectedTrack.PlaylistName, SelectedTrack.Name) != null);

        OpenLocationCommand = new RelayCommand(
            _ => OpenLocation(),
            _ => SelectedTrack != null
                 && LocalLibrary.FindLocalFile(SelectedTrack.PlaylistName, SelectedTrack.Name) != null);
    }

    #region State

    private TrackRowViewModel? SelectedTrack => _session.SelectedTrack;
    private Models.Playlist.PlaylistResponse? SelectedPlaylist => _session.SelectedPlaylist;

    public bool HasTrack => SelectedTrack != null;

    public string TrackName => SelectedTrack?.Name ?? string.Empty;
    public string? TrackThumbnailPath => SelectedTrack?.ThumbnailPath;

    public TimeSpan? TrackDuration => SelectedTrack?.Duration;
    public string TrackDurationLabel
    {
        get
        {
            var d = TrackDuration ?? TimeSpan.Zero;
            return d.TotalHours >= 1
                ? $"{(int)d.TotalHours}:{d.Minutes:00}:{d.Seconds:00}"
                : d.ToString(@"mm\:ss");
        }
    }

    public string? TrackSourceUrl => SelectedTrack?.SourceUrl;

    public bool IsRenameOpen
    {
        get => _isRenameOpen;
        private set
        {
            if (SetProperty(ref _isRenameOpen, value))
                System.Windows.Input.CommandManager.InvalidateRequerySuggested();
        }
    }

    public string RenameInput
    {
        get => _renameInput;
        set
        {
            if (SetProperty(ref _renameInput, value))
                System.Windows.Input.CommandManager.InvalidateRequerySuggested();
        }
    }

    #endregion

    #region Commands

    public RelayCommand RenameTrackCommand { get; }
    public RelayCommand ConfirmRenameCommand { get; }
    public RelayCommand CancelRenameCommand { get; }
    public RelayCommand RemoveTrackCommand { get; }
    public RelayCommand DownloadCommand { get; }
    public RelayCommand RemoveDownloadCommand { get; }
    public RelayCommand OpenLocationCommand { get; }

    #endregion

    public void Refresh()
    {
        OnPropertyChanged(nameof(HasTrack));
        OnPropertyChanged(nameof(TrackName));
        OnPropertyChanged(nameof(TrackThumbnailPath));
        OnPropertyChanged(nameof(TrackDuration));
        OnPropertyChanged(nameof(TrackDurationLabel));
        OnPropertyChanged(nameof(TrackSourceUrl));
        System.Windows.Input.CommandManager.InvalidateRequerySuggested();
    }

    #region Rename

    private void OpenRename()
    {
        RenameInput = SelectedTrack?.Name ?? string.Empty;
        IsRenameOpen = true;
    }

    private void CloseRename()
    {
        IsRenameOpen = false;
        RenameInput = string.Empty;
    }

    private async Task ConfirmRenameAsync()
    {
        if (SelectedTrack == null || SelectedPlaylist == null) return;

        var newName = _renameInput.Trim();
        if (string.IsNullOrWhiteSpace(newName) || newName == SelectedTrack.Name)
        {
            CloseRename();
            return;
        }

        // Reject duplicates within the open playlist before hitting the API.
        var clash = _session.SelectedPlaylistTracks
            .Any(t => t.Id != SelectedTrack.Id
                      && string.Equals(t.Name, newName, StringComparison.OrdinalIgnoreCase));

        if (clash)
        {
            _session.SetError($"A track named \"{newName}\" already exists in this playlist.");
            return;
        }

        var trackId = SelectedTrack.Id;
        var oldName = SelectedTrack.Name;

        var result = await _session.Repo.RenameTrackAsync(
            trackId, new Models.Track.RenameTrackRequest { Name = newName });
        if (!result.Success)
        {
            _session.SetError(result.Error ?? "Failed to rename track.");
            return;
        }

        foreach (var playlist in _session.Playlists)
        {
            LocalLibrary.RenameLocalFile(playlist.Name, oldName, newName);
            LocalLibrary.RenameLocalThumbnail(playlist.Name, oldName, newName);
        }

        await _session.ReloadSelectedPlaylistTracksAsync();
        _session.SelectedTrack = _session.SelectedPlaylistTracks.FirstOrDefault(t => t.Id == trackId);

        CloseRename();
        Refresh();
        _session.SetStatus($"Renamed track to '{newName}'");
    }

    #endregion

    #region Remove / cleanup

    private async Task RemoveTrackAsync()
    {
        if (SelectedPlaylist == null || SelectedTrack == null) return;

        var removed = SelectedTrack;
        var playlistName = SelectedPlaylist.Name;

        if (MessageBox.Show(
                $"Remove \"{removed.Name}\" from \"{playlistName}\"?",
                "Remove track",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        await _session.RunBusyAsync(async () =>
        {
            var result = await _session.Repo.RemoveTrackAsync(SelectedPlaylist.Id, removed.Id);
            if (!result.Success)
            {
                _session.SetError(result.Error ?? "Failed to remove track.");
                return;
            }

            LocalLibrary.DeleteLocalFile(playlistName, removed.Name);
            LocalLibrary.DeleteLocalThumbnail(playlistName, removed.Name);

            await _session.ReloadSelectedPlaylistTracksAsync();
            _session.SelectedTrack = null;
            await _session.LoadPlaylistsAsync();

            _session.SetStatus($"Removed '{removed.Name}' from playlist");
        });
    }

    private void OpenLocation()
    {
        if (SelectedTrack == null) return;
        var path = LocalLibrary.FindLocalFile(SelectedTrack.PlaylistName, SelectedTrack.Name);
        if (path == null) return;

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{path}\"",
                UseShellExecute = true
            });
        }
        catch { }
    }

    private void RemoveDownload()
    {
        if (SelectedTrack == null) return;

        if (MessageBox.Show(
                $"Delete the downloaded file for \"{SelectedTrack.Name}\"?",
                "Remove download",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        _session.Download.RemoveDownloadCommand.Execute(null);
    }

    #endregion
}