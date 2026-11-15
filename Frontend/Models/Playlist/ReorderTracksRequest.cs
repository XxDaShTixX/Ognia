namespace Ognia.Frontend.Models.Playlist;

public class ReorderTracksRequest
{
    public List<int> TrackIds { get; set; } = new();
}