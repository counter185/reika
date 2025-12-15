using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ReencGUI.UI
{
    /// <summary>
    /// Logika interakcji dla klasy WindowYTDLPDownload.xaml
    /// </summary>
    public partial class WindowYTDLPDownload : Window
    {
        MainWindow caller;
        YTDLP.YTDLPVideo currentVideo = null;

        public WindowYTDLPDownload(MainWindow caller)
        {
            this.caller = caller;
            InitializeComponent();

            Input_URL.InputField.TextChanged += (a, b) => URLChanged();

            Input_URL.InputField.TextChanged += (a,b) => UpdateFullArgsLabel();
            Input_ExtraArgs.InputField.TextChanged += (a,b) => UpdateFullArgsLabel();
            ListBox_FormatList.SelectionChanged += (a,b) => UpdateFullArgsLabel();

            SetMetadata(null);

            metaFetchThread = new Thread(MetadataFetchThread);
            metaFetchThread.Start();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            WindowUtil.SetWindowDarkMode(this);
        }

        protected override void OnClosed(EventArgs e)
        {
            metaFetchThread.Abort();
            base.OnClosed(e);
        }

        Thread metaFetchThread;
        string requestedURLNow = "";
        string metaURLNow = null;
        void MetadataFetchThread()
        {
            while (true)
            {
                if (requestedURLNow != metaURLNow)
                {
                    string nextURL = requestedURLNow;
                    Dispatcher.Invoke(() => {
                        Label_VideoTitle.Content = "<fetching media info...>";
                        ListBox_FormatList.Items.Clear();
                        Label_Channel.Content = Label_ID.Content = "";
                    });

                    currentVideo = YTDLP.GetVideoInfo(nextURL);
                    Dispatcher.Invoke(() =>
                    {
                        SetMetadata(currentVideo);
                    });
                    metaURLNow = nextURL;
                }
                Thread.Sleep(500);
            }
        }

        void SetMetadata(YTDLP.YTDLPVideo v)
        {
            Label_VideoTitle.Content = v != null ? v.title : "<no video info>";
            Label_ID.Content = v != null ? $"video ID: {v.id}" : "";
            Label_Channel.Content = v != null ? v.uploader : "";
            ListBox_FormatList.Items.Clear();
            if (v != null)
            {
                UIYTDLPFormatEntry autoPick = new UIYTDLPFormatEntry();
                autoPick.Label_FormatDisplayName.Content = "<autoselect best>";
                autoPick.Label_FormatID.Content = v.autoFormat;
                autoPick.Label_VideoDetails.Visibility = Visibility.Collapsed;
                autoPick.Label_AudioDetails.Visibility = Visibility.Collapsed;
                autoPick.Label_Extension.Content = v.autoExt;
                RadioButton autoRB = new RadioButton();
                autoRB.Content = autoPick;
                autoRB.GroupName = "FormatSel";
                autoRB.VerticalContentAlignment = VerticalAlignment.Center;
                autoRB.IsChecked = true;
                autoRB.Checked += (a,b) => UpdateFullArgsLabel();
                ListBox_FormatList.Items.Add(autoRB);

                var formatListReversed = v.formats.ToList();
                formatListReversed.Reverse();
                foreach (var format in formatListReversed)
                {
                    UIYTDLPFormatEntry entry = new UIYTDLPFormatEntry();
                    entry.ApplyFormat(format);
                    RadioButton rb = new RadioButton();
                    rb.Content = entry;
                    rb.GroupName = "FormatSel";
                    rb.VerticalContentAlignment = VerticalAlignment.Center;
                    rb.Checked += (a, b) => UpdateFullArgsLabel();
                    ListBox_FormatList.Items.Add(rb);
                }
            }
        }

        void UpdateFullArgsLabel()
        {
            Label_FullCommand.Text = "yt-dlp " + string.Join(" ", MakeYTDLPArgs());
        }

        void URLChanged()
        {
            requestedURLNow = Input_URL.InputField.Text;
        }

        private void Button_StartDownload_Click(object sender, RoutedEventArgs e)
        {
            var args = MakeYTDLPArgs();
            caller.EnqueueOtherOperation((entry) =>
            {
                Dispatcher.Invoke(() =>
                {
                    if (currentVideo != null)
                    {
                        entry.Label_Primary.Text = $"{currentVideo.title}";
                    }
                });
                
                YTDLP.RunDownload(args, entry);
            });
            if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                Close();
            }
        }

        public List<string> MakeYTDLPArgs()
        {
            string targetID = null;

            foreach (var rbObj in ListBox_FormatList.Items)
            {
                RadioButton rb = rbObj as RadioButton;
                if (rb.IsChecked == true)
                {
                    UIYTDLPFormatEntry entry = rb.Content as UIYTDLPFormatEntry;
                    targetID = entry.formatID;
                }
            }

            List<string> args = new List<string>();

            if (Input_ExtraArgs.InputField.Text != "")
            {
                args.Add(Input_ExtraArgs.InputField.Text);
            }

            if (targetID != null)
            {
                args.Add("-f");
                args.Add(targetID);
            }

            if (Input_OutputFolder.InputField.Text != "")
            {
                args.Add("-P");
                args.Add($"\"{Input_OutputFolder.InputField.Text}\"");
            }

            args.Add("--ffmpeg-location");
            args.Add("./ffmpeg");

            args.Add($"\"{Input_URL.InputField.Text}\"");

            return args;
        }
    }
}
