using System.Diagnostics;
using SpotifyDownloader.Scripts.Core;
using SpotifyDownloader.Scripts.Data;
using SpotifyDownloader.Scripts.Features.Config;

namespace SpotifyDownloader.Scripts.Features.YouTube
{
    public class YoutubeDownloadService
    {
        private const int DownloadTimeoutSeconds = 120;

        private readonly ToolPaths _toolPaths;
        private readonly YoutubeStateManager _stateManager;
        private readonly DownloadDatabase _database;
        private readonly ConfigManager _configManager;

        public YoutubeDownloadService(ToolPaths toolPaths, YoutubeStateManager stateManager, DownloadDatabase database, ConfigManager configManager)
        {
            _toolPaths = toolPaths;
            _stateManager = stateManager;
            _database = database;
            _configManager = configManager;
        }

        public async Task DownloadAsync(YoutubeDownloadRequest request, string downloadFolder)
        {
            string displayName = !string.IsNullOrWhiteSpace(request.Title)
                ? $"{request.Artist} - {request.Title}".Trim(' ', '-')
                : request.Url;

            _stateManager.Start(displayName);

            try
            {
                if (!Directory.Exists(downloadFolder))
                {
                    Directory.CreateDirectory(downloadFolder);
                }

                string baseName = !string.IsNullOrWhiteSpace(request.Title)
                    ? FileHelper.SanitizeFilename($"{request.Artist} - {request.Title}".Trim(' ', '-'))
                    : FileHelper.SanitizeFilename("youtube_download");

                string finalPath = FileHelper.GetUniqueFilepath(Path.Combine(downloadFolder, baseName));
                string safeName = Path.GetFileNameWithoutExtension(finalPath);
                string outputTemplate = Path.Combine(downloadFolder, $"{safeName}.%(ext)s");
                string expectedFile = Path.Combine(downloadFolder, $"{safeName}.mp3");

                ProcessStartInfo startInfo = new()
                {
                    FileName = _toolPaths.YtDlp,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                startInfo.ArgumentList.Add(request.Url);
                startInfo.ArgumentList.Add("--ffmpeg-location");
                startInfo.ArgumentList.Add(_toolPaths.Ffmpeg);
                startInfo.ArgumentList.Add("--format");
                startInfo.ArgumentList.Add("bestaudio/best");
                startInfo.ArgumentList.Add("--extract-audio");
                startInfo.ArgumentList.Add("--audio-format");
                startInfo.ArgumentList.Add("mp3");
                startInfo.ArgumentList.Add("--audio-quality");
                startInfo.ArgumentList.Add("0");
                startInfo.ArgumentList.Add("-o");
                startInfo.ArgumentList.Add(outputTemplate);
                startInfo.ArgumentList.Add("--no-playlist");
                startInfo.ArgumentList.Add("--quiet");
                startInfo.ArgumentList.Add("--no-warnings");

                if (_configManager.SponsorBlock)
                {
                    startInfo.ArgumentList.Add("--sponsorblock-remove");
                    startInfo.ArgumentList.Add("all");
                }

                using Process process = new();
                process.StartInfo = startInfo;
                process.Start();

                bool completed = await process.WaitForExitAsync(TimeSpan.FromSeconds(DownloadTimeoutSeconds));

                if (!completed)
                {
                    _stateManager.Fail("Download timed out");
                    return;
                }

                if (!File.Exists(expectedFile))
                {
                    _stateManager.Fail("Output file was not created — the URL may be invalid or unavailable");
                    return;
                }

                bool hasMetadata = !string.IsNullOrWhiteSpace(request.Title)
                    || !string.IsNullOrWhiteSpace(request.Artist)
                    || !string.IsNullOrWhiteSpace(request.Album)
                    || !string.IsNullOrWhiteSpace(request.Year);

                bool hasArt = !string.IsNullOrWhiteSpace(request.CoverArtUrl);

                if (hasMetadata || hasArt || _configManager.NormalizeVolume)
                {
                    bool embedded = await MediaEmbedder.EmbedMetadataAndArtAsync(
                        expectedFile, _toolPaths.Ffmpeg, downloadFolder,
                        request.Title, request.Artist, request.Album, request.Year,
                        request.CoverArtUrl,
                        _configManager.NormalizeVolume);

                    if (!embedded)
                    {
                        Console.WriteLine($"⚠ Could not embed metadata/art for: {displayName}");
                    }
                }

                _database.Add(request.Url, displayName, request.Artist, expectedFile, "youtube");
                _stateManager.Complete(expectedFile);
                Console.WriteLine($"✓ YouTube download complete: {expectedFile}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ YouTube download error: {ex.Message}");
                _stateManager.Fail(ex.Message);
            }
        }
    }
}
