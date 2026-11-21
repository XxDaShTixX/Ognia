namespace Ognia.Wpf.Models.Local;

public class LocalPlaylistTrackModel
{
    public int TrackId { get; set; }
    public int Position { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}
