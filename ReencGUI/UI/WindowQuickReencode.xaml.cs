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

namespace ReencGUI
{
    /// <summary>
    /// Logika interakcji dla klasy WindowQuickReencode.xaml
    /// </summary>
    public partial class WindowQuickReencode : Window
    {
        List<CreateFilePreset> presets;

        public WindowQuickReencode()
        {
            InitializeComponent();
            LoadPresets();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            WindowUtil.SetWindowDarkMode(this);
        }

        private void LoadPresets()
        {
            presets = PresetManager.LoadPresets();

            foreach (var preset in presets)
            {
                Combo_Presets.Items.Add(preset.name);
            }
            Combo_Presets.SelectedIndex = 0;
        }
        private void ProcessFile(string path)
        {
            CreateFilePreset pre = presets[Combo_Presets.SelectedIndex];
            FFMPEG.MediaInfo media = FFMPEG.GetMediaInfoForFile(path);
            if (pre is DynamicCreateFilePreset dynamicPreset)
            {
                dynamicPreset.Recalculate(media);
            }
            //todo: let presets choose other extensions
            string outputPath = path + ".reenc.mp4";
            List<string> args = new List<string>
            {
                "-i", $"\"{path}\"",
                (pre.vbitrate != "" ? $"-b:v {pre.vbitrate}" : ""),
                (pre.vcodecs.Any() ? $"-c:v {pre.vcodecs.Where(x=>MainWindow.instance.encoders.Any(y=>y.ID == x)).First()}" : ""),
                (pre.abitrate != "" ? $"-b:a {pre.abitrate}" : ""),
                (pre.acodec != "" ? $"-c:a {pre.acodec}" : ""),
                (pre.otherArgs != "" ? pre.otherArgs : ""),
                $"\"{outputPath}\""
            };
            MainWindow.instance.EnqueueEncodeOperation(args, media.Duration, null);
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                foreach (string file in (string[])e.Data.GetData(DataFormats.FileDrop))
                {
                    try
                    {
                        ProcessFile(file);
                    } catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to process file:\n {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
    }
}
