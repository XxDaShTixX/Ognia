namespace Ognia.Frontend.Models.Local;

public class TrackMetadataModel
{
    public string Name { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
}