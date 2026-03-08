using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using SpotifyDownloader.Scripts.Features.Config;

namespace SpotifyDownloader.Scripts.Features.Spotify
{
    public class SpotifyTrack
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Artist { get; set; } = "";
        public string? AlbumArtUrl { get; set; }
    }

    public class SpotifyClient
    {
        private readonly ConfigManager _configManager;
        private readonly HttpClient _httpClient;

        public SpotifyClient(ConfigManager configManager)
        {
            _configManager = configManager;
            _httpClient = new HttpClient();
        }

        public async Task<string?> GetAccessTokenAsync()
        {
            string clientId = _configManager.ClientId;
            string clientSecret = _configManager.ClientSecret;

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                return null;
            }

            string credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));

            HttpRequestMessage request = new(HttpMethod.Post, "https://accounts.spotify.com/api/token");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            request.Content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials")
            });

            try
            {
                HttpResponseMessage response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                string jsonResponse = await response.Content.ReadAsStringAsync();
                using JsonDocument doc = JsonDocument.Parse(jsonResponse);
                return doc.RootElement.GetProperty("access_token").GetString();
            }
            catch
            {
                return null;
            }
        }

        public async Task<List<SpotifyTrack>> GetPlaylistTracksAsync(string accessToken, string playlistId)
        {
            if (string.IsNullOrEmpty(playlistId))
            {
                return new List<SpotifyTrack>();
            }

            List<SpotifyTrack> allTracks = new();
            string? nextUrl = $"https://api.spotify.com/v1/playlists/{playlistId}/tracks?limit=100";

            while (!string.IsNullOrEmpty(nextUrl))
            {
                HttpRequestMessage request = new(HttpMethod.Get, nextUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                try
                {
                    HttpResponseMessage response = await _httpClient.SendAsync(request);

                    if (!response.IsSuccessStatusCode)
                    {
                        break;
                    }

                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    using JsonDocument doc = JsonDocument.Parse(jsonResponse);

                    JsonElement items = doc.RootElement.GetProperty("items");

                    foreach (JsonElement item in items.EnumerateArray())
                    {
                        if (item.TryGetProperty("track", out JsonElement track) && track.ValueKind != JsonValueKind.Null)
                        {
                            string trackId = track.GetProperty("id").GetString() ?? "";
                            string trackName = track.GetProperty("name").GetString() ?? "";

                            JsonElement artists = track.GetProperty("artists");
                            string artistName = artists.EnumerateArray().FirstOrDefault().GetProperty("name").GetString() ?? "";

                            string? albumArtUrl = null;
                            if (track.TryGetProperty("album", out JsonElement album))
                            {
                                if (album.TryGetProperty("images", out JsonElement images))
                                {
                                    JsonElement firstImage = images.EnumerateArray().FirstOrDefault();
                                    if (firstImage.ValueKind != JsonValueKind.Undefined)
                                    {
                                        albumArtUrl = firstImage.GetProperty("url").GetString();
                                    }
                                }
                            }

                            allTracks.Add(new SpotifyTrack
                            {
                                Id = trackId,
                                Name = trackName,
                                Artist = artistName,
                                AlbumArtUrl = albumArtUrl
                            });
                        }
                    }

                    nextUrl = doc.RootElement.TryGetProperty("next", out JsonElement next) && next.ValueKind != JsonValueKind.Null
                        ? next.GetString()
                        : null;
                }
                catch
                {
                    break;
                }
            }

            return allTracks;
        }

        public async Task<string?> GetPlaylistNameAsync(string accessToken, string playlistId)
        {
            if (string.IsNullOrEmpty(playlistId))
            {
                return null;
            }

            HttpRequestMessage request = new(HttpMethod.Get,
                $"https://api.spotify.com/v1/playlists/{playlistId}?fields=name");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            try
            {
                HttpResponseMessage response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                string jsonResponse = await response.Content.ReadAsStringAsync();
                using JsonDocument doc = JsonDocument.Parse(jsonResponse);
                return doc.RootElement.GetProperty("name").GetString();
            }
            catch
            {
                return null;
            }
        }
    }
}
