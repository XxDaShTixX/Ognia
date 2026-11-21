namespace Ognia.Wpf.Models.Local;

public class LocalPlaylistModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<LocalPlaylistTrackModel> Tracks { get; set; } = new();
}
