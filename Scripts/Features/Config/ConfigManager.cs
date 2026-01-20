using System.Text.Json;
using SpotifyDownloader.Scripts.Core;

namespace SpotifyDownloader.Scripts.Features.Config
{
    public class ConfigData
    {
        public string ClientId { get; set; } = "";
        public string ClientSecret { get; set; } = "";
        public string PlaylistId { get; set; } = "";
        public string DownloadFolder { get; set; } = "";
    }
    
    public class ConfigManager
    {
        public ConfigData Config { get; private set; }

        public string ClientId => Config.ClientId;
        public string ClientSecret => Config.ClientSecret;
        public string PlaylistId => Config.PlaylistId;
        public string DownloadFolder => Config.DownloadFolder;
        
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
                    ClientId = "",
                    ClientSecret = "",
                    PlaylistId = "",
                    DownloadFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), "SpotifyDownloads")
                };
            }
        
            try
            {
                var json = File.ReadAllText(_configPath);
                return JsonSerializer.Deserialize<ConfigData>(json) ?? new ConfigData();
            }
            catch
            {
                return new ConfigData();
            }
        }
    
        public void Save(string clientId, string clientSecret, string playlistId, string downloadFolder)
        {
            Config = new ConfigData
            {
                ClientId = clientId,
                ClientSecret = clientSecret,
                PlaylistId = playlistId,
                DownloadFolder = downloadFolder
            };
        
            var json = JsonSerializer.Serialize(Config, GlobalConfig.SerializerOptions);
        
            File.WriteAllText(_configPath, json);
        }
    }
}
