using System.Text.Json;
using System.Text.Json.Serialization;
using SpotifyDownloader.Scripts.Core;

namespace SpotifyDownloader.Scripts.Features.Config
{
    public class ConfigData
    {
        [JsonPropertyName("client_id")]
        public string ClientId { get; set; } = "";

        [JsonPropertyName("client_secret")]
        public string ClientSecret { get; set; } = "";

        [JsonPropertyName("playlist_ids")]
        public List<string> PlaylistIds { get; set; } = new();

        [JsonPropertyName("download_folder")]
        public string DownloadFolder { get; set; } = "";

        [JsonPropertyName("file_name_pattern")]
        public string FileNamePattern { get; set; } = "track";

        [JsonPropertyName("normalize_volume")]
        public bool NormalizeVolume { get; set; } = false;

        [JsonPropertyName("sponsorblock")]
        public bool SponsorBlock { get; set; } = false;

        [JsonPropertyName("auto_sync")]
        public bool AutoSync { get; set; } = false;

        [JsonPropertyName("ytm_playlist_ids")]
        public List<string> YtmPlaylistIds { get; set; } = new();

        [JsonPropertyName("ytm_cookies_browser")]
        public string YtmCookiesBrowser { get; set; } = "";
    }

    public class ConfigManager
    {
        public ConfigData Config { get; private set; }

        public string ClientId => Config.ClientId;
        public string ClientSecret => Config.ClientSecret;
        public List<string> PlaylistIds => Config.PlaylistIds;
        public string DownloadFolder => Config.DownloadFolder;
        public bool NormalizeVolume => Config.NormalizeVolume;
        public bool SponsorBlock => Config.SponsorBlock;
        public bool AutoSync => Config.AutoSync;
        public List<string> YtmPlaylistIds => Config.YtmPlaylistIds;
        public string YtmCookiesBrowser => Config.YtmCookiesBrowser;

        private readonly string _configPath;

        public ConfigManager()
        {
            if (!Directory.Exists(GlobalConfig.DataFolderPath))
            {
                Directory.CreateDirectory(GlobalConfig.DataFolderPath);
            }

            _configPath = Path.Combine(GlobalConfig.DataFolderPath, GlobalConfig.ConfigFileName);
            Config = LoadConfig();
        }

        private ConfigData LoadConfig()
        {
            if (!File.Exists(_configPath))
            {
                return new ConfigData
                {
                    DownloadFolder = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
                        "SpotifyDownloads")
                };
            }

            try
            {
                string json = File.ReadAllText(_configPath);
                return JsonSerializer.Deserialize<ConfigData>(json, GlobalConfig.SerializerOptions) ?? new ConfigData();
            }
            catch
            {
                return new ConfigData();
            }
        }

        public void Save(ConfigData newConfig)
        {
            Config = newConfig;
            string json = JsonSerializer.Serialize(Config, GlobalConfig.SerializerOptions);
            File.WriteAllText(_configPath, json);
        }
    }
}
