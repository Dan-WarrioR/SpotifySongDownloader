using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using SpotifyDownloader.Scripts.Core;
using SpotifyDownloader.Scripts.Data;
using SpotifyDownloader.Scripts.Features.Config;
using SpotifyDownloader.Scripts.Features.Download;
using SpotifyDownloader.Scripts.Features.Spotify;
using SpotifyDownloader.Scripts.Features.YouTube;

namespace SpotifyDownloader.Scripts.Controllers
{
    public class DownloadRequest
    {
        public string? PlaylistId { get; set; }
    }

    [ApiController]
    [Route("api")]
    public class ApiController : ControllerBase
    {
        private readonly ConfigManager _configManager;
        private readonly DownloadDatabase _database;
        private readonly SpotifyClient _spotifyClient;
        private readonly DownloadService _downloadService;
        private readonly DownloadStateManager _stateManager;
        private readonly YoutubeDownloadService _youtubeDownloadService;
        private readonly YoutubeStateManager _youtubeStateManager;
        private readonly ToolPaths _toolPaths;

        public ApiController(
            ConfigManager configManager,
            DownloadDatabase database,
            SpotifyClient spotifyClient,
            DownloadService downloadService,
            DownloadStateManager stateManager,
            YoutubeDownloadService youtubeDownloadService,
            YoutubeStateManager youtubeStateManager,
            ToolPaths toolPaths)
        {
            _configManager = configManager;
            _database = database;
            _spotifyClient = spotifyClient;
            _downloadService = downloadService;
            _stateManager = stateManager;
            _youtubeDownloadService = youtubeDownloadService;
            _youtubeStateManager = youtubeStateManager;
            _toolPaths = toolPaths;
        }

        // ===== CONFIG =====

        [HttpGet("config")]
        public IActionResult GetConfig()
        {
            try
            {
                return Ok(_configManager.Config);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("config")]
        public IActionResult SaveConfig([FromBody] ConfigData config)
        {
            try
            {
                _configManager.Save(config);
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // ===== TOOLS =====

        [HttpGet("check-ytdlp")]
        public IActionResult CheckYtDlp()
        {
            return Ok(new { installed = ToolPaths.CheckYtDlp() });
        }

        [HttpGet("check-ffmpeg")]
        public IActionResult CheckFfmpeg()
        {
            return Ok(new { installed = ToolPaths.CheckFfmpeg() });
        }

        [HttpPost("open-folder")]
        public IActionResult OpenFolder()
        {
            string folder = _configManager.DownloadFolder;

            if (string.IsNullOrEmpty(folder))
            {
                return BadRequest(new { error = "Download folder not configured" });
            }

            if (!Directory.Exists(folder))
            {
                return BadRequest(new { error = "Download folder does not exist" });
            }

            try
            {
                string command = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                    ? "explorer.exe"
                    : RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                        ? "open"
                        : "xdg-open";

                Process.Start(new ProcessStartInfo(command, folder) { UseShellExecute = true });

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // ===== SPOTIFY DOWNLOAD =====

        [HttpPost("download")]
        public async Task<IActionResult> StartDownload(
            [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] DownloadRequest? request)
        {
            if (_stateManager.GetState().InProgress)
            {
                return Conflict(new { error = "Download already in progress" });
            }

            string downloadFolder = _configManager.DownloadFolder;

            if (string.IsNullOrEmpty(downloadFolder))
            {
                return BadRequest(new { error = "Download folder not configured" });
            }

            if (!Directory.Exists(downloadFolder))
            {
                try
                {
                    Directory.CreateDirectory(downloadFolder);
                }
                catch (Exception ex)
                {
                    return BadRequest(new { error = $"Could not create folder: {ex.Message}" });
                }
            }

            List<string> playlistIds = request?.PlaylistId != null
                ? new List<string> { request.PlaylistId }
                : _configManager.PlaylistIds;

            if (playlistIds.Count == 0)
            {
                return BadRequest(new { error = "No playlists configured" });
            }

            string? token = await _spotifyClient.GetAccessTokenAsync();

            if (token == null)
            {
                return Unauthorized(new { error = "Failed to authenticate with Spotify" });
            }

            List<SpotifyTrack> allTracks = [];

            foreach (string playlistId in playlistIds)
            {
                var tracks = await _spotifyClient.GetPlaylistTracksAsync(token, playlistId);
                allTracks.AddRange(tracks);
            }

            allTracks = allTracks.DistinctBy(t => t.Id).ToList();

            if (allTracks.Count == 0)
            {
                return NotFound(new { error = "No tracks found or failed to fetch playlists" });
            }

            var newTracks = allTracks.Where(t => !_database.IsDownloaded(t.Id)).ToList();

            if (newTracks.Count == 0)
            {
                return Ok(new
                {
                    message = "All tracks already downloaded",
                    total = allTracks.Count,
                    @new = 0,
                    already_downloaded = allTracks.Count
                });
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    await _downloadService.DownloadTracksAsync(newTracks, downloadFolder);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Spotify] Fatal download error: {ex}");
                    _stateManager.FinishDownload();
                }
            });

            return Ok(new
            {
                message = "Download started",
                total = allTracks.Count,
                @new = newTracks.Count,
                already_downloaded = allTracks.Count - newTracks.Count
            });
        }

        [HttpPost("download/cancel")]
        public IActionResult CancelDownload()
        {
            _downloadService.Cancel();
            return Ok(new { success = true });
        }

        [HttpGet("download/progress")]
        public IActionResult GetProgress()
        {
            var state = _stateManager.GetState();
            return Ok(new
            {
                in_progress = state.InProgress,
                is_cancelled = state.IsCancelled,
                current_track = state.CurrentTrack,
                progress = state.Progress,
                total = state.Total,
                completed = state.Completed,
                failed = state.Failed,
                results = state.Results
            });
        }

        // ===== PLAYLIST INFO =====

        [HttpGet("playlist-name")]
        public async Task<IActionResult> GetPlaylistName([FromQuery] string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest(new { error = "Playlist ID is required" });
            }

            string? token = await _spotifyClient.GetAccessTokenAsync();

            if (token == null)
            {
                return Unauthorized(new { error = "Failed to authenticate with Spotify" });
            }

            string? name = await _spotifyClient.GetPlaylistNameAsync(token, id);

            return Ok(new { name = name ?? id });
        }

        // ===== YOUTUBE DOWNLOAD =====

        [HttpGet("youtube/info")]
        public async Task<IActionResult> GetYoutubeInfo([FromQuery] string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return BadRequest(new { error = "URL is required" });
            }

            try
            {
                ProcessStartInfo startInfo = new()
                {
                    FileName = _toolPaths.YtDlp,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                startInfo.ArgumentList.Add("--print");
                startInfo.ArgumentList.Add("title");
                startInfo.ArgumentList.Add("--print");
                startInfo.ArgumentList.Add("uploader");
                startInfo.ArgumentList.Add("--no-playlist");
                startInfo.ArgumentList.Add("--quiet");
                startInfo.ArgumentList.Add(url);

                using Process process = new();
                process.StartInfo = startInfo;
                process.Start();

                Task<string> readTask = process.StandardOutput.ReadToEndAsync();
                bool completed = await process.WaitForExitAsync(TimeSpan.FromSeconds(30));

                if (!completed)
                {
                    return BadRequest(new { error = "Timed out fetching video info" });
                }

                string output = await readTask;
                string[] lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                string title = lines.Length > 0 ? lines[0].Trim() : "";
                string uploader = lines.Length > 1 ? lines[1].Trim() : "";

                if (string.IsNullOrEmpty(title))
                {
                    return BadRequest(new { error = "Could not fetch video info — URL may be invalid or unavailable" });
                }

                return Ok(new { title, uploader });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("youtube/download")]
        public IActionResult StartYoutubeDownload([FromBody] YoutubeDownloadRequest request)
        {
            if (_youtubeStateManager.GetState().InProgress)
            {
                return Conflict(new { error = "YouTube download already in progress" });
            }

            if (string.IsNullOrWhiteSpace(request.Url))
            {
                return BadRequest(new { error = "URL is required" });
            }

            string downloadFolder = !string.IsNullOrWhiteSpace(request.DownloadFolder)
                ? request.DownloadFolder
                : _configManager.DownloadFolder;

            if (string.IsNullOrEmpty(downloadFolder))
            {
                return BadRequest(new { error = "Download folder not configured. Set it in Configuration or provide it in the request." });
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    await _youtubeDownloadService.DownloadAsync(request, downloadFolder);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[YouTube] Fatal download error: {ex}");
                    _youtubeStateManager.Fail(ex.Message);
                }
            });

            return Ok(new { message = "YouTube download started" });
        }

        [HttpGet("youtube/progress")]
        public IActionResult GetYoutubeProgress()
        {
            var state = _youtubeStateManager.GetState();
            return Ok(new
            {
                in_progress = state.InProgress,
                current_title = state.CurrentTitle,
                success = state.Success,
                file_path = state.FilePath,
                error = state.Error
            });
        }

        // ===== HISTORY / STATS =====

        [HttpPost("clear-history")]
        public IActionResult ClearHistory()
        {
            try
            {
                _database.ClearAll();
                return Ok(new { success = true, message = "Download history cleared successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("stats")]
        public IActionResult GetStats()
        {
            return Ok(new
            {
                total_downloaded = _database.TotalDownloaded(),
                recent = _database.GetRecentTracks(10)
            });
        }
    }
}