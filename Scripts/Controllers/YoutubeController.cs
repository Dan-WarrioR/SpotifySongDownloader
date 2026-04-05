using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SpotifyDownloader.Scripts.Core;
using SpotifyDownloader.Scripts.Features.Config;
using SpotifyDownloader.Scripts.Features.YouTube;

namespace SpotifyDownloader.Scripts.Controllers
{
    [ApiController]
    [Route("api/youtube")]
    public class YoutubeController : ControllerBase
    {
        private readonly ConfigManager _configManager;
        private readonly YoutubeDownloadService _youtubeDownloadService;
        private readonly YoutubeStateManager _youtubeStateManager;
        private readonly ToolPaths _toolPaths;

        public YoutubeController(
            ConfigManager configManager,
            YoutubeDownloadService youtubeDownloadService,
            YoutubeStateManager youtubeStateManager,
            ToolPaths toolPaths)
        {
            _configManager = configManager;
            _youtubeDownloadService = youtubeDownloadService;
            _youtubeStateManager = youtubeStateManager;
            _toolPaths = toolPaths;
        }

        [HttpGet("info")]
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

        [HttpPost("download")]
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

        [HttpGet("progress")]
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
    }
}
