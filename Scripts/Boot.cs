using SpotifyDownloader.Scripts.Core;

namespace SpotifyDownloader.Scripts
{
    public class Boot
    {
        public static void Main(string[] args)
        {
            Application app = new();
            app.Run(args);
        }
    }
}
