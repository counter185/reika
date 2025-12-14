using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            WindowUtil.SetWindowDarkMode(this);
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
                autoPick.Label_FormatID.Visibility = Visibility.Collapsed;
                autoPick.Label_VideoDetails.Visibility = Visibility.Collapsed;
                autoPick.Label_AudioDetails.Visibility = Visibility.Collapsed;
                autoPick.Label_Extension.Visibility = Visibility.Collapsed;
                RadioButton autoRB = new RadioButton();
                autoRB.Content = autoPick;
                autoRB.GroupName = "FormatSel";
                autoRB.VerticalContentAlignment = VerticalAlignment.Center;
                autoRB.IsChecked = true;
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
                    ListBox_FormatList.Items.Add(rb);
                }
            }
        }

        void URLChanged()
        {
            currentVideo = YTDLP.GetVideoInfo(Input_URL.InputField.Text);
            SetMetadata(currentVideo);
            //todo:async this
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
