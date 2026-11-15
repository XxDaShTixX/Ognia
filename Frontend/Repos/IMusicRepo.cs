using Ognia.Frontend.Models.Playlist;
using Ognia.Frontend.Models.Track;

namespace Ognia.Frontend.Repos
{
    /// <summary>
    /// The data operations the UI needs, independent of whether they are
    /// served by the backend API (account mode) or local storage (guest mode).
    /// Account-only concerns (login, token refresh) are not part of this interface.
    /// </summary>
    public interface IMusicRepo
    {
        #region Playlists
        Task<(bool Success, string? Error, List<PlaylistResponse>? Data)> GetPlaylistsAsync();
        Task<(bool Success, string? Error, PlaylistResponse? Data)> CreatePlaylistAsync(CreatePlaylistRequest request);
        Task<(bool Success, string? Error)> RenamePlaylistAsync(int id, RenamePlaylistRequest request);
        Task<(bool Success, string? Error)> DeletePlaylistAsync(int id);
        Task<(bool Success, string? Error, PlaylistDetailResponse? Data)> GetPlaylistDetailAsync(int id);
        #endregion

        #region Tracks
        Task<(bool Success, string? Error, int Added, int Skipped)> AddTracksAsync(int playlistId, string url, IProgress<string>? progress = null);
        Task<(bool Success, string? Error)> RemoveTrackAsync(int playlistId, int trackId);
        Task<(bool Success, string? Error)> ReorderTracksAsync(int playlistId, List<int> orderedTrackIds);
        Task<(bool Success, string? Error)> RenameTrackAsync(int trackId, RenameTrackRequest request);
        Task<(bool Success, string? Error, TrackUsageResponse? Data)> GetTrackUsageAsync(int trackId);
        #endregion
    }
}
