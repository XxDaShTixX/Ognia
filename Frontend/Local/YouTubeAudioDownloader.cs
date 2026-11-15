using System.IO;
using Ognia.Frontend.Helpers;
using Ognia.Frontend.ViewModels;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace Ognia.Frontend.Local;

public class YouTubeAudioDownloader
{
    private readonly YoutubeClient _youtube = new();

    /// <summary>
    /// Downloads a track's audio into the shared music root.
    /// Returns the saved file path.
    /// </summary>
    public async Task<string> DownloadAsync(
        TrackRowViewModel model,
        IProgress<double>? progress = null,
        CancellationToken ct = default)
    {
        var root = AppSettings.Current.MusicRootFolder;
        if (string.IsNullOrWhiteSpace(root))
            throw new InvalidOperationException("Music root folder is not configured.");

        var folder = Path.Combine(root, LocalLibrary.SanitizeFolderName(model.PlaylistName));
        Directory.CreateDirectory(folder);

        var targetPath = Path.Combine(folder, LocalLibrary.BuildFileName(model.Name));

        // Already downloaded in this playlist — skip.
        if (model.LocalFilePath != null) return targetPath;

        var manifest = await _youtube.Videos.Streams.GetManifestAsync(model.SourceId, ct);
        var audioStream = manifest.GetAudioOnlyStreams().GetWithHighestBitrate();

        if (progress != null)
        {
            await _youtube.Videos.Streams.DownloadAsync(
                audioStream, targetPath,
                new Progress<double>(p => progress.Report(p)), ct);
        }
        else
        {
            await _youtube.Videos.Streams.DownloadAsync(audioStream, targetPath, null, ct);
        }

        return targetPath;
    }
}