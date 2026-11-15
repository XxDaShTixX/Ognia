using Ognia.Backend.DTOs.Track;

namespace Ognia.Backend.DTOs.Playlist
{
    public class PlaylistDetailResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<TrackResponse> Tracks { get; set; } = new();
    }
}
