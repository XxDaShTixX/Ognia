using Ognia.Wpf.Models.Playlist;

namespace Ognia.Wpf.Navigation;

/// <summary>
/// The single seam through which any component requests a change of
/// main-pane or right-pane content. Implementations raise the events;
/// hosts (e.g. MainWindow / ShellView) subscribe and swap content.
/// No concrete view types cross this boundary.
/// </summary>
public interface IShellNavigator
{
    event Action<object>? MainContentChanged;
    event Action<object?>? RightContentChanged;

    void ShowLibrary();
    void ShowPlaylist(PlaylistResponse playlist);
    void ShowSettings();
    void ShowSearch();

    void ShowTrackDetails(object? trackViewModel);
    void ClearRightPane();
}