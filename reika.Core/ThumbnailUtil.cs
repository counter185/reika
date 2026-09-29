using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace reika.Core
{
    public class ThumbnailUtil
    {
        static List<string> ffmpegCreatedThumbnails = new List<string>();

        public static Uri FFMPEGExtractThumbnail(string filename, string timestamp = "00:00:01.000")
        {
            //todo: specific stream selection
            Random r = new Random();
            string tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"thumbnail_{r.Next(1000000)}.jpg");
            string[] args = new string[]
            {
                "-y",
                "-ss", timestamp,
                "-i", $"\"{filename}\"",
                "-frames:v", "1",
                $"\"{tempFile}\""
            };
            FFMPEG.RunCommandAndGetOutput("ffmpeg", args);
            Uri uri = new Uri(tempFile);
            ffmpegCreatedThumbnails.Add(uri.LocalPath);
            return uri;
        }

        public static void FFMPEGExtractThumbnailAsync(string filename, string timestamp, Action<Uri> callback)
        {
            Task.Run(() =>
            {
                Random r = new Random();
                string tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"thumbnail_{r.Next(1000000)}.jpg");
                string[] args = new string[]
                {
                    "-y",
                    "-ss", timestamp,
                    "-i", $"\"{filename}\"",
                    "-frames:v", "1",
                    $"\"{tempFile}\""
                };
                var output = FFMPEG.RunCommandAndGetOutput("ffmpeg", args);
                Uri uri = new Uri(tempFile);
                ffmpegCreatedThumbnails.Add(uri.LocalPath);
                if (!File.Exists(tempFile))
                {
                    Console.WriteLine($"Output: {string.Join("\n", output)}");
                }
                if (callback != null)
                {
                    callback(uri);
                }
            });
        }

        public static void FFMPEGCleanupThumbnails()
        {
            foreach (string thumbnail in ffmpegCreatedThumbnails)
            {
                try
                {
                    File.Delete(thumbnail);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deleting thumbnail {thumbnail}: {ex.Message}");
                }
            }
            ffmpegCreatedThumbnails.Clear();
        }
        public static void FFMPEGManualDeleteThumbnail(string thumbnailPath)
        {
            if (ffmpegCreatedThumbnails.Contains(thumbnailPath))
            {
                try
                {
                    File.Delete(thumbnailPath);
                    ffmpegCreatedThumbnails.Remove(thumbnailPath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deleting thumbnail {thumbnailPath}: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine($"{thumbnailPath} was not tracked for deletion.");
            }
        }
    }
}
