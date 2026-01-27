using System.Diagnostics;
using System.Runtime.InteropServices;
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
            string ytdlpCmd = GetExecutablePath("yt-dlp");
            string ffmpegPath = GetExecutablePath("ffmpeg");
        
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
                            bool artEmbedded = await EmbedAlbumArtAsync(finalPath, track.AlbumArtUrl, downloadFolder, safeName);
                            if (artEmbedded)
                            {
                                Console.WriteLine($"✓ Album art embedded for: {track.Name}");
                            }
                            else
                            {
                                Console.WriteLine($"⚠ Could not embed album art for: {track.Name}");
                            }
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
    
        private async Task<bool> EmbedAlbumArtAsync(string audioFile, string albumArtUrl, string downloadFolder, string safeName)
        {
            string artworkPath = Path.Combine(downloadFolder, $"{safeName}_cover.jpg");
            string tempOutput = Path.Combine(downloadFolder, $"{safeName}_temp.mp3");
        
            try
            {
                Console.WriteLine($"Downloading album art for: {safeName}");
                using HttpClient client = new();
                client.Timeout = TimeSpan.FromSeconds(30);
                var imageData = await client.GetByteArrayAsync(albumArtUrl);
                await File.WriteAllBytesAsync(artworkPath, imageData);
                
                Console.WriteLine($"Album art downloaded: {artworkPath} ({imageData.Length} bytes)");
            
                string ffmpegCmd = GetExecutablePath("ffmpeg");
            
                ProcessStartInfo startInfo = new()
                {
                    FileName = ffmpegCmd,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                
                startInfo.ArgumentList.Add("-i");
                startInfo.ArgumentList.Add(audioFile);
                startInfo.ArgumentList.Add("-i");
                startInfo.ArgumentList.Add(artworkPath);
                startInfo.ArgumentList.Add("-map");
                startInfo.ArgumentList.Add("0:a");
                startInfo.ArgumentList.Add("-map");
                startInfo.ArgumentList.Add("1:0");
                startInfo.ArgumentList.Add("-c:a");
                startInfo.ArgumentList.Add("copy");
                startInfo.ArgumentList.Add("-c:v");
                startInfo.ArgumentList.Add("mjpeg");
                startInfo.ArgumentList.Add("-disposition:v");
                startInfo.ArgumentList.Add("attached_pic");
                startInfo.ArgumentList.Add("-metadata:s:v");
                startInfo.ArgumentList.Add("title=Album cover");
                startInfo.ArgumentList.Add("-metadata:s:v");
                startInfo.ArgumentList.Add("comment=Cover (front)");
                startInfo.ArgumentList.Add("-id3v2_version");
                startInfo.ArgumentList.Add("3");
                startInfo.ArgumentList.Add("-y");
                startInfo.ArgumentList.Add(tempOutput);
            
                Console.WriteLine($"Running ffmpeg to embed album art...");
                
                using Process process = new();
                process.StartInfo = startInfo;
                
                string stdOutput = "";
                string stdError = "";
                
                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        stdOutput += e.Data + "\n";
                };
                
                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        stdError += e.Data + "\n";
                };
                
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                
                bool completed = await process.WaitForExitAsync(TimeSpan.FromSeconds(30));
            
                if (!completed)
                {
                    Console.WriteLine($"✗ ffmpeg timeout for {safeName}");
                    return false;
                }
                
                if (process.ExitCode != 0)
                {
                    Console.WriteLine($"✗ ffmpeg failed with exit code {process.ExitCode}");
                    Console.WriteLine($"Error output: {stdError}");
                    return false;
                }
                
                if (!File.Exists(tempOutput))
                {
                    Console.WriteLine($"✗ Output file not created: {tempOutput}");
                    return false;
                }
                
                FileInfo tempInfo = new(tempOutput);
                if (tempInfo.Length == 0)
                {
                    Console.WriteLine($"✗ Output file is empty");
                    File.Delete(tempOutput);
                    return false;
                }
                
                Console.WriteLine($"✓ Album art embedded successfully. Replacing original file...");
                
                await Task.Delay(200);
                
                try
                {
                    File.Delete(audioFile);
                    File.Move(tempOutput, audioFile);
                    Console.WriteLine($"✓ File replaced: {audioFile}");
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"✗ Failed to replace file: {ex.Message}");
                    if (File.Exists(tempOutput) && !File.Exists(audioFile))
                    {
                        File.Move(tempOutput, audioFile);
                        return true;
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Exception embedding album art for {safeName}: {ex.Message}");
                return false;
            }
            finally
            {
                if (File.Exists(artworkPath))
                {
                    try
                    {
                        File.Delete(artworkPath);
                        Console.WriteLine($"Cleaned up: {artworkPath}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"⚠ Could not delete artwork file: {ex.Message}");
                    }
                }
                
                if (File.Exists(tempOutput))
                {
                    try
                    {
                        File.Delete(tempOutput);
                        Console.WriteLine($"Cleaned up: {tempOutput}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"⚠ Could not delete temp file: {ex.Message}");
                    }
                }
            }
        }
    
        private static string GetExecutablePath(string executable)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                string exePath = $"{executable}.exe";
                if (File.Exists(exePath))
                {
                    return Path.GetFullPath(exePath);
                }
            }
            
            if (File.Exists(executable))
            {
                return Path.GetFullPath(executable);
            }
            
            return executable;
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
            return CheckCommandExists("yt-dlp") || File.Exists("yt-dlp.exe") || File.Exists("yt-dlp");
        }
    
        public static bool CheckFfmpeg()
        {
            return CheckCommandExists("ffmpeg") || File.Exists("ffmpeg.exe") || File.Exists("ffmpeg");
        }
    
        private static bool CheckCommandExists(string command)
        {
            try
            {
                string executable = command;
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    executable = $"{command}.exe";
                }
                
                ProcessStartInfo startInfo = new()
                {
                    FileName = executable,
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
}