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

        public static List<string> RunCommandAndGetOutput(string command, IEnumerable<string> args)
        {
            List<string> output = new List<string>();
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = string.Join(" ", args),
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
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
                        Thread.Sleep(100);
                        process.WaitForExit();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error running FFMPEG command: " + ex.Message, ex);
            }
            return output;
        }
        public static void RunCommandWithAsyncOutput(string command, IEnumerable<string> args, 
            Action<string> outputLineCallback,
            Action<int> exitCallback = null)
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = string.Join(" ", args),
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
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
            }
            catch (Exception ex)
            {
                throw new Exception("Error running FFMPEG command: " + ex.Message, ex);
            }
        }

        private static void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            throw new NotImplementedException();
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

        public static BitmapImage ExtractThumbnail(string filename)
        {
            //todo: specific stream selection
            Random r = new Random();
            string tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"thumbnail_{r.Next(1000000)}.jpg");
            string[] args = new string[]
            {
                "-y",
                "-i", $"\"{filename}\"",
                "-ss", "00:00:01.000",
                "-frames:v", "1",
                $"\"{tempFile}\""
            };
            string argstr = string.Join(" ", args);
            RunCommandAndGetOutput("ffmpeg", args);
            return new BitmapImage(new Uri(tempFile)); ;
        }
    }
}
