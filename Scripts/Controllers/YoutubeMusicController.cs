using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using SpotifyDownloader.Scripts.Data;
using SpotifyDownloader.Scripts.Features.Config;
using SpotifyDownloader.Scripts.Features.YouTubeMusic;

namespace SpotifyDownloader.Scripts.Controllers
{
    [ApiController]
    [Route("api/ytmusic")]
    public class YoutubeMusicController : ControllerBase
    {
        private readonly ConfigManager _configManager;
        private readonly DownloadDatabase _database;
        private readonly YoutubeMusicClient _ytmClient;
        private readonly YoutubeMusicDownloadService _ytmDownloadService;
        private readonly YtmDownloadStateManager _ytmStateManager;

        public YoutubeMusicController(
            ConfigManager configManager,
            DownloadDatabase database,
            YoutubeMusicClient ytmClient,
            YoutubeMusicDownloadService ytmDownloadService,
            YtmDownloadStateManager ytmStateManager)
        {
            _configManager = configManager;
            _database = database;
            _ytmClient = ytmClient;
            _ytmDownloadService = ytmDownloadService;
            _ytmStateManager = ytmStateManager;
        }

        [HttpPost("download")]
        public async Task<IActionResult> StartDownload(
            [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] DownloadRequest? request)
        {
            if (_ytmStateManager.GetState().InProgress)
            {
                return Conflict(new { error = "YouTube Music download already in progress" });
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
                : _configManager.YtmPlaylistIds;

            if (playlistIds.Count == 0)
            {
                return BadRequest(new { error = "No YouTube Music playlists configured" });
            }

            List<YoutubeMusicTrack> allTracks = [];

            foreach (string playlistId in playlistIds)
            {
                var tracks = await _ytmClient.FetchPlaylistTracksAsync(playlistId, _configManager.YtmCookiesBrowser);
                allTracks.AddRange(tracks);
            }

            allTracks = allTracks.DistinctBy(t => t.VideoId).ToList();

            if (allTracks.Count == 0)
            {
                return NotFound(new { error = "No tracks found or failed to fetch playlists — check playlist IDs and auth settings" });
            }

            var newTracks = allTracks.Where(t => !_database.IsDownloaded(t.VideoId)).ToList();

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
                    await _ytmDownloadService.DownloadTracksAsync(newTracks, downloadFolder);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[YTMusic] Fatal download error: {ex}");
                    _ytmStateManager.FinishDownload();
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
            _ytmDownloadService.Cancel();
            return Ok(new { success = true });
        }

        [HttpGet("progress")]
        public IActionResult GetProgress()
        {
            var state = _ytmStateManager.GetState();
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
    }
}
