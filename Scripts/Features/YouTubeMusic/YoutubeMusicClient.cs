using System.Diagnostics;
using System.Text.Json;
using SpotifyDownloader.Scripts.Core;

namespace SpotifyDownloader.Scripts.Features.YouTubeMusic
{
    public class YoutubeMusicClient
    {
        private readonly ToolPaths _toolPaths;

        public YoutubeMusicClient(ToolPaths toolPaths)
        {
            _toolPaths = toolPaths;
        }

        public async Task<List<YoutubeMusicTrack>> FetchPlaylistTracksAsync(string playlistId, string? cookiesBrowser)
        {
            string playlistUrl = $"https://music.youtube.com/playlist?list={playlistId}";

            ProcessStartInfo startInfo = new()
            {
                FileName = _toolPaths.YtDlp,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            startInfo.ArgumentList.Add("--flat-playlist");
            startInfo.ArgumentList.Add("--dump-json");
            startInfo.ArgumentList.Add("--no-warnings");

            if (!string.IsNullOrWhiteSpace(cookiesBrowser))
            {
                startInfo.ArgumentList.Add("--cookies-from-browser");
                startInfo.ArgumentList.Add(cookiesBrowser);
            }

            startInfo.ArgumentList.Add(playlistUrl);

            List<YoutubeMusicTrack> tracks = [];

            try
            {
                using Process process = new();
                process.StartInfo = startInfo;
                process.Start();

                string output = await process.StandardOutput.ReadToEndAsync();
                string errors = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode != 0 && string.IsNullOrWhiteSpace(output))
                {
                    Console.WriteLine($"[YTMusic] yt-dlp exited with code {process.ExitCode} for playlist '{playlistId}': {errors}");
                    return tracks;
                }

                string[] lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);

                foreach (string line in lines)
                {
                    string trimmed = line.Trim();

                    if (!trimmed.StartsWith('{'))
                    {
                        continue;
                    }

                    try
                    {
                        using JsonDocument doc = JsonDocument.Parse(trimmed);
                        JsonElement root = doc.RootElement;

                        string videoId = root.TryGetProperty("id", out JsonElement idEl)
                            ? idEl.GetString() ?? ""
                            : "";

                        string title = root.TryGetProperty("title", out JsonElement titleEl)
                            ? titleEl.GetString() ?? ""
                            : "";

                        string artist = "";
                        if (root.TryGetProperty("channel", out JsonElement channelEl) && channelEl.ValueKind == JsonValueKind.String)
                        {
                            artist = channelEl.GetString() ?? "";
                        }
                        else if (root.TryGetProperty("uploader", out JsonElement uploaderEl) && uploaderEl.ValueKind == JsonValueKind.String)
                        {
                            artist = uploaderEl.GetString() ?? "";
                        }

                        string? thumbnailUrl = null;
                        if (root.TryGetProperty("thumbnail", out JsonElement thumbnailEl) && thumbnailEl.ValueKind == JsonValueKind.String)
                        {
                            thumbnailUrl = thumbnailEl.GetString();
                            if (!string.IsNullOrEmpty(thumbnailUrl) && thumbnailUrl.Contains("hqdefault"))
                            {
                                thumbnailUrl = thumbnailUrl.Replace("hqdefault", "maxresdefault");
                            }
                        }

                        if (string.IsNullOrEmpty(thumbnailUrl) && !string.IsNullOrEmpty(videoId))
                        {
                            thumbnailUrl = $"https://i.ytimg.com/vi/{videoId}/maxresdefault.jpg";
                        }

                        if (!string.IsNullOrEmpty(videoId) && !string.IsNullOrEmpty(title))
                        {
                            tracks.Add(new YoutubeMusicTrack
                            {
                                VideoId = videoId,
                                Title = title,
                                Artist = artist,
                                ThumbnailUrl = thumbnailUrl
                            });
                        }
                    }
                    catch (JsonException ex)
                    {
                        Console.WriteLine($"[YTMusic] Could not parse entry JSON: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[YTMusic] Failed to fetch playlist '{playlistId}': {ex.Message}");
            }

            return tracks;
        }
    }
}
