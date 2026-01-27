using System.Text.Json;
using SpotifyDownloader.Scripts.Core;

namespace SpotifyDownloader.Scripts.Data
{
    public class TrackRecord
    {
        public string TrackId { get; set; } = "";
        public string Name { get; set; } = "";
        public string Artist { get; set; } = "";
        public string FilePath { get; set; } = "";
        public DateTime DownloadedAt { get; set; }
    }
    
    public class DownloadDatabase
    {
        private readonly string _databasePath;
        private List<TrackRecord> _tracks;
        private readonly Lock _lock = new();
    
        public DownloadDatabase()
        {
            if (!Directory.Exists(GlobalConfig.DataFolderPath))
            {
                Directory.CreateDirectory(GlobalConfig.DataFolderPath);
            }
        
            _databasePath = Path.Combine(GlobalConfig.DataFolderPath, GlobalConfig.DatabaseFilename);
            _tracks = LoadDatabase();
        }
    
        private List<TrackRecord> LoadDatabase()
        {
            if (!File.Exists(_databasePath))
            {
                return [];
            }
        
            try
            {
                var json = File.ReadAllText(_databasePath);
                return JsonSerializer.Deserialize<List<TrackRecord>>(json) ?? [];
            }
            catch
            {
                return [];
            }
        }
    
        private void SaveDatabase()
        {
            lock (_lock)
            {
                var json = JsonSerializer.Serialize(_tracks, GlobalConfig.SerializerOptions);
                File.WriteAllText(_databasePath, json);
            }
        }
    
        public bool IsDownloaded(string trackId)
        {
            lock (_lock)
            {
                return _tracks.Any(t => t.TrackId == trackId);
            }
        }
    
        public void Add(string trackId, string trackName, string artist, string filePath)
        {
            lock (_lock)
            {
                if (IsDownloaded(trackId))
                {
                    return;
                }

                _tracks.Add(new TrackRecord
                {
                    TrackId = trackId,
                    Name = trackName,
                    Artist = artist,
                    FilePath = filePath,
                    DownloadedAt = DateTime.Now
                });
                
                SaveDatabase();
            }
        }
    
        public void ClearAll()
        {
            lock (_lock)
            {
                _tracks.Clear();
                SaveDatabase();
                Console.WriteLine("✓ Download history cleared");
            }
        }
    
        public List<TrackRecord> GetRecentTracks(int count = 10)
        {
            lock (_lock)
            {
                return _tracks
                    .OrderByDescending(t => t.DownloadedAt)
                    .Take(count)
                    .ToList();
            }
        }
    
        public int TotalDownloaded()
        {
            lock (_lock)
            {
                return _tracks.Count;
            }
        }
    }
}