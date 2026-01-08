using Microsoft.Win32;
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
        List<CreateFilePreset> presets;

        public WindowYTDLPDownload(MainWindow caller)
        {
            this.caller = caller;
            InitializeComponent();
            LoadPresets();

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
                        UpdateFullArgsLabel();
                    });

                    currentVideo = YTDLP.GetVideoInfo(nextURL);
                    Dispatcher.Invoke(() =>
                    {
                        SetMetadata(currentVideo);
                        UpdateFullArgsLabel();
                    });
                    metaURLNow = nextURL;
                }
                Thread.Sleep(500);
            }
        }

        private void LoadPresets()
        {
            Combo_Presets.Items.Clear();
            presets = PresetManager.LoadPresets();

            foreach (var preset in presets)
            {
                Combo_Presets.Items.Add(preset.name);
            }
            Combo_Presets.SelectedIndex = 0;
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
            if (Input_URL.InputField.Text != "")
            {
                var args = MakeYTDLPArgs();
                bool reencodeAfterDownload = Checkbox_RunReenc.IsChecked == true;
                CreateFilePreset reencPreset = (uint)Combo_Presets.SelectedIndex < presets.Count ? presets[Combo_Presets.SelectedIndex] : null;
                caller.EnqueueOtherOperation((entry) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        entry.Label_Primary.Text = currentVideo != null ? $"{currentVideo.title}" : "YT-DLP video";
                    });

                    string outputFile = reencodeAfterDownload ? YTDLP.GetOutputFileName(args) : null;
                    bool downloadResult = YTDLP.RunDownload(args, entry);
                    if (outputFile != null && downloadResult && reencPreset != null)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            WindowQuickReencode.QueueReencodeWithPreset(outputFile, reencPreset, true);
                        });
                    }
                });
                if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                {
                    Close();
                }
            } else
            {
                MessageBox.Show("URL cannot be empty.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private void Button_OutputFolderPick_Click(object sender, RoutedEventArgs e)
        {
            using (System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog())
            {
                fbd.Description = "Select output folder for downloads";
                fbd.SelectedPath = Input_OutputFolder.InputField.Text;
                fbd.ShowNewFolderButton = true;
                System.Windows.Forms.DialogResult result = fbd.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    Input_OutputFolder.InputField.Text = fbd.SelectedPath;
                }
            }
        }

        private void LoadPreset_Click(object sender, RoutedEventArgs e)
        {
            PresetManager.PromptInstallPreset();
            LoadPresets();
        }
    }
}
