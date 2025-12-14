using ReencGUI.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Xml.Linq;
using System.Xml.XPath;

namespace ReencGUI
{
    public class YTDLP
    {
        public class YTDLPFormat
        {
            public string formatID;
            public string formatDisplayName;
            public string ext;
            public long fileSize;

            public string vcodec;
            public int? fps;
            public int? vbr;
            public int? width;
            public int? height;

            public string acodec;
            public int? asr;
            public int? abr;


            public override string ToString()
            {
                string fsStr = fileSize >= 0 ? Utils.KiBStringToFriendlySizeString((fileSize/1024)+"KiB") : "";
                return $"{formatID}: {formatDisplayName} {fsStr}";
            }
        }

        public class YTDLPVideo
        {
            public string id;
            public string uploader;
            public string title;
            public List<YTDLPFormat> formats;
        }

        public static YTDLPVideo GetVideoInfo(string url)
        {
            if (url == "")
            {
                return null;
            }

            try
            {
                List<string> output = FFMPEG.RunCommandAndGetOutput(GetCommandPath("yt-dlp"), 
                    new List<string> {
                        "-j",
                        "--list-formats",
                        url
                    }
                );

                string json = output.Where(x => x.StartsWith("{\"id\"")).FirstOrDefault();
                if (json != null)
                {
                    //anything to not add newtonsoft.json
                    var jsonReader = JsonReaderWriterFactory.CreateJsonReader(Encoding.UTF8.GetBytes(json), new System.Xml.XmlDictionaryReaderQuotas());

                    var root = XElement.Load(jsonReader);
                    YTDLPVideo video = new YTDLPVideo();
                    video.id = root.XPathSelectElement("//id")?.Value;
                    video.title = root.XPathSelectElement("//title")?.Value;
                    video.uploader = root.XPathSelectElement("//uploader")?.Value;
                    video.formats = new List<YTDLPFormat>();
                    var descNodes = root.XPathSelectElement("//formats").Elements().ToList();
                    foreach (var format in descNodes)
                    {
                        video.formats.Add(new YTDLPFormat { 
                            formatID = format.XPathSelectElement("format_id")?.Value,
                            formatDisplayName = format.XPathSelectElement("format")?.Value,
                            ext = format.XPathSelectElement("ext")?.Value,
                            fileSize = long.TryParse(format.XPathSelectElement("filesize")?.Value, out long fs) ? (long)fs : -1,

                            vcodec = format.XPathSelectElement("vcodec")?.Value,
                            vbr = int.TryParse(format.XPathSelectElement("vbr")?.Value, out int vbrv) ? (int?)vbrv : null,
                            fps = int.TryParse(format.XPathSelectElement("fps")?.Value, out int fpsv) ? (int?)fpsv : null,
                            width = int.TryParse(format.XPathSelectElement("width")?.Value, out int wv) ? (int?)wv : null,
                            height = int.TryParse(format.XPathSelectElement("height")?.Value, out int hv) ? (int?)hv : null,

                            acodec = format.XPathSelectElement("acodec")?.Value,
                            asr = int.TryParse(format.XPathSelectElement("asr")?.Value, out int asrv) ? (int?)asrv : null,
                            abr = int.TryParse(format.XPathSelectElement("abr")?.Value, out int abrv) ? (int?)abrv : null,

                        });
                    }

                    Console.WriteLine($"YT-DLP Video Info: ID={video.id}, Title={video.title}");
                    Console.WriteLine($"Formats:");
                    foreach (var fmt in video.formats)
                    {
                        Console.WriteLine($" - {fmt}");
                    }

                    return video;
                }
            } catch (Exception ex)
            {
                return null;
            }
            return null;
        }

        public static bool RunDownload(List<string> args, UIFFMPEGOperationEntry progress)
        {
            bool finished = false;
            int exitCode = -1;
            FFMPEG.RunCommandWithAsyncOutput(GetCommandPath("yt-dlp"), args, 
                (s) => {
                    progress.Dispatcher.Invoke(() => { progress.Label_Secondary.Content = s; });
                },
                (ec) => { finished = true; exitCode = ec; });

            while (!finished)
            {
                Thread.Sleep(100);
            }
            return exitCode == 0;
        }

        public static bool DownloadLatest(UIFFMPEGOperationEntry progressCallback)
        {
            progressCallback.Dispatcher.Invoke(() =>
            {
                progressCallback.Label_Primary.Text = "Finding latest yt-dlp release";
                progressCallback.Label_Secondary.Content = "";
            });
            string releasesURL = "https://api.github.com/repos/yt-dlp/yt-dlp/releases";
            WebClient client = new WebClient();
            client.Headers.Add("User-Agent", "ReencGUI/1.0");
            client.Headers.Add("Accept", "application/json");
            try
            {
                string jsons = client.DownloadString(releasesURL);
                string nextUrl = Regex.Match(jsons, @"""url"":\s*""(https://api\.github\.com/repos/yt-dlp/yt-dlp/releases/[0-9]+)""").Groups[1].Value;

                client.Headers.Add("User-Agent", "ReencGUI/1.0");
                string jsonss = client.DownloadString(nextUrl);

                Match downloadMatches = Regex.Match(jsonss,
                    @"""browser_download_url"":\s*""([^""]+)""");
                while (downloadMatches.Success)
                {
                    string urlNow = downloadMatches.Groups[1].Value;
                    if (urlNow.Contains("yt-dlp") && urlNow.Contains(".exe")
                        && !urlNow.Contains("_arm64") && !urlNow.Contains("_x86"))
                    {
                        progressCallback.Dispatcher.Invoke(() =>
                        {
                            progressCallback.Label_Primary.Text = "Downloading yt-dlp";
                            progressCallback.Label_Secondary.Content = "";
                        });

                        Console.WriteLine("Downloading yt-dlp release from: " + urlNow);
                        client.Headers.Add("User-Agent", "ReencGUI/1.0");

                        bool downloadDone = false;
                        client.DownloadProgressChanged += (sender, e) =>
                        {
                            progressCallback.Dispatcher.Invoke(() =>
                            {
                                progressCallback.Label_Secondary.Content = $"{(double)e.BytesReceived / Utils.Megabytes(1):.02}MB / {(double)e.TotalBytesToReceive / Utils.Megabytes(1):.02}MB";
                                progressCallback.ProgressBar_Operation.Value = e.ProgressPercentage;
                            });
                        };
                        client.DownloadFileCompleted += (sender, e) =>
                        {
                            downloadDone = true;
                        };
                        Directory.CreateDirectory("yt-dlp");
                        client.DownloadFileAsync(new Uri(urlNow), "yt-dlp\\yt-dlp.exe");

                        while (!downloadDone)
                        {
                            Thread.Sleep(100);
                        }
                        return true;
                    }
                    downloadMatches = downloadMatches.NextMatch();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error downloading yt-dlp releases: " + ex.Message);
            }
            return false;
        }

        public static string GetCommandPath(string command)
        {
            if (File.Exists($"yt-dlp\\{command}.exe"))
            {
                return $"yt-dlp\\{command}.exe";
            }
            else
            {
                return command;
            }
        }
    }
}
