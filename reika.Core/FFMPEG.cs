using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.IO;
using System.Threading;
using System.Net;
using System.IO.Compression;
using Microsoft.Win32;

namespace reika.Core
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

        public class MediaInfo : ICreateFileSession
        {
            public string fileName;
            public string date;
            public string mediaEncoder;
            public int dH, dM, dS, dMS;
            public string overallBitrate;
            public List<StreamInfo> streams = new List<StreamInfo>();

            public ulong Duration { get => Utils.LengthToMS(dH, dM, dS, dMS); }

            public ulong GetDuration() => Duration;
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

        public static string GetCommandPath(string command)
        {
            if (File.Exists($"ffmpeg/{command}.exe"))
            {
                return $"ffmpeg/{command}.exe";
            } 
            else
            {
                return command;
            }
        }

        public static List<string> RunCommandAndGetOutput(string command, IEnumerable<string> args)
        {
            if (File.Exists($"ffmpeg/{command}.exe"))
            {
                command = $"ffmpeg/{command}.exe";
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
            if (File.Exists($"ffmpeg/{command}.exe"))
            {
                command = $"ffmpeg/{command}.exe";
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
            => ParseFFMPEGCodecList(RunFFMPEGCommandlineForOutput(new string[] { "-decoders" }));
        public static List<CodecInfo> GetAvailableEncoders()
            => ParseFFMPEGCodecList(RunFFMPEGCommandlineForOutput(new string[] { "-encoders" }));
        
        public static MediaInfo GetMediaInfoForFile(string fileName)
            => ParseFFProbeMediaInfo(RunFFProbeCommandlineForOutput(new string[] { $"\"{fileName}\"" }));

        public static bool TestFFMPEG()
        {
            try
            {
                FFMPEG.RunFFMPEGCommandlineForOutput(new string[] { "-version" });
                FFMPEG.RunFFProbeCommandlineForOutput(new string[] { "-version" });
                return true;
            }
            catch (Exception)
            {
                return false;
            }
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
    }
}
