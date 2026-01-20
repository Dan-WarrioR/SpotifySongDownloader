using System.Text.Json;

namespace SpotifyDownloader.Scripts.Core
{
    public static class GlobalConfig
    {
        public const string DatabaseFilename = "downloaded_tracks.json";
        public const string DataFolderPath = "Data";
        
        public const string ConfigFileName = "config.json";
        
        public static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
        };
    }
}