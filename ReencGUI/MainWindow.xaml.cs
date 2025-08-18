﻿using ReencGUI.UI;
using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace ReencGUI
{
    /// <summary>
    /// Logika interakcji dla klasy MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        struct EncodeOperation
        {
            public IEnumerable<string> ffmpegArgs;
            public ulong outputDuration;
            public UIFFMPEGOperationEntry uiQueueEntry;
            public Action<UIFFMPEGOperationEntry, int> onFinished;
        }
        struct OtherOperation
        {
            public Action<UIFFMPEGOperationEntry> action;
            public UIFFMPEGOperationEntry uiQueueEntry;
        }

        public static MainWindow instance;

        public List<FFMPEG.CodecInfo> decoders;
        public List<FFMPEG.CodecInfo> encoders;

        volatile bool downloadingFFMPEG = false;

        Queue<EncodeOperation> encodeQueue = new Queue<EncodeOperation>();
        Queue<OtherOperation> otherOpsQueue = new Queue<OtherOperation>();
        bool encoding = false;
        volatile bool doingOtherOp = false;

        public MainWindow()
        {
            instance = this;
            InitializeComponent();


            if (!File.Exists("ffmpeg\\ffmpeg.exe")
                || !File.Exists("ffmpeg\\ffprobe.exe"))
            {
                if (MessageBox.Show("FFMPEG not found. Download it now?" +
                    "\n\n*At least 500MB of free space is required" +
                    "\n*FFMPEG will be downloaded from github.com/GyanD/codexffmpeg/releases", 
                    "FFMPEG Not Found", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    downloadingFFMPEG = true;
                    EnqueueOtherOperation((entry) => {
                        if (FFMPEG.DownloadLatest(entry))
                        {
                            ReloadEncoders();
                            downloadingFFMPEG = false;
                        } else
                        {
                            MessageBox.Show("Failed to download FFMPEG.\nClosing.", "FFMPEG Download Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                            Application.Current.Shutdown();
                        }
                    });
                } else
                {
                    try
                    {
                        FFMPEG.RunFFMPEGCommandlineForOutput(new string[]{ "-version"});
                        FFMPEG.RunFFProbeCommandlineForOutput(new string[]{ "-version"});
                        ReloadEncoders();
                    } catch (Exception)
                    {
                        MessageBox.Show($"FFMPEG was not found in PATH.\nClosing.", "FFMPEG Not Found", MessageBoxButton.OK, MessageBoxImage.Error);
                        Application.Current.Shutdown();
                    }
                    
                }
            } else
            {
                ReloadEncoders();
            }

            AppData.GetAppDataPath();
            AppData.GetAppDataSubdir("presets");
            Label_HwInfo.Content = Utils.GetSystemHardwareInfo();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            WindowUtil.SetWindowDarkMode(this);
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            FFMPEG.CleanupThumbnails();
            instance = null;
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (downloadingFFMPEG)
            {
                MessageBox.Show("FFMPEG is currently being downloaded.\nPlease wait until it finishes.", "FFMPEG download in progress", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                return;
            }


            try
            {
                string fileName = ((string[])e.Data.GetData(DataFormats.FileDrop))[0];
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
                    wd.Input_OutFileName.InputField.Text = fileName + ".reenc.mp4";
                    wd.Show();
                } else
                {
                    MessageBox.Show("Failed to identify file.\nCheck if ffmpeg is installed.", "Invalid File", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error processing file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        private void TestEncoders(UIFFMPEGOperationEntry progressCallback)
        {
            Dispatcher.Invoke(() =>
            {
                progressCallback.Label_Primary.Content = "Testing HW encoders";
                progressCallback.Label_Secondary.Content = "";
                progressCallback.Label_Secondary2.Content = "";
            });
            string[] hwEncKeywords = new string[]
            {
                "nvenc", "amf", "qsv", "vaapi", "_mf", "_vulkan", "d3d1"
            };
            var targetEncoders = encoders.Where(x => hwEncKeywords.Any(y => x.ID.Contains(y))).ToList();
            List<string> compatible = new List<string>(), incompatible = new List<string>();
            int i = 0;
            foreach (var enc in targetEncoders)
            {
                Dispatcher.Invoke(() =>
                {
                    progressCallback.Label_Secondary.Content = Utils.SanitizeForXAML(enc.ID);
                    progressCallback.ProgressBar_Operation.Value = 100 * ((double)(i++) / targetEncoders.Count);
                });
                string[] args =
                {
                    "-loglevel", "error",
                    "-f", "lavfi",
                    "-i", (enc.Type == FFMPEG.CodecType.Video ? "color=black:s=640x360" : "sine=frequency=1000:duration=1"),
                    (enc.Type == FFMPEG.CodecType.Video ? "-vframes 1" : ""),
                    (enc.Type == FFMPEG.CodecType.Video ? "-an" : ""),
                    (enc.Type == FFMPEG.CodecType.Video ? "-c:v" : "-c:a"), enc.ID,
                    "-f", "null",
                    "-"
                };
                List<string> output = FFMPEG.RunFFMPEGCommandlineForOutput(args);
                if (output.Any(x=>x.ToLower().Contains("error")))
                {
                    incompatible.Add(enc.ID);
                    Dispatcher.Invoke(() =>
                    {
                        progressCallback.Label_Secondary2.Content = $"compat. {compatible.Count}/{incompatible.Count} incompat.";
                        encoders.Remove(enc);
                    });
                } else
                {
                    compatible.Add(enc.ID);
                }
            }
            Console.WriteLine($"Compatible HW encoders:\n{string.Join("\n", compatible)}");
            Console.WriteLine($"Incompatible HW encoders:\n{string.Join("\n", incompatible)}");
        }

        private void ReloadEncoders()
        {
            decoders = FFMPEG.GetAvailableDecoders();
            encoders = FFMPEG.GetAvailableEncoders();

            encoders.Insert(0, new FFMPEG.CodecInfo
            {
                Name = "Copy (same as source)",
                ID = "copy",
                Type = FFMPEG.CodecType.Video,
            });

            encoders.Insert(0, new FFMPEG.CodecInfo
            {
                Name = "Copy (same as source)",
                ID = "copy",
                Type = FFMPEG.CodecType.Audio,
            });

            Dispatcher.Invoke(() =>
            {
                EnqueueOtherOperation((entry) => TestEncoders(entry));
                Label_FFMPEGVersion.Text = FFMPEG.GetFFMPEGVersion();
            });
        }

        public void EnqueueOtherOperation(Action<UIFFMPEGOperationEntry> action)
        {
            UIFFMPEGOperationEntry entry = new UIFFMPEGOperationEntry();
            entry.Label_Primary.Content = $"In queue";
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

        public void EnqueueEncodeOperation(IEnumerable<string> args, ulong outputDuration, Action<UIFFMPEGOperationEntry, int> onFinished = null)
        {
            UIFFMPEGOperationEntry entry = new UIFFMPEGOperationEntry();
            entry.Label_Primary.Content = $"In queue";
            entry.Label_Secondary.Content = "";
            entry.Label_Secondary2.Content = "";
            Panel_Operations.Items.Add(entry);

            encodeQueue.Enqueue(new EncodeOperation
            {
                ffmpegArgs = args,
                outputDuration = outputDuration,
                uiQueueEntry = entry,
                onFinished = onFinished
            });
            ProcessNextEncode();
        }

        public void EncodeFailed(string details1, string details2, 
            Action<UIFFMPEGFailedReencode> onRetry, Action<UIFFMPEGFailedReencode> onViewLog)
        {
            UIFFMPEGFailedReencode failedReencode = new UIFFMPEGFailedReencode();
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
            if (!encoding && encodeQueue.Any())
            {
                encoding = true;
                EncodeOperation next = encodeQueue.Dequeue();
                next.uiQueueEntry.Label_Primary.Content = $"Encoding";
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
                            });
                        } else
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
                        if (exit != 0)
                        {
                            EncodeFailed($"Exit code {exit:X}", "", 
                                (el) => { 
                                    EnqueueEncodeOperation(next.ffmpegArgs, next.outputDuration);
                                }, 
                                (el) => { 
                                    File.WriteAllText("ffmpeg_log.txt", string.Join("\n", logLines));
                                    Process.Start("notepad.exe", "ffmpeg_log.txt");
                                });
                        } else if (!cancelling)
                        {
                            next.onFinished?.Invoke(next.uiQueueEntry, exit);
                        }
                        Panel_Operations.Items.Remove(next.uiQueueEntry);
                        encoding = false;
                        ProcessNextEncode();
                    });
                });
                
                next.uiQueueEntry.MouseRightButtonDown += (a, b) =>
                {
                    if (!cancelling && MessageBox.Show("Are you sure you want to cancel this operation?", "Cancel Operation", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                    {
                        cancelling = true;
                        Dispatcher.Invoke(() =>
                        {
                            next.uiQueueEntry.Label_Primary.Content = $"Cancelling...";
                        });
                        newP.StandardInput.WriteLine("q");
                        newP.StandardInput.Flush();
                        Thread.Sleep(1000);
                        try
                        {
                            newP.Kill();
                        } catch (Exception) { } //who cares
                    }
                };
            }
        }
        public void ProcessNextOtherOperation()
        {
            if (!doingOtherOp && otherOpsQueue.Any())
            {
                doingOtherOp = true;
                OtherOperation next = otherOpsQueue.Dequeue();
                next.uiQueueEntry.Label_Primary.Content = $"Processing";
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
                MessageBox.Show("FFMPEG is currently being downloaded.\nPlease wait until it finishes.", "FFMPEG download in progress", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Button_QuickReenc_Click(object sender, RoutedEventArgs e)
        {
            if (downloadingFFMPEG)
            {
                MessageBox.Show("FFMPEG is currently being downloaded.\nPlease wait until it finishes.", "FFMPEG download in progress", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            new WindowQuickReencode().Show();
        }
    }
}
