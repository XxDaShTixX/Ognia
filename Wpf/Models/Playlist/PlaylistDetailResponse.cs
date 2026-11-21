using Ognia.Wpf.Models.Track;

namespace Ognia.Wpf.Models.Playlist
{
    public class PlaylistDetailResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<TrackResponse> Tracks { get; set; } = new();
    }
}
