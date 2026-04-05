using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Mvc;
using SpotifyDownloader.Scripts.Core;
using SpotifyDownloader.Scripts.Data;
using SpotifyDownloader.Scripts.Features.Config;

namespace SpotifyDownloader.Scripts.Controllers
{
    [ApiController]
    [Route("api")]
    public class ConfigController : ControllerBase
    {
        private readonly ConfigManager _configManager;
        private readonly DownloadDatabase _database;
        private readonly ToolPaths _toolPaths;

        public ConfigController(ConfigManager configManager, DownloadDatabase database, ToolPaths toolPaths)
        {
            _configManager = configManager;
            _database = database;
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

        [HttpPost("update-ytdlp")]
        public async Task<IActionResult> UpdateYtDlp()
        {
            try
            {
                var result = await _toolPaths.UpdateYtDlpAsync();
                return Ok(new { updated = result.Updated, already_up_to_date = result.AlreadyUpToDate, message = result.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // ===== OPEN FOLDER =====

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
