using System.Diagnostics;
using SpotifyDownloader.Scripts.Core;
using SpotifyDownloader.Scripts.Data;
using SpotifyDownloader.Scripts.Features.Config;

namespace SpotifyDownloader.Scripts.Features.YouTubeMusic
{
    public class YoutubeMusicDownloadService
    {
        private const int MaxConcurrentDownloads = 3;
        private const int MaxRetries = 2;
        private const int DownloadTimeoutSeconds = 120;

        private readonly DownloadDatabase _database;
        private readonly YtmDownloadStateManager _stateManager;
        private readonly ToolPaths _toolPaths;
        private readonly ConfigManager _configManager;

        private CancellationTokenSource? _cancellationSource;

        public YoutubeMusicDownloadService(
            DownloadDatabase database,
            YtmDownloadStateManager stateManager,
            ToolPaths toolPaths,
            ConfigManager configManager)
        {
            _database = database;
            _stateManager = stateManager;
            _toolPaths = toolPaths;
            _configManager = configManager;
        }

        public void Cancel()
        {
            _cancellationSource?.Cancel();
        }

        public async Task DownloadTracksAsync(List<YoutubeMusicTrack> tracks, string downloadFolder)
        {
            using CancellationTokenSource cts = new();
            _cancellationSource = cts;
            CancellationToken token = cts.Token;

            _stateManager.StartDownload(tracks.Count);

            using SemaphoreSlim semaphore = new(MaxConcurrentDownloads);
            List<Task> tasks = [];

            foreach (var track in tracks)
            {
                tasks.Add(Task.Run(async () =>
                {
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }

                    bool acquired = false;

                    try
                    {
                        await semaphore.WaitAsync(token);
                        acquired = true;

                        if (token.IsCancellationRequested)
                        {
                            return;
                        }

                        await DownloadSingleTrackAsync(track, downloadFolder);
                    }
                    catch (OperationCanceledException)
                    {
                        // expected on cancellation
                    }
                    finally
                    {
                        if (acquired)
                        {
                            semaphore.Release();
                        }
                    }
                }));
            }

            await Task.WhenAll(tasks);

            bool cancelled = token.IsCancellationRequested;
            _stateManager.FinishDownload(cancelled);
            _cancellationSource = null;
        }

        private async Task DownloadSingleTrackAsync(YoutubeMusicTrack track, string downloadFolder)
        {
            _stateManager.UpdateCurrentTrack($"{track.Artist} - {track.Title}");

            string safeName = FileHelper.SanitizeFilename(track.Title);
            string basePath = Path.Combine(downloadFolder, safeName);
            string finalPath = FileHelper.GetUniqueFilepath(basePath);
            string outputTemplate = Path.Combine(downloadFolder, $"{safeName}.%(ext)s");
            string videoUrl = $"https://www.youtube.com/watch?v={track.VideoId}";

            for (int attempt = 0; attempt <= MaxRetries; attempt++)
            {
                try
                {
                    ProcessStartInfo startInfo = new()
                    {
                        FileName = _toolPaths.YtDlp,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    startInfo.ArgumentList.Add(videoUrl);
                    startInfo.ArgumentList.Add("--ffmpeg-location");
                    startInfo.ArgumentList.Add(_toolPaths.Ffmpeg);
                    startInfo.ArgumentList.Add("--format");
                    startInfo.ArgumentList.Add("bestaudio/best");
                    startInfo.ArgumentList.Add("--extract-audio");
                    startInfo.ArgumentList.Add("--audio-format");
                    startInfo.ArgumentList.Add("mp3");
                    startInfo.ArgumentList.Add("--audio-quality");
                    startInfo.ArgumentList.Add("0");
                    startInfo.ArgumentList.Add("--add-metadata");
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

                    if (!string.IsNullOrWhiteSpace(_configManager.YtmCookiesBrowser))
                    {
                        startInfo.ArgumentList.Add("--cookies-from-browser");
                        startInfo.ArgumentList.Add(_configManager.YtmCookiesBrowser);
                    }

                    using Process process = new();
                    process.StartInfo = startInfo;
                    process.Start();

                    bool completed = await process.WaitForExitAsync(TimeSpan.FromSeconds(DownloadTimeoutSeconds));

                    if (!completed)
                    {
                        process.Kill(entireProcessTree: true);

                        if (attempt < MaxRetries)
                        {
                            continue;
                        }

                        _stateManager.AddFailure(track.Title, track.Artist, "Download timed out");
                        return;
                    }

                    string expectedFile = Path.Combine(downloadFolder, $"{safeName}.mp3");

                    if (File.Exists(expectedFile))
                    {
                        if (expectedFile != finalPath)
                        {
                            File.Move(expectedFile, finalPath, true);
                        }

                        if (!string.IsNullOrEmpty(track.ThumbnailUrl) || _configManager.NormalizeVolume)
                        {
                            bool artEmbedded = await MediaEmbedder.EmbedMetadataAndArtAsync(
                                finalPath, _toolPaths.Ffmpeg, downloadFolder,
                                null, null, null, null,
                                track.ThumbnailUrl,
                                _configManager.NormalizeVolume);

                            Console.WriteLine(artEmbedded
                                ? $"✓ Album art embedded for: {track.Title}"
                                : $"⚠ Could not embed album art for: {track.Title}");
                        }

                        _database.Add(track.VideoId, track.Title, track.Artist, finalPath, "ytmusic");
                        _stateManager.AddSuccess(track.Title, track.Artist);
                        return;
                    }

                    if (attempt < MaxRetries)
                    {
                        continue;
                    }

                    _stateManager.AddFailure(track.Title, track.Artist, $"File not created after {MaxRetries + 1} attempts");
                    return;
                }
                catch (Exception ex)
                {
                    if (attempt < MaxRetries)
                    {
                        continue;
                    }

                    _stateManager.AddFailure(track.Title, track.Artist, ex.Message);
                    return;
                }
            }
        }
    }
}
