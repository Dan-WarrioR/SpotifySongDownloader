using System.Diagnostics;

namespace SpotifyDownloader.Scripts.Core
{
    public static class MediaEmbedder
    {
        private const int EmbedTimeoutSeconds = 30;

        /// <summary>
        /// Embeds metadata and/or album art into an MP3 file in-place.
        /// Pass null for any metadata field to leave it unchanged.
        /// Pass null for artUrl to skip art embedding.
        /// </summary>
        public static async Task<bool> EmbedMetadataAndArtAsync(
            string audioFile,
            string ffmpegPath,
            string workDir,
            string? title,
            string? artist,
            string? album,
            string? year,
            string? artUrl,
            bool normalizeVolume = false)
        {
            string safeName = Path.GetFileNameWithoutExtension(audioFile);
            string tempOutput = Path.Combine(workDir, $"{safeName}_temp.mp3");
            string? artworkPath = null;

            try
            {
                bool hasArt = false;

                if (!string.IsNullOrWhiteSpace(artUrl))
                {
                    artworkPath = Path.Combine(workDir, $"{safeName}_cover.jpg");

                    try
                    {
                        using HttpClient httpClient = new() { Timeout = TimeSpan.FromSeconds(30) };
                        byte[] imageData = await httpClient.GetByteArrayAsync(artUrl);
                        await File.WriteAllBytesAsync(artworkPath, imageData);
                        hasArt = true;
                        Console.WriteLine($"Album art downloaded: {artworkPath} ({imageData.Length} bytes)");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"⚠ Could not download artwork: {ex.Message}");
                        artworkPath = null;
                    }
                }

                ProcessStartInfo startInfo = new()
                {
                    FileName = ffmpegPath,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                startInfo.ArgumentList.Add("-i");
                startInfo.ArgumentList.Add(audioFile);

                if (hasArt && artworkPath != null)
                {
                    startInfo.ArgumentList.Add("-i");
                    startInfo.ArgumentList.Add(artworkPath);

                    string filterComplex = normalizeVolume
                        ? "[0:a]loudnorm=I=-16:LRA=11:TP=-1.5[aout];[1:v]crop=in_h:in_h[vout]"
                        : "[1:v]crop=in_h:in_h[vout]";

                    startInfo.ArgumentList.Add("-filter_complex");
                    startInfo.ArgumentList.Add(filterComplex);
                    startInfo.ArgumentList.Add("-map");
                    startInfo.ArgumentList.Add(normalizeVolume ? "[aout]" : "0:a");
                    startInfo.ArgumentList.Add("-map");
                    startInfo.ArgumentList.Add("[vout]");
                    startInfo.ArgumentList.Add("-c:a");
                    startInfo.ArgumentList.Add(normalizeVolume ? "libmp3lame" : "copy");

                    if (normalizeVolume)
                    {
                        startInfo.ArgumentList.Add("-q:a");
                        startInfo.ArgumentList.Add("0");
                    }

                    startInfo.ArgumentList.Add("-c:v");
                    startInfo.ArgumentList.Add("mjpeg");
                    startInfo.ArgumentList.Add("-disposition:v");
                    startInfo.ArgumentList.Add("attached_pic");
                    startInfo.ArgumentList.Add("-metadata:s:v");
                    startInfo.ArgumentList.Add("title=Album cover");
                    startInfo.ArgumentList.Add("-metadata:s:v");
                    startInfo.ArgumentList.Add("comment=Cover (front)");
                }
                else
                {
                    startInfo.ArgumentList.Add("-map");
                    startInfo.ArgumentList.Add("0:a");

                    if (normalizeVolume)
                    {
                        startInfo.ArgumentList.Add("-c:a");
                        startInfo.ArgumentList.Add("libmp3lame");
                        startInfo.ArgumentList.Add("-q:a");
                        startInfo.ArgumentList.Add("0");
                        startInfo.ArgumentList.Add("-af");
                        startInfo.ArgumentList.Add("loudnorm=I=-16:LRA=11:TP=-1.5");
                    }
                    else
                    {
                        startInfo.ArgumentList.Add("-c:a");
                        startInfo.ArgumentList.Add("copy");
                    }
                }

                if (!string.IsNullOrWhiteSpace(title))
                {
                    startInfo.ArgumentList.Add("-metadata");
                    startInfo.ArgumentList.Add($"title={title}");
                }

                if (!string.IsNullOrWhiteSpace(artist))
                {
                    startInfo.ArgumentList.Add("-metadata");
                    startInfo.ArgumentList.Add($"artist={artist}");
                }

                if (!string.IsNullOrWhiteSpace(album))
                {
                    startInfo.ArgumentList.Add("-metadata");
                    startInfo.ArgumentList.Add($"album={album}");
                }

                if (!string.IsNullOrWhiteSpace(year))
                {
                    startInfo.ArgumentList.Add("-metadata");
                    startInfo.ArgumentList.Add($"date={year}");
                }

                startInfo.ArgumentList.Add("-id3v2_version");
                startInfo.ArgumentList.Add("3");
                startInfo.ArgumentList.Add("-y");
                startInfo.ArgumentList.Add(tempOutput);

                string stdError = "";

                using Process process = new();
                process.StartInfo = startInfo;
                process.ErrorDataReceived += (_, e) =>
                {
                    if (e.Data != null)
                    {
                        stdError += e.Data + "\n";
                    }
                };
                process.Start();
                process.BeginErrorReadLine();

                bool completed = await process.WaitForExitAsync(TimeSpan.FromSeconds(EmbedTimeoutSeconds));

                if (!completed)
                {
                    Console.WriteLine($"✗ ffmpeg timeout for {safeName}");
                    return false;
                }

                if (process.ExitCode != 0)
                {
                    Console.WriteLine($"✗ ffmpeg failed (exit {process.ExitCode}): {stdError}");
                    return false;
                }

                if (!File.Exists(tempOutput) || new FileInfo(tempOutput).Length == 0)
                {
                    Console.WriteLine($"✗ ffmpeg output missing or empty for {safeName}");
                    TryDelete(tempOutput);
                    return false;
                }

                await Task.Delay(200);

                try
                {
                    File.Delete(audioFile);
                    File.Move(tempOutput, audioFile);
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"✗ Failed to replace file: {ex.Message}");

                    if (File.Exists(tempOutput) && !File.Exists(audioFile))
                    {
                        File.Move(tempOutput, audioFile);
                        return true;
                    }

                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ MediaEmbedder exception for {safeName}: {ex.Message}");
                return false;
            }
            finally
            {
                TryDelete(artworkPath);
                TryDelete(tempOutput);
            }
        }

        private static void TryDelete(string? path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return;
            }

            try
            {
                File.Delete(path);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠ Could not delete temp file {path}: {ex.Message}");
            }
        }
    }
}
