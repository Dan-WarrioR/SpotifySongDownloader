namespace SpotifyDownloader.Scripts.Features.YouTube
{
    public class YoutubeDownloadState
    {
        public bool InProgress { get; set; }
        public string CurrentTitle { get; set; } = "";
        public bool? Success { get; set; }
        public string? FilePath { get; set; }
        public string? Error { get; set; }
    }

    public class YoutubeStateManager
    {
        private readonly Lock _lock = new();
        private YoutubeDownloadState _state = new();

        public YoutubeDownloadState GetState()
        {
            lock (_lock)
            {
                return new YoutubeDownloadState
                {
                    InProgress = _state.InProgress,
                    CurrentTitle = _state.CurrentTitle,
                    Success = _state.Success,
                    FilePath = _state.FilePath,
                    Error = _state.Error
                };
            }
        }

        public void Start(string title)
        {
            lock (_lock)
            {
                _state = new YoutubeDownloadState
                {
                    InProgress = true,
                    CurrentTitle = title,
                    Success = null,
                    FilePath = null,
                    Error = null
                };
            }
        }

        public void Complete(string filePath)
        {
            lock (_lock)
            {
                _state.InProgress = false;
                _state.Success = true;
                _state.FilePath = filePath;
                _state.Error = null;
            }
        }

        public void Fail(string error)
        {
            lock (_lock)
            {
                _state.InProgress = false;
                _state.Success = false;
                _state.Error = error;
            }
        }
    }
}
