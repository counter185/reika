using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows.Media.Imaging;
using System.IO;
using System.Threading;
using System.Net;
using System.IO.Compression;
using ReencGUI.UI;
using System.Windows.Threading;
using Microsoft.Win32;

namespace ReencGUI
{
    public class FFMPEG
    {
        public enum CodecType
        {
            Invalid,
            Video,
            Audio,
            Subtitle
        }
        public struct CodecInfo
        {
            public string ID;
            public string Name;
            public CodecType Type;
        }

        public class MediaInfo
        {
            public string fileName;
            public string date;
            public string mediaEncoder;
            public int dH, dM, dS, dMS;
            public string overallBitrate;
            public List<StreamInfo> streams = new List<StreamInfo>();

            public ulong Duration { get => Utils.LengthToMS(dH, dM, dS, dMS); }
        }

        public class StreamInfo
        {
            public CodecType mediaType;
            public string encoderID;
            public string encoderName;
            public string bitrate;
            public string resolution;   //hz for audio, width x height for video
            public List<string> fullRawData = new List<string>();
            public List<string> otherData = new List<string>();
        }

        public static bool MachineShouldUseEssentialBuild()
        {
            try
            {
                //check for windows 7 or 8
                var currentVersionReg = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "CurrentVersion", "6.1")?.ToString();
                var match = Regex.Match(currentVersionReg, @"(\d+)\.(\d+)");
                if (match.Success)
                {
                    int major = int.Parse(match.Groups[1].Value);
                    int minor = int.Parse(match.Groups[2].Value);
                    
                    return major < 6 || (major == 6 && minor < 3);
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error checking Windows version: " + ex.Message);
                return false;
            }
        }

        public static bool DownloadLatest(UIFFMPEGOperationEntry progressCallback)
        {
            progressCallback.Dispatcher.Invoke(() =>
            {
                progressCallback.Label_Primary.Text = "Finding latest FFMPEG release";
                progressCallback.Label_Secondary.Content = "";
            });
            string releasesURL = "https://api.github.com/repos/GyanD/codexffmpeg/releases";
            WebClient client = new WebClient();
            client.Headers.Add("User-Agent", "ReencGUI/1.0");
            client.Headers.Add("Accept", "application/json");
            try
            {
                //json parsers are for the weak
                string jsons = client.DownloadString(releasesURL);
                string nextUrl = Regex.Match(jsons, @"""url"":\s*""(https://api\.github\.com/repos/GyanD/codexffmpeg/releases/[0-9]+)""").Groups[1].Value;

                client.Headers.Add("User-Agent", "ReencGUI/1.0");
                string jsonss = client.DownloadString(nextUrl);

                Match downloadMatches = Regex.Match(jsonss,
                    @"""browser_download_url"":\s*""([^""]+)""");
                while (downloadMatches.Success)
                {
                    string urlNow = downloadMatches.Groups[1].Value;
                    if (urlNow.Contains("ffmpeg") && urlNow.Contains(MachineShouldUseEssentialBuild() ? "essential" : "full") 
                        && urlNow.Contains("build") && urlNow.Contains(".zip")
                        && !urlNow.Contains("shared"))
                    {
                        progressCallback.Dispatcher.Invoke(() =>
                        {
                            progressCallback.Label_Primary.Text = "Downloading FFMPEG";
                            progressCallback.Label_Secondary.Content = "";
                        });

                        Console.WriteLine("Downloading FFMPEG release from: " + urlNow);
                        client.Headers.Add("User-Agent", "ReencGUI/1.0");

                        bool downloadDone = false;
                        client.DownloadProgressChanged += (sender, e) =>
                        {
                            progressCallback.Dispatcher.Invoke(() =>
                            {
                                progressCallback.Label_Secondary.Content = $"{(double)e.BytesReceived/Utils.Megabytes(1):.02}MB / {(double)e.TotalBytesToReceive/Utils.Megabytes(1):.02}MB";
                                progressCallback.ProgressBar_Operation.Value = e.ProgressPercentage;
                            });
                        };
                        client.DownloadFileCompleted += (sender, e) =>
                        {
                            downloadDone = true;
                        };
                        client.DownloadFileAsync(new Uri(urlNow), "ffmpeg.zip");

                        while (!downloadDone)
                        {
                            Thread.Sleep(100);
                        }

                        progressCallback.Dispatcher.Invoke(() =>
                        {
                            progressCallback.Label_Primary.Text = "Extracting FFMPEG";
                            progressCallback.Label_Secondary.Content = "";
                        });


                        Console.WriteLine("Extracting FFMPEG release...");
                        ZipArchive zip = ZipFile.OpenRead("ffmpeg.zip");
                        Directory.CreateDirectory("ffmpeg");
                        var extractTargets = zip.Entries.Where(x => x.Name.EndsWith(".exe"));
                        int done = 0;
                        foreach (ZipArchiveEntry entry in extractTargets)
                        {
                            progressCallback.Dispatcher.Invoke(() =>
                            {
                                progressCallback.Label_Secondary.Content = entry.Name;
                                progressCallback.Label_Secondary2.Content = $"{done} / {extractTargets.Count()} files";
                                progressCallback.ProgressBar_Operation.Value = (double)done / extractTargets.Count() * 100;
                            });
                            entry.ExtractToFile(Path.Combine("ffmpeg", entry.Name));
                            done++;
                        }
                        zip.Dispose();
                        File.Delete("ffmpeg.zip");
                        return true;
                    }
                    downloadMatches = downloadMatches.NextMatch();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error downloading FFMPEG releases: " + ex.Message);
            }
            return false;
        }

        public static string GetCommandPath(string command)
        {
            if (File.Exists($"ffmpeg\\{command}.exe"))
            {
                return $"ffmpeg\\{command}.exe";
            } 
            else
            {
                return command;
            }
        }

        public static List<string> RunCommandAndGetOutput(string command, IEnumerable<string> args)
        {
            if (File.Exists($"ffmpeg\\{command}.exe"))
            {
                command = $"ffmpeg\\{command}.exe";
            }
            List<string> output = new List<string>();
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = string.Join(" ", args),
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    StandardErrorEncoding = Encoding.UTF8,
                    StandardOutputEncoding = Encoding.UTF8,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using (Process process = Process.Start(startInfo))
                {
                    if (process != null)
                    {
                        process.EnableRaisingEvents = true;
                        process.BeginOutputReadLine();
                        process.BeginErrorReadLine();
                        process.OutputDataReceived += (sender, e) =>
                        {
                            if (e.Data != null)
                            {
                                output.Add(e.Data);
                            }
                        };
                        process.ErrorDataReceived += (sender, e) =>
                        {
                            if (e.Data != null)
                            {
                                output.Add(e.Data);
                            }
                        };
                        bool exited = false;
                        process.Exited += (sender, e) =>
                        {
                            exited = true;
                        };
                        while (!exited)
                        {
                            Thread.Sleep(100);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error running FFMPEG command: " + ex.Message, ex);
            }
            return output;
        }
        public static Process RunCommandWithAsyncOutput(string command, IEnumerable<string> args, 
            Action<string> outputLineCallback,
            Action<int> exitCallback = null)
        {
            if (File.Exists($"ffmpeg\\{command}.exe"))
            {
                command = $"ffmpeg\\{command}.exe";
            }

            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = string.Join(" ", args),
                    RedirectStandardOutput = true,
                    StandardErrorEncoding = Encoding.UTF8,
                    StandardOutputEncoding = Encoding.UTF8,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                Process process = Process.Start(startInfo);
                process.EnableRaisingEvents = true;
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.OutputDataReceived += (a,b) => outputLineCallback(b.Data);
                process.ErrorDataReceived += (a,b) => outputLineCallback(b.Data);
                process.Exited += (a, b) =>
                {
                    if (exitCallback != null)
                    {
                        exitCallback(process.ExitCode);
                    }
                };
                return process;
            }
            catch (Exception ex)
            {
                throw new Exception("Error running FFMPEG command: " + ex.Message, ex);
            }
        }

        public static List<string> RunFFMPEGCommandlineForOutput(IEnumerable<string> args)
        {
            args = args.Append("-hide_banner").ToList();
            return RunCommandAndGetOutput("ffmpeg", args);
        }
        public static List<string> RunFFProbeCommandlineForOutput(IEnumerable<string> args)
        {
            args = args.Append("-hide_banner").ToList();
            return RunCommandAndGetOutput("ffprobe", args);
        }

        public static List<CodecInfo> ParseFFMPEGCodecList(IEnumerable<string> outputLines)
        {
            List<CodecInfo> ret = new List<CodecInfo>();
            string codecMatch = @"\s?([A-Z\.]+)\s+([^\s=]+)\s+(.+)";
            foreach (string line in outputLines)
            {
                if (line != null)
                {
                    Match match = Regex.Match(line, codecMatch);
                    if (match.Success)
                    {
                        string codecInfoString = match.Groups[1].Value;
                        ret.Add(new CodecInfo
                        {
                            ID = match.Groups[2].Value,
                            Name = match.Groups[3].Value,
                            Type = codecInfoString.Contains('V') ? CodecType.Video
                                   : codecInfoString.Contains('A') ? CodecType.Audio
                                   : codecInfoString.Contains('S') ? CodecType.Subtitle
                                   : CodecType.Invalid
                        });
                    }
                }
            }

            return ret;
        }

        public static MediaInfo ParseFFProbeMediaInfo(IEnumerable<string> outputLines)
        {
            MediaInfo ret = null;
            StreamInfo currentStream = null;
            bool readingMeta = false;
            string inputMatch = @"Input #0,";
            string metaMatch = @"\s*([^\s]+)\s*:\s+(.+)";
            string durationMatch = @"\s*Duration:\s+(\d+):(\d+):(\d+)\.(\d+),";
            string streamMatch = @"\s*Stream #0:([0-9]+)[^:]*:\s*(Video|Audio):\s*(.+)";

            foreach (string line in outputLines)
            {
                if (ret == null)
                {
                    if (line.StartsWith(inputMatch))
                    {
                        ret = new MediaInfo();
                        ret.fileName = Regex.Match(line, @".+from '(.+)':").Groups[1].Value;
                    }
                } else
                {
                    var matchStream = Regex.Match(line, streamMatch);
                    var matchMeta = Regex.Match(line, @"\s*Metadata:");
                    var matchDuration = Regex.Match(line, durationMatch);
                    var matchMetaData = Regex.Match(line, metaMatch);
                    if (matchMeta.Success)
                    {
                        readingMeta = true;
                    }
                    else if (matchDuration.Success)
                    {
                        //parse duration
                        int h = int.Parse(matchDuration.Groups[1].Value);
                        int m = int.Parse(matchDuration.Groups[2].Value);
                        int s = int.Parse(matchDuration.Groups[3].Value);
                        int ms = int.Parse(matchDuration.Groups[4].Value);
                        ret.dH = h;
                        ret.dM = m;
                        ret.dS = s;
                        ret.dMS = ms;
                        readingMeta = false;
                    }
                    else if (matchStream.Success)
                    {
                        //parse stream
                        if (currentStream != null)
                        {
                            ret.streams.Add(currentStream);
                        }
                        currentStream = new StreamInfo();
                        readingMeta = false;
                        string mediaDataStr = matchStream.Groups[3].Value;
                        string mediaTypeStr = matchStream.Groups[2].Value;
                        var dSplit = Regex.Split(mediaDataStr, @",(?![^()]*\))").Select(x=>x.Trim());

                        currentStream.mediaType = mediaTypeStr == "Video" ? CodecType.Video
                                            : mediaTypeStr == "Audio" ? CodecType.Audio
                                            : mediaTypeStr == "Subtitle" ? CodecType.Subtitle
                                            : CodecType.Invalid;
                        currentStream.encoderID = dSplit.First().Split(' ')[0];
                        currentStream.resolution =
                            currentStream.mediaType == CodecType.Audio ?
                                (from x in dSplit
                                 where x.EndsWith(" Hz")
                                 select x).FirstOrDefault()
                            : currentStream.mediaType == CodecType.Video ?
                                (from x in dSplit
                                 where Regex.IsMatch(x, @"^\d+x\d+")
                                 select x).FirstOrDefault()
                            : "";
                        currentStream.bitrate =
                            (from x in dSplit
                             where x.Contains("b/s")
                             select x).FirstOrDefault();
                        currentStream.fullRawData = dSplit.ToList();
                    } 
                    else if (readingMeta && matchMetaData.Success)
                    {
                        string key = matchMetaData.Groups[1].Value;
                        string value = matchMetaData.Groups[2].Value;
                        if (key == "encoder")
                        {
                            if (currentStream == null)
                            {
                                ret.mediaEncoder = value;
                            } else
                            {
                                currentStream.encoderName = value;
                            }
                        } else
                        {
                            if (currentStream != null)
                            {
                                currentStream.otherData.Add($"{key}: {value}");
                            }
                        }
                    }
                }
            }

            if (currentStream != null)
            {
                ret.streams.Add(currentStream);
            }
            return ret;
        }

        public static List<CodecInfo> GetAvailableDecoders()
        {
            return ParseFFMPEGCodecList(RunFFMPEGCommandlineForOutput(new string[] { "-decoders" }));
        }
        public static List<CodecInfo> GetAvailableEncoders()
        {
            return ParseFFMPEGCodecList(RunFFMPEGCommandlineForOutput(new string[] { "-encoders" }));
        }
        public static MediaInfo GetMediaInfoForFile(string fileName)
        {
            return ParseFFProbeMediaInfo(RunFFProbeCommandlineForOutput(new string[] { $"\"{fileName}\"" }));
        }

        public static string GetFFMPEGVersion()
        {
            var output = RunFFMPEGCommandlineForOutput(new string[] { "-version" });
            if (output.Count > 0)
            {
                var matchingLines = output.Where(x => x.Contains("ffmpeg version "));
                if (matchingLines.Any()) {
                    string versionLine = matchingLines.First();
                    Match versionMatch = Regex.Match(versionLine, @"version ([^\s]+)");
                    if (versionMatch.Success)
                    {
                        return "ffmpeg version " + versionMatch.Groups[1].Value;
                    } else
                    {
                        return versionLine;
                    }
                } else
                {
                    return "";
                }
            } else
            {
                return "";
            }
        }

        static List<string> createdThumbnails = new List<string>();

        public static BitmapImage ExtractThumbnail(string filename, string timestamp = "00:00:01.000")
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
            RunCommandAndGetOutput("ffmpeg", args);
            Uri uri = new Uri(tempFile);
            createdThumbnails.Add(uri.LocalPath);
            return new BitmapImage(uri);
        }

        public static void ExtractThumbnailAsync(string filename, string timestamp, Action<Uri> callback)
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
                var output = RunCommandAndGetOutput("ffmpeg", args);
                Uri uri = new Uri(tempFile);
                createdThumbnails.Add(uri.LocalPath);
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

        public static void CleanupThumbnails()
        {
            foreach (string thumbnail in createdThumbnails)
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
            createdThumbnails.Clear();
        }
        public static void ManualDeleteThumbnail(string thumbnailPath)
        {
            if (createdThumbnails.Contains(thumbnailPath))
            {
                try
                {
                    File.Delete(thumbnailPath);
                    createdThumbnails.Remove(thumbnailPath);
                } catch (Exception ex)
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
