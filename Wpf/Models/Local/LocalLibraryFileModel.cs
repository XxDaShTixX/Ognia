namespace Ognia.Wpf.Models.Local;

/// <summary>
/// The on-disk shape of the guest library. Kept separate from the DTOs so the
/// file format can evolve without changing the API contracts.
/// </summary>
public class LocalLibraryFileModel
{
    public int SchemaVersion { get; set; } = 1;
    public List<LocalPlaylistModel> Playlists { get; set; } = new();
    public List<LocalTrackModel> Tracks { get; set; } = new();
}





