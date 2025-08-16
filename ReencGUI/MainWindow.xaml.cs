using ReencGUI.UI;
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
        }
        struct OtherOperation
        {
            public Action<UIFFMPEGOperationEntry> action;
            public UIFFMPEGOperationEntry uiQueueEntry;
        }

        public static MainWindow instance;

        public List<FFMPEG.CodecInfo> decoders;
        public List<FFMPEG.CodecInfo> encoders;

        Queue<EncodeOperation> encodeQueue = new Queue<EncodeOperation>();
        Queue<OtherOperation> otherOpsQueue = new Queue<OtherOperation>();
        bool encoding = false;
        volatile bool doingOtherOp = false;

        public MainWindow()
        {
            instance = this;
            InitializeComponent();

            decoders = FFMPEG.GetAvailableDecoders();
            encoders = FFMPEG.GetAvailableEncoders();

            if (!File.Exists("ffmpeg\\ffmpeg.exe")
                || !File.Exists("ffmpeg\\ffprobe.exe"))
            {
                if (MessageBox.Show("FFMPEG not found. Download it now?", "FFMPEG Not Found", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    EnqueueOtherOperation((entry) => FFMPEG.DownloadLatest(entry));
                } else
                {
                    Application.Current.Shutdown();
                }
            } else
            {
                EnqueueOtherOperation((entry) => TestEncoders(entry));
            }
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
                "nvenc", "amf", "qsv"
            };
            var targetEncoders = encoders.Where(x => hwEncKeywords.Any(y => x.ID.Contains(y))).ToList();
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
                    "-i", "color=black:s=640x360",
                    "-vframes", "1",
                    "-an",
                    "-c:v", enc.ID,
                    "-f", "null",
                    "-"
                };
                List<string> output = FFMPEG.RunFFMPEGCommandlineForOutput(args);
                if (output.Any(x=>x.ToLower().Contains("error")))
                {
                    Dispatcher.Invoke(() =>
                    {
                        encoders.Remove(enc);
                        Console.WriteLine($"Encoder {enc.ID} is not compatible");
                    });
                } else
                {
                    Console.WriteLine($"Encoder {enc.ID} is compatible");
                }
            }
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

        public void EnqueueEncodeOperation(IEnumerable<string> args, ulong outputDuration)
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
                uiQueueEntry = entry
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

                List<string> logLines = new List<string>();
                FFMPEG.RunCommandWithAsyncOutput("ffmpeg", next.ffmpegArgs, (line) =>
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
                        }
                        Panel_Operations.Items.Remove(next.uiQueueEntry);
                        encoding = false;
                        ProcessNextEncode();
                    });
                });
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
                }).Start();
            }
        }

        private void Button_NewEmpty_Click(object sender, RoutedEventArgs e)
        {
            new WindowCreateFile().Show();
        }

        private void Button_QuickReenc_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
