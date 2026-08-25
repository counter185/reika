using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;

namespace reika.Core
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
            public string autoFormat;
            public string autoExt;
            public string filename;
            public List<YTDLPFormat> formats;
        }

        public static YTDLPVideo GetVideoInfo(List<string> args)
        {
            if (!args.Any() || args.Last() == "")
            {
                return null;
            }

            try
            {

                List<string> listArgs = new List<string> {
                    "-j",
                    "--list-formats"
                };

                string cookiesFromBrowser = Settings.settings.FromKey("reika.ytdlp.cookiesFromBrowser").GetString();
                if (cookiesFromBrowser != "")
                {
                    listArgs.Add("--cookies-from-browser");
                    listArgs.Add(cookiesFromBrowser);
                }

                List<string> output = FFMPEG.RunCommandAndGetOutput(GetCommandPath("yt-dlp"), listArgs.Concat(args));

                string json = output.Where(x => x.StartsWith("{\"")).FirstOrDefault();
                if (json != null)
                {
                    //anything to not add newtonsoft.json
                    var jsonReader = JsonReaderWriterFactory.CreateJsonReader(Encoding.UTF8.GetBytes(json), new System.Xml.XmlDictionaryReaderQuotas());

                    var root = XElement.Load(jsonReader);
                    YTDLPVideo video = new YTDLPVideo();
                    video.id = root.XPathSelectElement("//id")?.Value;
                    video.title = root.XPathSelectElement("//title")?.Value;
                    video.uploader = root.XPathSelectElement("//uploader")?.Value;
                    video.autoFormat = root.XPathSelectElement("format_id")?.Value;
                    video.autoExt = root.XPathSelectElement("ext")?.Value;
                    video.filename = root.XPathSelectElement("filename")?.Value;
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

        public static string GetOutputFileName(List<string> args)
        {
            YTDLPVideo vid = GetVideoInfo(args);
            if (vid == null || vid.filename == null || vid.filename == "")
            {
                return null;
            }
            return vid.filename;
        }

        public static bool RunDownload(List<string> args, IOperationEntryUI progress)
        {
            bool finished = false;
            int exitCode = -1;
            FFMPEG.RunCommandWithAsyncOutput(GetCommandPath("yt-dlp"), args, 
                (s) => {
                    progress.SetTextSecondary(s);
                    progress.UpdateProgressBasedOnYTDLPLine(s);
                },
                (ec) => { finished = true; exitCode = ec; });

            while (!finished)
            {
                Thread.Sleep(100);
            }
            return exitCode == 0;
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

        public static string GetDenoVersion()
        {
            try
            {
                List<string> output = FFMPEG.RunCommandAndGetOutput(GetCommandPath("deno"), new List<string> { "-version" });
                if (output.Count > 0)
                {
                    return output[0];
                }
                return "";
            }
            catch { 
                return ""; 
            }
        }
    }
}
