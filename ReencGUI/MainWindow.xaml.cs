using ReencGUI.UI;
using System;
using System.Collections.Generic;
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
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            WindowUtil.SetWindowDarkMode(this);
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
                WindowCreateFile wd = new WindowCreateFile(from x in media.streams
                                                           select new StreamTarget
                                                           {
                                                               mediaInfo = media,
                                                               streamInfo = x,
                                                               indexInStream = media.streams.IndexOf(x)
                                                           });
                wd.Input_OutFileName.InputField.Text = fileName + ".reenc.mp4";
                wd.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error processing file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

        public void ProcessNextEncode()
        {
            if (!encoding && encodeQueue.Any())
            {
                encoding = true;
                EncodeOperation next = encodeQueue.Dequeue();
                next.uiQueueEntry.Label_Primary.Content = $"Encoding";

                FFMPEG.RunCommandWithAsyncOutput("ffmpeg", next.ffmpegArgs, (line) =>
                {
                    if (line != null)
                    {
                        Console.WriteLine(line);

                        Match match = Regex.Match(line, @"([^\s]+)=\s*([^\s]+)");
                        Dictionary<string, string> logOutputKVs = new Dictionary<string, string>();
                        while (match.Success)
                        {
                            string key = match.Groups[1].Value;
                            string value = match.Groups[2].Value;
                            logOutputKVs[key] = value;
                            match = match.NextMatch();
                        }

                        Dispatcher.Invoke(() =>
                        {
                            next.uiQueueEntry.UpdateProgressBasedOnLogKVs(logOutputKVs, next.outputDuration);
                        });
                    }
                },
                (exit) =>
                {
                    Console.WriteLine($"FFMPEG exited with code {exit}");
                    Dispatcher.Invoke(() =>
                    {
                        if (exit != 0)
                        {
                            MessageBox.Show($"FFMPEG Error: exit code {exit}", "Error");
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
    }
}
