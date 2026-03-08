using System.Text.RegularExpressions;

namespace SpotifyDownloader.Scripts.Core
{
    public static class FileHelper
    {
        public static string SanitizeFilename(string filename)
        {
            string invalid = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            string pattern = $"[{Regex.Escape(invalid)}]";
            string sanitized = Regex.Replace(filename, pattern, "_");
            sanitized = Regex.Replace(sanitized, @"\s+", " ").Trim();
            return sanitized.Length > 200 ? sanitized[..200] : sanitized;
        }

        public static string GetUniqueFilepath(string basePath)
        {
            string path = $"{basePath}.mp3";
            int counter = 1;

            while (File.Exists(path))
            {
                path = $"{basePath}_{counter}.mp3";
                counter++;
            }

            return path;
        }
    }
}
