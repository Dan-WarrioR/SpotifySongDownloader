using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SpotifyDownloader.Scripts.Core
{
    public class YtDlpUpdateResult
    {
        public bool Updated { get; set; }
        public bool AlreadyUpToDate { get; set; }
        public string Message { get; set; } = "";
    }

    public class ToolPaths
    {
        public string YtDlp { get; }
        public string Ffmpeg { get; }

        public ToolPaths()
        {
            YtDlp = Resolve("yt-dlp");
            Ffmpeg = Resolve("ffmpeg");
        }

        public static bool CheckYtDlp() => IsAvailable("yt-dlp");
        public static bool CheckFfmpeg() => IsAvailable("ffmpeg");

        private static string Resolve(string name)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                string winExe = $"{name}.exe";
                if (File.Exists(winExe))
                {
                    return Path.GetFullPath(winExe);
                }
            }

            if (File.Exists(name))
            {
                return Path.GetFullPath(name);
            }

            return name;
        }

        private static bool IsAvailable(string name)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && File.Exists($"{name}.exe"))
            {
                return true;
            }

            if (File.Exists(name))
            {
                return true;
            }

            return RunVersionCheck(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? $"{name}.exe" : name)
                || RunVersionCheck(name);
        }

        public async Task<YtDlpUpdateResult> UpdateYtDlpAsync()
        {
            try
            {
                ProcessStartInfo startInfo = new()
                {
                    FileName = YtDlp,
                    Arguments = "-U",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using Process process = new();
                process.StartInfo = startInfo;
                process.Start();

                Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync();
                Task<string> stderrTask = process.StandardError.ReadToEndAsync();
                bool completed = await process.WaitForExitAsync(TimeSpan.FromSeconds(60));

                if (!completed)
                {
                    return new YtDlpUpdateResult { Message = "Update timed out" };
                }

                string output = (await stdoutTask + await stderrTask).Trim();

                if (output.Contains("Updated yt-dlp"))
                {
                    return new YtDlpUpdateResult { Updated = true, Message = output };
                }

                if (output.Contains("up to date") || output.Contains("uptodate"))
                {
                    return new YtDlpUpdateResult { AlreadyUpToDate = true, Message = output };
                }

                return new YtDlpUpdateResult { Message = output };
            }
            catch (Exception ex)
            {
                return new YtDlpUpdateResult { Message = $"Error: {ex.Message}" };
            }
        }

        private static bool RunVersionCheck(string executable)
        {
            try
            {
                using Process process = new();
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = executable,
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                process.Start();
                process.WaitForExit(5000);
                return process.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }
    }
}
