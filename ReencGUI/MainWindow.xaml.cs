using ReencGUI.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shell;
using reika.Core;
using ReencGUI.UI.Popup;

namespace ReencGUI
{
    /// <summary>
    /// Logika interakcji dla klasy MainWindow.xaml
    /// </summary>
    public partial class MainWindow : DarkWindow
    {
        struct EncodeOperation
        {
            public IEnumerable<string> ffmpegArgs;
            public ulong outputDuration;
            public string visualEncoderID;
            public string outputFileName;
            public UIFFMPEGOperationEntry uiQueueEntry;
            public Action<UIFFMPEGOperationEntry, int> onFinished;
        }
        struct OtherOperation
        {
            public Action<UIFFMPEGOperationEntry> action;
            public UIFFMPEGOperationEntry uiQueueEntry;
        }

        public static MainWindow instance;

        volatile bool downloadingFFMPEG = false;
        volatile bool downloadingYTDLP = false;

        List<EncodeOperation> encodeQueue = new List<EncodeOperation>();
        Queue<OtherOperation> otherOpsQueue = new Queue<OtherOperation>();
        int encodesRunning = 0;
        volatile bool doingOtherOp = false;

        public MainWindow()
        {
            instance = this;
            InitializeComponent();

            Label_VersionNumber.Content = string.Join(" ", new string[] { ReleaseInfo.Version, ReleaseInfo.Ref }); 


            if ((!File.Exists("ffmpeg\\ffmpeg.exe")
                || !File.Exists("ffmpeg\\ffprobe.exe"))
                && !FFMPEG.TestFFMPEG())
            {
                if (PopupYesNo.Show("FFMPEG not found. Download it now?" +
                    "\n\n*At least 500MB of free space is required" +
                    "\n*FFMPEG will be downloaded from github.com/GyanD/codexffmpeg/releases", 
                    "FFMPEG Not Found", PopupStyle.Warning) == PopupResult.Yes)
                {
                    StartFFMPEGDownload(true);
                }
                else
                {
                    PopupOK.Show($"FFMPEG was not found in PATH.\nClosing.", "FFMPEG Not Found", PopupStyle.Error);
                    Environment.Exit(-1);
                }
            } else
            {
                ReloadEncoders();
            }

            AppData.GetAppDataPath();
            AppData.GetAppDataSubdir("presets");
            Label_HwInfo.Content = WindowsUtils.GetSystemHardwareInfo();
        }

        public void StartFFMPEGDownload(bool required)
        {
            if (!downloadingFFMPEG)
            {
                downloadingFFMPEG = true;
                EnqueueOtherOperation((entry) =>
                {
                    if (WindowsExternalDownloads.FFMPEGDownloadLatest(entry))
                    {
                        ReloadEncoders();
                        downloadingFFMPEG = false;
                    }
                    else
                    {
                        if (required)
                        {
                            PopupOK.Show("Failed to download FFMPEG.\nClosing.", "FFMPEG Download Failed", PopupStyle.Error);
                            Environment.Exit(-1);
                        }
                        else
                        {
                            PopupOK.Show("Failed to download FFMPEG.", "FFMPEG Download Failed", PopupStyle.Error);
                            downloadingFFMPEG = false;
                        }
                    }
                });
            } else
            {
                PopupOK.Show("Already downloading FFMPEG.", "FFMPEG Download Failed", PopupStyle.Error);
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            if (encodeQueue.Count > 0 || otherOpsQueue.Count > 0 || encodesRunning > 0 || doingOtherOp)
            {
                if (PopupYesNo.Show("Operations are still in queue." +
                    "\nClosing reika will not stop any running encode operations." +
                    "\nClose anyway?", "Confirm close", PopupStyle.Warning) == PopupResult.No)
                {
                    e.Cancel = true;
                    return;
                }
            }
            this.Hide();
            WindowsUtils.FFMPEGCleanupThumbnails();
            instance = null;
            Environment.Exit(0);
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (downloadingFFMPEG)
            {
                PopupOK.Show("FFMPEG is currently being downloaded.\nPlease wait until it finishes.", "FFMPEG download in progress", PopupStyle.Info);
                return;
            }

            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                return;
            }

            try
            {
                foreach (string fileName in (string[])e.Data.GetData(DataFormats.FileDrop))
                {
                    OpenCreateFileWindowForFile(fileName);
                }
            }
            catch (Exception ex)
            {
                PopupOK.Show($"Error processing file: {ex.Message}", "Error", PopupStyle.Error);
            }

        }

        public void OpenCreateFileWindowForFile(string fileName)
        {
            FFMPEG.MediaInfo media = FFMPEG.GetMediaInfoForFile(fileName);
            if (media != null)
            {
                WindowCreateFile wd = new WindowCreateFile(from x in media.streams
                                                           select new StreamTarget
                                                           {
                                                               mediaInfo = media,
                                                               streamInfo = x,
                                                               indexInStream = media.streams.IndexOf(x)
                                                           });
                wd.Input_OutFileName.InputField.Text = fileName + ".reenc";
                wd.Tbox_Extension.Text = ".mp4";
                wd.Show();
            }
            else
            {
                PopupOK.Show("Failed to identify file.\nCheck if ffmpeg is installed.", "Invalid File", PopupStyle.Error);
            }
        }

        private void ReloadEncoders()
        {
            FFMPEGCodecs.LoadCodecList();

            Dispatcher.Invoke(() =>
            {
                EnqueueOtherOperation((entry) => FFMPEGCodecs.TestHWEncoders(entry));
                Label_FFMPEGVersion.Text = FFMPEG.GetFFMPEGVersion();
            });
        }

        public void EnqueueOtherOperation(Action<UIFFMPEGOperationEntry> action)
        {
            UIFFMPEGOperationEntry entry = new UIFFMPEGOperationEntry();
            entry.Label_Primary.Text = $"In queue";
            entry.Label_Secondary.Content = "";
            entry.Label_Secondary2.Content = "";
            Panel_Operations.Items.Add(entry);
            otherOpsQueue.Enqueue(new OtherOperation
            {
                action = action,
                uiQueueEntry = entry
            });
            ProcessNextOtherOperation();
        }

        public void EnqueueEncodeOperation(IEnumerable<string> args, ulong outputDuration, string visualEncoderID, string outFileName, Action<UIFFMPEGOperationEntry, int> onFinished = null)
        {

            UIFFMPEGOperationEntry entry = new UIFFMPEGOperationEntry();
            entry.Background = WindowPickEncoder.GetGradientForCodecID(visualEncoderID);
            entry.Label_Primary.Text = $"In queue";
            entry.Label_Secondary.Content = Utils.SanitizeForXAML(Path.GetFileName(outFileName));
            entry.Label_Secondary2.Content = "";
            entry.SetProgressBarStyleForEncoderID(visualEncoderID);
            Panel_Operations.Items.Add(entry);

            EncodeOperation op = new EncodeOperation
            {
                ffmpegArgs = args,
                outputDuration = outputDuration,
                uiQueueEntry = entry,
                onFinished = onFinished,
                outputFileName = outFileName,
                visualEncoderID = visualEncoderID
            };

            encodeQueue.Add(op);

            entry.onRightClick = (a) =>
            {
                if (PopupYesNo.Show("Run this encode operation now?", "reika") == PopupResult.Yes)
                {
                    Dispatcher.Invoke(() =>
                    {
                        if (encodeQueue.Contains(op))
                        {
                            encodeQueue.Remove(op);
                            ProcessEncode(op);
                        }
                    });
                }
            };

            ProcessNextEncode();
        }

        public void EncodeFailed(string details1, string details2, bool manuallyCancelled,
            Action<UIFFMPEGFailedReencode> onRetry, Action<UIFFMPEGFailedReencode> onViewLog)
        {
            UIFFMPEGFailedReencode failedReencode = new UIFFMPEGFailedReencode();
            if (manuallyCancelled)
            {
                failedReencode.Label_Primary.Content = "Encode cancelled";
            }
            failedReencode.Label_Secondary.Content = details1;
            failedReencode.Label_Secondary2.Content = details2;
            failedReencode.Button_Retry.Click += (s, e) =>
            {
                onRetry(failedReencode);
                Panel_Operations.Items.Remove(failedReencode);
            };
            failedReencode.Button_ViewLog.Click += (s, e) =>
            {
                onViewLog(failedReencode);
            };
            failedReencode.MouseRightButtonDown += (s, e) =>
            {
                Panel_Operations.Items.Remove(failedReencode);
            };
            Panel_Operations.Items.Add(failedReencode);
        }

        public void ProcessNextEncode()
        {
            if (encodesRunning == 0 && encodeQueue.Any())
            {
                EncodeOperation next = encodeQueue.First();
                encodeQueue.RemoveAt(0);
                ProcessEncode(next);
            }
        }

        private void ProcessEncode(EncodeOperation next)
        {
            this.TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
            encodesRunning++;
            next.uiQueueEntry.Label_Primary.Text = Path.GetFileName(next.outputFileName);
            bool cancelling = false;

            List<string> logLines = new List<string>();
            Process newP = FFMPEG.RunCommandWithAsyncOutput("ffmpeg", next.ffmpegArgs, (line) =>
            {
                if (line != null)
                {
                    Console.WriteLine(line);

                    Match match = Regex.Match(line, @"([^\s]+)=\s*([^\s]+)");
                    Dictionary<string, string> logOutputKVs = new Dictionary<string, string>();
                    bool anyFound = false;
                    while (match.Success)
                    {
                        anyFound = true;
                        string key = match.Groups[1].Value;
                        string value = match.Groups[2].Value;
                        logOutputKVs[key] = value;
                        match = match.NextMatch();
                    }

                    if (anyFound)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            next.uiQueueEntry.UpdateProgressBasedOnLogKVs(logOutputKVs, next.outputDuration);
                            this.TaskbarItemInfo.ProgressValue = next.uiQueueEntry.ProgressBar_Operation.Value / 100d;
                        });
                    }
                    else
                    {
                        logLines.Add(line);
                    }
                }
            },
            (exit) =>
            {
                Console.WriteLine($"FFMPEG exited with code {exit:X}");
                Dispatcher.Invoke(() =>
                {
                    this.TaskbarItemInfo.ProgressState = TaskbarItemProgressState.None;
                    if (exit != 0)
                    {
                        EncodeFailed($"Exit code {exit:X}", "", cancelling,
                            (el) =>
                            {
                                EnqueueEncodeOperation(next.ffmpegArgs, next.outputDuration, next.visualEncoderID, next.outputFileName);
                            },
                            (el) =>
                            {
                                File.WriteAllText("ffmpeg_log.txt", string.Join("\n", logLines));
                                Process.Start("notepad.exe", "ffmpeg_log.txt");
                            });
                    }
                    else if (!cancelling)
                    {
                        next.onFinished?.Invoke(next.uiQueueEntry, exit);
                    }
                    Panel_Operations.Items.Remove(next.uiQueueEntry);
                    encodesRunning--;
                    ProcessNextEncode();
                });
            });

            next.uiQueueEntry.onRightClick = (b) =>
            {
                if (!cancelling 
                    && PopupYesNo.Show("Are you sure you want to cancel this operation?", "Cancel Operation", PopupStyle.Warning) == PopupResult.Yes)
                {
                    cancelling = true;
                    Dispatcher.Invoke(() =>
                    {
                        next.uiQueueEntry.Label_Primary.Text = $"Cancelling...";
                    });
                    newP.StandardInput.WriteLine("q");
                    newP.StandardInput.Flush();
                    Thread.Sleep(1000);
                    try
                    {
                        newP.Kill();
                    }
                    catch (Exception) { } //who cares
                }
            };
        }

        public void ProcessNextOtherOperation()
        {
            if (!doingOtherOp && otherOpsQueue.Any())
            {
                doingOtherOp = true;
                OtherOperation next = otherOpsQueue.Dequeue();
                next.uiQueueEntry.Label_Primary.Text = $"Processing";
                new Thread(() =>
                {
                    next.action(next.uiQueueEntry);
                    doingOtherOp = false;
                    Dispatcher.Invoke(() =>
                    {
                        Panel_Operations.Items.Remove(next.uiQueueEntry);
                    });
                    Dispatcher.Invoke(() =>
                    {
                        ProcessNextOtherOperation();
                    });
                }).Start();
            }
        }

        private void Button_NewEmpty_Click(object sender, RoutedEventArgs e)
        {
            if (!downloadingFFMPEG)
            {
                new WindowCreateFile().Show();
            } else
            {
                PopupOK.Show("FFMPEG is currently being downloaded.\nPlease wait until it finishes.", "FFMPEG download in progress", PopupStyle.Info);
            }
        }

        private void Button_QuickReenc_Click(object sender, RoutedEventArgs e)
        {
            if (downloadingFFMPEG)
            {
                PopupOK.Show("FFMPEG is currently being downloaded.\nPlease wait until it finishes.", "FFMPEG download in progress", PopupStyle.Info);
                return;
            }
            new WindowQuickReencode().Show();
        }

        private void Button_Settings_Click(object sender, RoutedEventArgs e)
        {
            new WindowSettings().Show();
        }

        private void Button_Download_Click(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(YTDLP.GetCommandPath("yt-dlp")) || downloadingYTDLP)
            {
                if (downloadingYTDLP)
                {
                    PopupOK.Show("yt-dlp is currently being downloaded.\nPlease wait until it finishes.", "yt-dlp download in progress", PopupStyle.Info);
                }
                else
                {
                    downloadingYTDLP = true;
                    EnqueueOtherOperation((entry) =>
                    {
                        if (WindowsExternalDownloads.YTDLPDownloadLatest(entry))
                        {
                            Dispatcher.Invoke(()=>PopupOK.Show("yt-dlp downloaded successfully.", "yt-dlp Downloaded", PopupStyle.Info));
                        }
                        else
                        {
                            Dispatcher.Invoke(() => PopupOK.Show("Failed to download yt-dlp.", "yt-dlp Download Failed", PopupStyle.Error));
                        }
                        downloadingYTDLP = false;
                    });
                }
            } else
            {
                new WindowYTDLPDownload(this).Show();
            }
        }
    }
}
