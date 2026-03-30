using System.Diagnostics;
using SpotifyDownloader.Scripts.Core;
using SpotifyDownloader.Scripts.Data;
using SpotifyDownloader.Scripts.Features.Config;
using SpotifyDownloader.Scripts.Features.Spotify;

namespace SpotifyDownloader.Scripts.Features.Download
{
    public class DownloadService
    {
        private const int MaxConcurrentDownloads = 3;
        private const int MaxRetries = 2;
        private const int DownloadTimeoutSeconds = 120;

        private readonly DownloadDatabase _database;
        private readonly DownloadStateManager _stateManager;
        private readonly ToolPaths _toolPaths;
        private readonly ConfigManager _configManager;

        private CancellationTokenSource? _cancellationSource;

        public DownloadService(DownloadDatabase database, DownloadStateManager stateManager, ToolPaths toolPaths, ConfigManager configManager)
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

        public async Task DownloadTracksAsync(List<SpotifyTrack> tracks, string downloadFolder)
        {
            using CancellationTokenSource cts = new();
            _cancellationSource = cts;
            CancellationToken token = cts.Token;

            _stateManager.StartDownload(tracks.Count);

            SemaphoreSlim semaphore = new(MaxConcurrentDownloads);
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
                        // expected when cancellation is requested during semaphore wait
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

        private async Task DownloadSingleTrackAsync(SpotifyTrack track, string downloadFolder)
        {
            _stateManager.UpdateCurrentTrack($"{track.Artist} - {track.Name}");

            string safeName = FileHelper.SanitizeFilename(track.Name);
            string basePath = Path.Combine(downloadFolder, safeName);
            string finalPath = FileHelper.GetUniqueFilepath(basePath);
            string outputTemplate = Path.Combine(downloadFolder, $"{safeName}.%(ext)s");

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

                    startInfo.ArgumentList.Add($"ytsearch5:{track.Artist} {track.Name}");
                    startInfo.ArgumentList.Add("--match-filter");
                    startInfo.ArgumentList.Add("title!~='(?i)(live|concert|tour|session|karaoke|at the |at a )'");
                    startInfo.ArgumentList.Add("--max-downloads");
                    startInfo.ArgumentList.Add("1");
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
                    startInfo.ArgumentList.Add("--no-video");
                    startInfo.ArgumentList.Add("--quiet");
                    startInfo.ArgumentList.Add("--no-warnings");

                    if (_configManager.SponsorBlock)
                    {
                        startInfo.ArgumentList.Add("--sponsorblock-remove");
                        startInfo.ArgumentList.Add("sponsor,selfpromo,interaction");
                    }

                    using Process process = new();
                    process.StartInfo = startInfo;
                    process.Start();

                    await process.WaitForExitAsync(TimeSpan.FromSeconds(DownloadTimeoutSeconds));

                    string expectedFile = Path.Combine(downloadFolder, $"{safeName}.mp3");

                    if (File.Exists(expectedFile))
                    {
                        if (expectedFile != finalPath)
                        {
                            File.Move(expectedFile, finalPath, true);
                        }

                        if (!string.IsNullOrEmpty(track.AlbumArtUrl) || _configManager.NormalizeVolume)
                        {
                            bool artEmbedded = await MediaEmbedder.EmbedMetadataAndArtAsync(
                                finalPath, _toolPaths.Ffmpeg, downloadFolder,
                                null, null, null, null,
                                track.AlbumArtUrl,
                                _configManager.NormalizeVolume);

                            Console.WriteLine(artEmbedded
                                ? $"✓ Album art embedded for: {track.Name}"
                                : $"⚠ Could not embed album art for: {track.Name}");
                        }

                        _database.Add(track.Id, track.Name, track.Artist, finalPath);
                        _stateManager.AddSuccess(track.Name, track.Artist);
                        return;
                    }

                    if (attempt < MaxRetries)
                    {
                        continue;
                    }

                    _stateManager.AddFailure(track.Name, track.Artist, $"File not created after {MaxRetries + 1} attempts");
                    return;
                }
                catch (Exception ex)
                {
                    if (attempt < MaxRetries)
                    {
                        continue;
                    }

                    _stateManager.AddFailure(track.Name, track.Artist, ex.Message);
                    return;
                }
            }
        }
    }
}
