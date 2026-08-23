using Microsoft.Win32;
using ReencGUI.UI;
using reika.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ReencGUI
{
    public static class WindowsExternalDownloads
    {
        public static bool FFMPEGShouldUseEssentialBuild()
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

        public static bool FFMPEGDownloadLatest(UIFFMPEGOperationEntry progressCallback)
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
                    if (urlNow.Contains("ffmpeg") && urlNow.Contains(FFMPEGShouldUseEssentialBuild() ? "essential" : "full")
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
                                progressCallback.Label_Secondary.Content = $"{(double)e.BytesReceived / Utils.Megabytes(1):.02}MB / {(double)e.TotalBytesToReceive / Utils.Megabytes(1):.02}MB";
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
                            entry.ExtractToFile(Path.Combine("ffmpeg", entry.Name), true);
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

        public static bool DenoInstallLatest(UIFFMPEGOperationEntry progress)
        {
            bool finished = false;
            int exitCode = -1;
            FFMPEG.RunCommandWithAsyncOutput($"{Environment.GetEnvironmentVariable("SYSTEMROOT")}\\Sysnative\\conhost.exe", new List<string> { "winget", "install", "DenoLand.Deno" },
                (s) => {
                    progress.Dispatcher.Invoke(() => {
                        progress.Label_Secondary.Content = s;
                        //progress.UpdateProgressBasedOnYTDLPLine(s);
                    });
                },
                (ec) => { finished = true; exitCode = ec; });

            while (!finished)
            {
                Thread.Sleep(100);
            }
            return exitCode == 0;
        }

        public static bool YTDLPDownloadLatest(UIFFMPEGOperationEntry progressCallback)
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
    }
}
