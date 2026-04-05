using System.Diagnostics;
using SpotifyDownloader.Scripts.Data;
using SpotifyDownloader.Scripts.Features.Config;
using SpotifyDownloader.Scripts.Features.Download;
using SpotifyDownloader.Scripts.Features.Spotify;
using SpotifyDownloader.Scripts.Features.YouTube;
using SpotifyDownloader.Scripts.Features.YouTubeMusic;

namespace SpotifyDownloader.Scripts.Core
{
    public class Application
    {
        private WebApplication? _app;

        public void Run(string[] args)
        {
            try
            {
                Console.WriteLine("Initializing application...");
                InitializeDataFolder();

                Console.WriteLine("Configuring services...");
                ConfigureServices(args);

                Console.WriteLine("Configuring middleware...");
                ConfigureMiddleware();

                Console.WriteLine("Starting server...");
                StartServer();
            }
            catch (Exception ex)
            {
                Console.WriteLine("FATAL ERROR:");
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine();
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
                Environment.Exit(1);
            }
        }

        private void InitializeDataFolder()
        {
            string dataPath = Path.Combine(Directory.GetCurrentDirectory(), "Data");

            if (!Directory.Exists(dataPath))
            {
                Directory.CreateDirectory(dataPath);
            }
        }

        private void ConfigureServices(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Environment.WebRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

            builder.Services.AddRazorPages();
            builder.Services.AddControllers();

            builder.Services.AddSingleton<ToolPaths>();
            builder.Services.AddSingleton<ConfigManager>();
            builder.Services.AddSingleton<DownloadDatabase>();
            builder.Services.AddSingleton<SpotifyClient>();
            builder.Services.AddSingleton<DownloadService>();
            builder.Services.AddSingleton<DownloadStateManager>();
            builder.Services.AddSingleton<YoutubeStateManager>();
            builder.Services.AddSingleton<YoutubeDownloadService>();
            builder.Services.AddSingleton<YoutubeMusicClient>();
            builder.Services.AddSingleton<YtmDownloadStateManager>();
            builder.Services.AddSingleton<YoutubeMusicDownloadService>();

            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader();
                });
            });

            _app = builder.Build();
        }

        private void ConfigureMiddleware()
        {
            if (_app == null)
            {
                throw new InvalidOperationException("Application not configured");
            }

            _app.UseCors();
            _app.UseStaticFiles();
            _app.UseRouting();

            _app.MapRazorPages();
            _app.MapControllers();

            _app.MapGet("/", () => Results.Redirect("/Index"));
        }

        private void StartServer()
        {
            if (_app == null)
            {
                throw new InvalidOperationException("Application not configured");
            }

            Console.WriteLine("🎵 Spotify & YouTube Downloader");
            Console.WriteLine("==================================================");
            Console.WriteLine("Opening at: http://localhost:5000");
            Console.WriteLine("==================================================");

            _app.Lifetime.ApplicationStarted.Register(() =>
            {
                OpenBrowser("http://localhost:5000");
                _ = Task.Run(async () =>
                {
                    await RunYtDlpUpdateAsync();
                    await RunAutoSyncAsync();
                });
            });

            _app.Run("http://localhost:5000");
        }

        private static void OpenBrowser(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not open browser automatically: {ex.Message}");
            }
        }

        private async Task RunYtDlpUpdateAsync()
        {
            if (_app == null)
            {
                return;
            }

            Console.WriteLine("[yt-dlp] Checking for updates...");

            var toolPaths = _app.Services.GetRequiredService<ToolPaths>();
            var result = await toolPaths.UpdateYtDlpAsync();

            if (result.Updated)
            {
                Console.WriteLine($"[yt-dlp] Updated to latest version");
            }
            else if (result.AlreadyUpToDate)
            {
                Console.WriteLine("[yt-dlp] Already up to date");
            }
            else
            {
                Console.WriteLine($"[yt-dlp] Update check result: {result.Message}");
            }
        }

        private async Task RunAutoSyncAsync()
        {
            if (_app == null)
            {
                return;
            }

            var configManager = _app.Services.GetRequiredService<ConfigManager>();

            if (!configManager.AutoSync)
            {
                return;
            }

            Console.WriteLine("[Auto-sync] Checking for new tracks...");

            var spotifyClient = _app.Services.GetRequiredService<SpotifyClient>();
            var downloadService = _app.Services.GetRequiredService<DownloadService>();
            var stateManager = _app.Services.GetRequiredService<DownloadStateManager>();
            var database = _app.Services.GetRequiredService<DownloadDatabase>();

            if (string.IsNullOrEmpty(configManager.DownloadFolder) || configManager.PlaylistIds.Count == 0)
            {
                Console.WriteLine("[Auto-sync] Skipped — no playlists or download folder configured");
                return;
            }

            string? token = await spotifyClient.GetAccessTokenAsync();

            if (token == null)
            {
                Console.WriteLine("[Auto-sync] Failed to authenticate with Spotify");
                return;
            }

            List<SpotifyTrack> allTracks = [];

            foreach (string playlistId in configManager.PlaylistIds)
            {
                var tracks = await spotifyClient.GetPlaylistTracksAsync(token, playlistId);
                allTracks.AddRange(tracks);
            }

            allTracks = allTracks.DistinctBy(t => t.Id).ToList();

            var newTracks = allTracks.Where(t => !database.IsDownloaded(t.Id)).ToList();

            if (newTracks.Count == 0)
            {
                Console.WriteLine("[Auto-sync] All tracks already downloaded");
                return;
            }

            Console.WriteLine($"[Auto-sync] Found {newTracks.Count} new track(s) — starting download");

            try
            {
                await downloadService.DownloadTracksAsync(newTracks, configManager.DownloadFolder);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Auto-sync] Error: {ex.Message}");
                stateManager.FinishDownload();
            }
        }
    }
}
