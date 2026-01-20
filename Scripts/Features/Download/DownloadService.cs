using System.Diagnostics;
using System.Text.RegularExpressions;
using SpotifyDownloader.Scripts.Data;
using SpotifyDownloader.Scripts.Features.Spotify;

namespace SpotifyDownloader.Scripts.Features.Download
{
    public static class ProcessExtensions
    {
        public static async Task<bool> WaitForExitAsync(this Process process, TimeSpan timeout)
        {
            using CancellationTokenSource cts = new(timeout);
        
            try
            {
                await process.WaitForExitAsync(cts.Token);
                return true;
            }
            catch (OperationCanceledException)
            {
                try
                {
                    process.Kill(true);
                }
                catch
                {
                    
                }
            
                return false;
            }
        }
    }
    
    public class DownloadService
    {
        private const int MaxConcurrentDownloads = 3;
        private const int MaxRetries = 2;
        private const int DownloadTimeoutSeconds = 120;
    
        private readonly DownloadDatabase _database;
        private readonly DownloadStateManager _stateManager;
    
        public DownloadService(DownloadDatabase database, DownloadStateManager stateManager)
        {
            _database = database;
            _stateManager = stateManager;
        }
    
        public async Task DownloadTracksAsync(List<SpotifyTrack> tracks, string downloadFolder)
        {
            _stateManager.StartDownload(tracks.Count);
        
            SemaphoreSlim semaphore = new(MaxConcurrentDownloads);
            List<Task> tasks = [];
        
            foreach (var track in tracks)
            {
                tasks.Add(Task.Run(async () =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        await DownloadSingleTrackAsync(track, downloadFolder);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }));
            }
        
            await Task.WhenAll(tasks);
            _stateManager.FinishDownload();
        }
    
        private async Task DownloadSingleTrackAsync(SpotifyTrack track, string downloadFolder)
        {
            _stateManager.UpdateCurrentTrack($"{track.Artist} - {track.Name}");
        
            string safeName = SanitizeFilename(track.Name);
            string basePath = Path.Combine(downloadFolder, safeName);
            string finalPath = GetUniqueFilepath(basePath);
        
            string searchQuery = $"{track.Artist} {track.Name}";
            string ytdlpCmd = File.Exists("yt-dlp.exe") ? "yt-dlp.exe" : "yt-dlp";
            string ffmpegPath = File.Exists("ffmpeg.exe") ? Path.GetFullPath("ffmpeg.exe") : "ffmpeg";
        
            string outputTemplate = Path.Combine(downloadFolder, $"{safeName}.%(ext)s");
        
            List<string> args =
            [
                $"ytsearch1:{searchQuery}",
                "--ffmpeg-location", ffmpegPath,
                "--format", "bestaudio/best",
                "--extract-audio",
                "--audio-format", "mp3",
                "--audio-quality", "0",
                "--add-metadata",
                "-o", outputTemplate,
                "--no-playlist",
                "--no-video",
                "--quiet",
                "--no-warnings"
            ];
        
            for (int attempt = 0; attempt <= MaxRetries; attempt++)
            {
                try
                {
                    ProcessStartInfo startInfo = new()
                    {
                        FileName = ytdlpCmd,
                        Arguments = string.Join(" ", args.Select(arg => arg.Contains(' ') ? $"\"{arg}\"" : arg)),
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                
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
                    
                        if (!string.IsNullOrEmpty(track.AlbumArtUrl))
                        {
                            await EmbedAlbumArtAsync(finalPath, track.AlbumArtUrl, downloadFolder, safeName);
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
    
        private async Task EmbedAlbumArtAsync(string audioFile, string albumArtUrl, string downloadFolder, string safeName)
        {
            string artworkPath = Path.Combine(downloadFolder, $"{safeName}_cover.jpg");
        
            try
            {
                using HttpClient client = new();
                var imageData = await client.GetByteArrayAsync(albumArtUrl);
                await File.WriteAllBytesAsync(artworkPath, imageData);
            
                string ffmpegCmd = File.Exists("ffmpeg.exe") ? "ffmpeg.exe" : "ffmpeg";
                string tempOutput = Path.Combine(downloadFolder, $"{safeName}_temp.mp3");
            
                ProcessStartInfo startInfo = new()
                {
                    FileName = ffmpegCmd,
                    Arguments = $"-i \"{audioFile}\" -i \"{artworkPath}\" -map 0:0 -map 1:0 -c copy -id3v2_version 3 -metadata:s:v title=\"Album cover\" -metadata:s:v comment=\"Cover (front)\" \"{tempOutput}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
            
                using Process process = new();
                process.StartInfo = startInfo;
                process.Start();
                await process.WaitForExitAsync(TimeSpan.FromSeconds(30));
            
                if (File.Exists(tempOutput))
                {
                    File.Delete(audioFile);
                    File.Move(tempOutput, audioFile);
                }
            }
            finally
            {
                if (File.Exists(artworkPath))
                {
                    try
                    {
                        File.Delete(artworkPath);
                    }
                    catch
                    {
                        
                    }
                }
            }
        }
    
        private static string SanitizeFilename(string filename)
        {
            string invalid = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            string pattern = $"[{Regex.Escape(invalid)}]";
            string sanitized = Regex.Replace(filename, pattern, "_");
            sanitized = Regex.Replace(sanitized, @"\s+", " ").Trim();
        
            return sanitized.Length > 200 ? sanitized[..200] : sanitized;
        }
    
        private static string GetUniqueFilepath(string basePath)
        {
            string path = $"{basePath}.mp3";
            int counter = 1;
        
            while (File.Exists(path))
            {
                path = $"{basePath}_{counter}.mp3";
                counter++;
            }
        
            return path;
        }
    
        public static bool CheckYtDlp()
        {
            return CheckCommandExists("yt-dlp") || File.Exists("yt-dlp.exe");
        }
    
        public static bool CheckFfmpeg()
        {
            return CheckCommandExists("ffmpeg") || File.Exists("ffmpeg.exe");
        }
    
        private static bool CheckCommandExists(string command)
        {
            try
            {
                ProcessStartInfo startInfo = new()
                {
                    FileName = command,
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
            
                using Process process = new();
                process.StartInfo = startInfo;
                process.Start();
                process.WaitForExit(5000);
            
                return process.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }
    }
}
