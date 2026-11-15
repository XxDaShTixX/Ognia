namespace Ognia.Backend.DTOs.Playlist;

public class ReorderTracksRequest
{
    public List<int> TrackIds { get; set; } = new();
}