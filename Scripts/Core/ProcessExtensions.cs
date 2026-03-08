using System.Diagnostics;

namespace SpotifyDownloader.Scripts.Core
{
    public static class ProcessExtensions
    {
        public static async Task<bool> WaitForExitAsync(this Process process, TimeSpan timeout)
        {
            using CancellationTokenSource cts = new(timeout);

            try
            {
                await process.WaitForExitAsync(cts.Token);
                return true;
            }
            catch (OperationCanceledException)
            {
                try
                {
                    process.Kill(true);
                }
                catch
                {
                }

                return false;
            }
        }
    }
}
