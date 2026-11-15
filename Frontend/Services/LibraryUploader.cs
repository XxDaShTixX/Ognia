using Ognia.Frontend.Helpers;
using Ognia.Frontend.Models.Local;
using Ognia.Frontend.Models.Playlist;
using Ognia.Frontend.Repos;

namespace Ognia.Frontend.Services;

/// <summary>
/// Pushes a local guest library to the backend as the current user's library.
/// Reuses existing server tracks by (SourceName, SourceId).
/// </summary>
public class LibraryUploader
{
    private readonly IMusicRepo _repo;

    public LibraryUploader(IMusicRepo repo)
    {
        _repo = repo;
    }

    /// <summary>
    /// Uploads the local library. Returns (playlistsUploaded, tracksSkipped, error).
    /// </summary>
    public async Task<(int PlaylistsUploaded, int TracksSkipped, string? Error)> UploadAsync(
        LocalLibraryFileModel library,
        IProgress<string>? progress = null)
    {
        var playlistsUploaded = 0;
        var tracksSkipped = 0;

        // Map local playlist ID -> server playlist ID as we create them.
        var playlistIdMap = new Dictionary<int, int>();

        foreach (var localPlaylist in library.Playlists)
        {
            progress?.Report($"Creating playlist '{localPlaylist.Name}'…");

            var createResult = await _repo.CreatePlaylistAsync(
                new CreatePlaylistRequest { Name = localPlaylist.Name });

            if (!createResult.Success || createResult.Data == null)
                return (playlistsUploaded, tracksSkipped, $"Failed to create playlist '{localPlaylist.Name}': {createResult.Error}");

            playlistIdMap[localPlaylist.Id] = createResult.Data.Id;
            playlistsUploaded++;

            foreach (var link in localPlaylist.Tracks.OrderBy(t => t.Position))
            {
                var localTrack = library.Tracks.FirstOrDefault(t => t.Id == link.TrackId);
                if (localTrack == null) continue;

                progress?.Report($"Adding '{localTrack.Name}'…");

                // Reconstruct a URL from the source. Currently only YouTube is supported.
                var url = TrackSourceUrlResolver.BuildUrl(localTrack.SourceName, localTrack.SourceId);
                if (url == null)
                {
                    tracksSkipped++;
                    continue;
                }

                var addResult = await _repo.AddTracksAsync(createResult.Data.Id, url);

                if (!addResult.Success)
                {
                    // "Track already in this playlist" is a benign conflict — count it and move on.
                    tracksSkipped++;
                }
            }
        }

        return (playlistsUploaded, tracksSkipped, null);
    }
}