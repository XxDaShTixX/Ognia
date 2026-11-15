using Ognia.Frontend.Models.Track;

namespace Ognia.Frontend.Models.Playlist
{
    public class PlaylistDetailResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<TrackResponse> Tracks { get; set; } = new();
    }
}
