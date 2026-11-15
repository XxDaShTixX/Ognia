using Ognia.Frontend.Models.Playlist;
using Ognia.Frontend.Navigation;
using Ognia.Frontend.Session;

namespace Ognia.Frontend.ViewModels;

public sealed class LibraryViewModel : ViewModelBase
{
    private readonly IShellNavigator _nav;

    public LibraryViewModel(AppSession session, IShellNavigator nav)
    {
        Session = session;
        _nav = nav;

        OpenPlaylistCommand = new RelayCommand(
            p => _nav.ShowPlaylist((PlaylistResponse)p!),
            p => p is PlaylistResponse);

        BeginCreatePlaylistCommand = new RelayCommand(_ => Session.BeginCreatePlaylist());

        ConfirmCreatePlaylistCommand = new RelayCommand(
            async _ => await ConfirmCreatePlaylistAsync(),
            _ => Session.IsCreatingPlaylist
                 && !string.IsNullOrWhiteSpace(Session.NewPlaylistName));

        CancelCreatePlaylistCommand = new RelayCommand(
            _ => Session.EndCreatePlaylist(),
            _ => Session.IsCreatingPlaylist);

        _ = Session.LoadPlaylistsAsync();
    }

    public AppSession Session { get; }

    public RelayCommand OpenPlaylistCommand { get; }
    public RelayCommand BeginCreatePlaylistCommand { get; }
    public RelayCommand ConfirmCreatePlaylistCommand { get; }
    public RelayCommand CancelCreatePlaylistCommand { get; }

    #region Create playlist

    private async Task ConfirmCreatePlaylistAsync()
    {
        var name = Session.NewPlaylistName.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            Session.EndCreatePlaylist();
            return;
        }

        var result = await Session.Repo.CreatePlaylistAsync(
            new CreatePlaylistRequest { Name = name });

        if (!result.Success)
        {
            Session.SetError(result.Error ?? "Failed to create playlist.");
            return;
        }

        Session.EndCreatePlaylist();
        await Session.LoadPlaylistsAsync();
        Session.SetStatus($"Created playlist: {name}");
    }

    #endregion
}