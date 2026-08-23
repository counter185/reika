using Microsoft.Win32;
using reika.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace ReencGUI
{
    public static class WindowsUtils
    {
        public static BitmapImage LoadToMemFromUri(Uri uri)
        {
            if (uri != null)
            {
                try
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = uri;
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    return bitmap;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading image from URI: {uri}. Exception: {ex.Message}");
                }
            }
            return null;
        }

        static List<string> ffmpegCreatedThumbnails = new List<string>();

        public static BitmapImage FFMPEGExtractThumbnail(string filename, string timestamp = "00:00:01.000")
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
            return new BitmapImage(uri);
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

        public static string GetSystemHardwareInfo()
        {
            //cpu name: HARDWARE\DESCRIPTION\System\CentralProcessor\0 key ProcessorNameString
            //video drivers: SYSTEM\CurrentControlSet\Control\Video\{guid}\0000
            //    key DriverDesc for gpu name
            //    key DriverVersion for gpu driver version

            try
            {
                string cpuName = "";
                using (var cpuKey = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor", false))
                {
                    if (cpuKey != null)
                    {
                        using (RegistryKey cpuKey0 = cpuKey.OpenSubKey("0", false))
                        {
                            cpuName = cpuKey0.GetValue("ProcessorNameString")?.ToString() ?? "";
                        }
                        cpuName += $"(x {cpuKey.SubKeyCount})";
                    }
                }


                List<string> gpuNames = new List<string>();

                using (var videoKeys = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Video", false))
                {
                    string[] subKeys = videoKeys.GetSubKeyNames();
                    foreach (string gpuKeyStr in subKeys)
                    {
                        using (var gpuKey = videoKeys.OpenSubKey($"{gpuKeyStr}\\0000", false))
                        {
                            if (gpuKey != null)
                            {
                                string infSection = gpuKey.GetValue("InfSection")?.ToString() ?? "";
                                //ignore meta virtual monitor whatever
                                if (!infSection.StartsWith("VirtualScreen"))
                                {
                                    string gpuName = gpuKey.GetValue("DriverDesc")?.ToString() ?? "";
                                    string gpuVersion = gpuKey.GetValue("DriverVersion")?.ToString() ?? "";
                                    if (gpuName != "")
                                    {
                                        gpuNames.Add($"{gpuName} (driver {gpuVersion})");
                                    }
                                }
                            }
                        }
                    }
                }

                return string.Join("\n", (new string[] { cpuName }).Concat(gpuNames));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving system hardware info: {ex.Message}");
                return "<error getting hardware info>";
            }
        }
    }
}
