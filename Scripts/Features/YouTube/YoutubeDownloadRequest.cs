using System.Text.Json.Serialization;

namespace SpotifyDownloader.Scripts.Features.YouTube
{
    public class YoutubeDownloadRequest
    {
        [JsonPropertyName("url")]
        public string Url { get; set; } = "";

        [JsonPropertyName("title")]
        public string Title { get; set; } = "";

        [JsonPropertyName("artist")]
        public string Artist { get; set; } = "";

        [JsonPropertyName("album")]
        public string Album { get; set; } = "";

        [JsonPropertyName("year")]
        public string Year { get; set; } = "";

        [JsonPropertyName("cover_art_url")]
        public string CoverArtUrl { get; set; } = "";

        [JsonPropertyName("download_folder")]
        public string? DownloadFolder { get; set; }
    }
}
