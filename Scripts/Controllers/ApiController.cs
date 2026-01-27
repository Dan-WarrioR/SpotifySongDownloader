using Microsoft.AspNetCore.Mvc;
using SpotifyDownloader.Scripts.Data;
using SpotifyDownloader.Scripts.Features.Config;
using SpotifyDownloader.Scripts.Features.Download;
using SpotifyDownloader.Scripts.Features.Spotify;

namespace SpotifyDownloader.Scripts.Controllers
{
    [ApiController]
    [Route("api")]
    public class ApiController : ControllerBase
    {
        private readonly ConfigManager _configManager;
        private readonly DownloadDatabase _database;
        private readonly SpotifyClient _spotifyClient;
        private readonly DownloadService _downloadService;
        private readonly DownloadStateManager _stateManager;
    
        public ApiController(ConfigManager configManager, DownloadDatabase database, SpotifyClient spotifyClient, DownloadService downloadService, DownloadStateManager stateManager)
        {
            _configManager = configManager;
            _database = database;
            _spotifyClient = spotifyClient;
            _downloadService = downloadService;
            _stateManager = stateManager;
        }
    
        [HttpGet("config")]
        public IActionResult GetConfig()
        {
            try
            {
                var config = _configManager.Config;
                return Ok(new
                {
                    client_id = config.ClientId,
                    client_secret = config.ClientSecret,
                    playlist_id = config.PlaylistId,
                    download_folder = config.DownloadFolder
                });
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
                _configManager.Save(
                    config.ClientId,
                    config.ClientSecret,
                    config.PlaylistId,
                    config.DownloadFolder
                );
            
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    
        [HttpGet("check-ytdlp")]
        public IActionResult CheckYtDlp()
        {
            return Ok(new { installed = DownloadService.CheckYtDlp() });
        }
    
        [HttpGet("check-ffmpeg")]
        public IActionResult CheckFfmpeg()
        {
            return Ok(new { installed = DownloadService.CheckFfmpeg() });
        }
    
        [HttpPost("download")]
        public async Task<IActionResult> StartDownload()
        {
            var state = _stateManager.GetState();
        
            if (state.InProgress)
            {
                return Conflict(new { error = "Download already in progress" });
            }
        
            var downloadFolder = _configManager.DownloadFolder;
        
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
        
            var token = await _spotifyClient.GetAccessTokenAsync();
        
            if (token == null)
            {
                return Unauthorized(new { error = "Failed to authenticate with Spotify" });
            }
        
            var tracks = await _spotifyClient.GetPlaylistTracksAsync(token);
        
            if (tracks.Count == 0)
            {
                return NotFound(new { error = "No tracks found or failed to fetch playlist" });
            }
        
            var newTracks = tracks.Where(t => !_database.IsDownloaded(t.Id)).ToList();
        
            if (newTracks.Count == 0)
            {
                return Ok(new
                {
                    message = "All tracks already downloaded",
                    total = tracks.Count,
                    @new = 0,
                    already_downloaded = tracks.Count
                });
            }
        
            _ = Task.Run(async () =>
            {
                await _downloadService.DownloadTracksAsync(newTracks, downloadFolder);
            });
        
            return Ok(new
            {
                message = "Download started",
                total = tracks.Count,
                @new = newTracks.Count,
                already_downloaded = tracks.Count - newTracks.Count
            });
        }
    
        [HttpGet("download/progress")]
        public IActionResult GetProgress()
        {
            var state = _stateManager.GetState();
            return Ok(new
            {
                in_progress = state.InProgress,
                current_track = state.CurrentTrack,
                progress = state.Progress,
                total = state.Total,
                completed = state.Completed,
                failed = state.Failed,
                results = state.Results
            });
        }
    
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