using ReencGUI.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
            Combo_Presets.Items.Clear();
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
            List<string> vfArgs = new List<string>();

            if (pre.vresolution != null && Regex.IsMatch(pre.vresolution, @"^(\d+)(?:x|:)(\d+)$"))
            {
                try
                {
                    var dimensions = Regex.Match(pre.vresolution, @"^(\d+)(?:x|:)(\d+)$").Groups.OfType<Group>().Skip(1).Select(g => int.Parse(g.Value)).ToList();
                    vfArgs.Add($"scale={dimensions[0]}:{dimensions[1]}");
                    vfArgs.Add("setsar=1");
                }
                catch (Exception) { }
            }

            string otherArgs = pre.otherArgs ?? "";
            string regexVFArgs = @"-vf\s+(?:(?:([^""=]+=[^\s""]+))|(?:""([^=]+=[^""]+)""))\s*";
            Match otherVFArgs = Regex.Match(otherArgs, regexVFArgs);
            while (otherVFArgs.Success)
            {
                vfArgs.Add(otherVFArgs.Groups[1].Value);
                otherVFArgs = otherVFArgs.NextMatch();
            }
            otherArgs = Regex.Replace(otherArgs, regexVFArgs, "").Trim();

            string usedVcodec = "";
            if (pre.vcodecs.Any())
            {
                usedVcodec = pre.vcodecs.Where(x => MainWindow.instance.encoders.Any(y => y.ID == x)).First();
            }
            string usedAcodec = pre.acodec;

            List<string> args = new List<string>
            {
                "-i", $"\"{path}\"",
                (pre.vbitrate != "" ? $"-b:v {pre.vbitrate}" : ""),
                (pre.vcodecs.Any() ? $"-c:v {usedVcodec}" : ""),
                (pre.abitrate != "" ? $"-b:a {pre.abitrate}" : ""),
                (pre.acodec != "" ? $"-c:a {pre.acodec}" : ""),
                (vfArgs.Any() ? $"-vf \"{string.Join(",", vfArgs)}\"" : ""),
                otherArgs,
                $"\"{outputPath}\""
            };
            Action<UIFFMPEGOperationEntry, int> onFinished = null;
            if (Check_DeleteSourceMedia.IsChecked == true)
            {
                onFinished = (ui, exit) =>
                {
                    try
                    {
                        File.Delete(path);
                    } catch (Exception ex)
                    {
                        Console.WriteLine($"Error deleting source media: {ex.Message}");
                    }
                };
            }
            MainWindow.instance.EnqueueEncodeOperation(args, media.Duration, usedVcodec != "" ? usedVcodec : usedAcodec, outputPath, onFinished);
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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            PresetManager.PromptInstallPreset();
            LoadPresets();
        }
    }
}
