using Ognia.Wpf.Helpers;
using Ognia.Wpf.Models.Track;
using YoutubeExplode;

namespace Ognia.Wpf.Local;

/// <summary>
/// Client-side YouTube search. Mirrors YouTubeUrlParser — same library, same
/// single responsibility. No backend involvement; both account and guest modes
/// use this.
/// </summary>
public class YouTubeSearch
{
    private readonly YoutubeClient _client = new();

    public string SourceName => "youtube";

    public async Task<IReadOnlyList<TrackResponse>> SearchAsync(
    string query, int limit = 20, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query)) return Array.Empty<TrackResponse>();

        var results = new List<TrackResponse>();
        await foreach (var video in _client.Search.GetResultsAsync(query, ct))
        {
            if (video is not YoutubeExplode.Search.VideoSearchResult v) continue;
            if (string.IsNullOrWhiteSpace(v.Id.Value)) continue;

            results.Add(new TrackResponse
            {
                Id = 0,
                SourceName = SourceName,
                SourceId = v.Id.Value,
                Name = v.Title,
                ThumbnailUrl = v.Thumbnails
                    .OrderByDescending(t => t.Resolution.Width)
                    .Select(t => t.Url)
                    .FirstOrDefault() ?? string.Empty,
                Duration = v.Duration ?? TimeSpan.Zero,
                Position = 0
            });

            if (results.Count >= limit) break;
        }

        // Download thumbnails in parallel, after all results are collected.
        var downloads = results.Select(async track =>
        {
            if (string.IsNullOrWhiteSpace(track.ThumbnailUrl)) return;
            var localPath = await ThumbnailCache.GetTempAsync(
                track.ThumbnailUrl, track.SourceName, track.SourceId, ct);
            if (localPath != null)
                track.ThumbnailUrl = localPath;
        });

        await Task.WhenAll(downloads);

        return results;
    }
}