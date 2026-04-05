using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using SpotifyDownloader.Scripts.Data;
using SpotifyDownloader.Scripts.Features.Config;
using SpotifyDownloader.Scripts.Features.Download;
using SpotifyDownloader.Scripts.Features.Spotify;

namespace SpotifyDownloader.Scripts.Controllers
{
    [ApiController]
    [Route("api")]
    public class SpotifyController : ControllerBase
    {
        private readonly ConfigManager _configManager;
        private readonly DownloadDatabase _database;
        private readonly SpotifyClient _spotifyClient;
        private readonly DownloadService _downloadService;
        private readonly DownloadStateManager _stateManager;

        public SpotifyController(
            ConfigManager configManager,
            DownloadDatabase database,
            SpotifyClient spotifyClient,
            DownloadService downloadService,
            DownloadStateManager stateManager)
        {
            _configManager = configManager;
            _database = database;
            _spotifyClient = spotifyClient;
            _downloadService = downloadService;
            _stateManager = stateManager;
        }

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
    }
}
