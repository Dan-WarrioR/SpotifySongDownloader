namespace SpotifyDownloader.Scripts.Features.Download
{
    public class DownloadState
    {
        public bool InProgress { get; set; }
        public string CurrentTrack { get; set; } = "";
        public int Progress { get; set; }
        public int Total { get; set; }
        public int Completed { get; set; }
        public int Failed { get; set; }
        public List<DownloadResult> Results { get; set; } = [];
    }

    public class DownloadResult
    {
        public string Track { get; set; } = "";
        public string Artist { get; set; } = "";
        public bool Success { get; set; }
        public string? Error { get; set; }
    }
    
    public class DownloadStateManager
    {
        private readonly Lock _lock = new();
        private DownloadState _state = new();

        public DownloadState GetState()
        {
            lock (_lock)
            {
                return new DownloadState
                {
                    InProgress = _state.InProgress,
                    CurrentTrack = _state.CurrentTrack,
                    Progress = _state.Progress,
                    Total = _state.Total,
                    Completed = _state.Completed,
                    Failed = _state.Failed,
                    Results = new List<DownloadResult>(_state.Results)
                };
            }
        }
    
        public void StartDownload(int totalTracks)
        {
            lock (_lock)
            {
                _state = new DownloadState
                {
                    InProgress = true,
                    Total = totalTracks,
                    Completed = 0,
                    Failed = 0,
                    Progress = 0,
                    CurrentTrack = "",
                    Results = []
                };
            }
        }
    
        public void UpdateCurrentTrack(string trackName)
        {
            lock (_lock)
            {
                _state.CurrentTrack = trackName;
            }
        }
    
        public void AddSuccess(string track, string artist)
        {
            lock (_lock)
            {
                _state.Completed++;
                _state.Results.Add(new DownloadResult
                {
                    Track = track,
                    Artist = artist,
                    Success = true
                });
                UpdateProgress();
            }
        }
    
        public void AddFailure(string track, string artist, string error)
        {
            lock (_lock)
            {
                _state.Failed++;
                _state.Results.Add(new DownloadResult
                {
                    Track = track,
                    Artist = artist,
                    Success = false,
                    Error = error
                });
                UpdateProgress();
            }
        }
    
        public void FinishDownload()
        {
            lock (_lock)
            {
                _state.InProgress = false;
                _state.CurrentTrack = "";
            }
        }
    
        private void UpdateProgress()
        {
            int processed = _state.Completed + _state.Failed;
            _state.Progress = _state.Total > 0 ? (int)((double)processed / _state.Total * 100) : 0;
        }
    }
}
