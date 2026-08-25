using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using reika.Core;

namespace reika.Linux
{
    public static class LinuxExternalDownloads
    {
        public static bool FFMPEGDownloadLatest(IOperationEntryUI progressCallback)
        {
            progressCallback.SetTextPrimary("Finding latest FFMPEG release");
            progressCallback.SetTextSecondary("");

            string releasesURL = "https://api.github.com/repos/BtbN/FFmpeg-Builds/releases";
            WebClient client = new WebClient();
            client.Headers.Add("User-Agent", "reika/1.0");
            client.Headers.Add("Accept", "application/json");
            try
            {
                //json parsers are for the weak
                string jsons = client.DownloadString(releasesURL);
                string nextUrl = Regex.Match(jsons, @"""url"":\s*""(https://api\.github\.com/repos/BtbN/FFmpeg-Builds/releases/[0-9]+)""").Groups[1].Value;

                client.Headers.Add("User-Agent", "reika/1.0");
                string jsonss = client.DownloadString(nextUrl);

                Match downloadMatches = Regex.Match(jsonss,
                    @"""browser_download_url"":\s*""([^""]+)""");
                while (downloadMatches.Success)
                {
                    string urlNow = downloadMatches.Groups[1].Value;
                    if (urlNow.Contains("ffmpeg-master") && urlNow.Contains("linux64")
                        && urlNow.Contains("latest") && urlNow.Contains(".tar.xz")
                        && !urlNow.Contains("shared"))
                    {
                        progressCallback.SetTextPrimary("Downloading FFMPEG");

                        Console.WriteLine("Downloading FFMPEG release from: " + urlNow);
                        client.Headers.Add("User-Agent", "ReencGUI/1.0");

                        bool downloadDone = false;
                        client.DownloadProgressChanged += (sender, e) =>
                        {
                            progressCallback.SetTextSecondary($"{(double)e.BytesReceived / Utils.Megabytes(1):.02}MB / {(double)e.TotalBytesToReceive / Utils.Megabytes(1):.02}MB");
                            progressCallback.SetProgress(e.ProgressPercentage);
                        };
                        client.DownloadFileCompleted += (sender, e) =>
                        {
                            downloadDone = true;
                        };
                        client.DownloadFileAsync(new Uri(urlNow), "ffmpeg.tar.xz");

                        while (!downloadDone)
                        {
                            Thread.Sleep(100);
                        }

                        progressCallback.SetTextPrimary("Extracting FFMPEG");
                        progressCallback.SetTextSecondary("");

                        Console.WriteLine("Extracting FFMPEG release...");
                        Directory.CreateDirectory("ffmpeg");
                        var extractTargets = new List<string> {
                            "ffmpeg-master-latest-linux64-gpl/bin/ffmpeg", 
                            "ffmpeg-master-latest-linux64-gpl/bin/ffprobe", 
                            "ffmpeg-master-latest-linux64-gpl/bin/ffplay"
                        };
                        int done = 0;
                        foreach (string path in extractTargets)
                        {
                            FFMPEG.RunCommandAndGetOutput("tar", new string[] {"-xf", "ffmpeg.tar.xz", path, "-C", "ffmpeg", "--strip-components=2"});
                            progressCallback.SetTextSecondary(path.Split("/").Last());
                            progressCallback.SetTextSecondary2($"{done} / {extractTargets.Count()} files");
                            progressCallback.SetProgress((double)done / extractTargets.Count() * 100);
                            done++;
                        }
                        File.Delete("ffmpeg.tar.xz");
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
    }
}