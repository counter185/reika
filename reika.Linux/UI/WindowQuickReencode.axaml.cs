using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using reika.Core;
using reika.Linux.UI.Popup;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace reika.Linux.UI
{
    public partial class WindowQuickReencode : Window
    {
        List<CreateFilePreset> presets;

        public WindowQuickReencode()
        {
            InitializeComponent();
            LoadPresets();
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
            try
            {
                QueueReencodeWithPreset(path, pre, Check_DeleteSourceMedia.IsChecked == true);
            }
            catch (Exception ex)
            {
                PopupOK.Show($"Failed to process file:\n {ex.Message}", "Error", PopupStyle.Error);
            }
        }

        public static void QueueReencodeWithPreset(string path, CreateFilePreset pre, bool deleteSource)
        {
            FFMPEG.MediaInfo media = FFMPEG.GetMediaInfoForFile(path);
            if (pre is DynamicCreateFilePreset dynamicPreset)
            {
                dynamicPreset.Recalculate(media);
            }
            string outputPath = path + ".reenc" + (pre.requiredExtension ?? ".mp4");
            List<string> vfArgs = new List<string>();

            if (pre.cropString != null)
            {
                vfArgs.Add($"crop={pre.cropString}");
            }
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
            var matchingVcodecs = pre.vcodecs.Where(x => FFMPEGCodecs.encoders.Any(y => y.ID == x));
            if (matchingVcodecs.Any())
            {
                usedVcodec = matchingVcodecs.First();
            }
            else if (pre.vcodecs.Where(x => x.Any()).Any())
            {
                throw new ArgumentException($"None of the codecs ({String.Join(",", pre.vcodecs)}) are available.");
            }
            string usedAcodec = pre.acodec;

            List<string> args = new List<string>
            {
                "-i", $"\"{path}\"",
                (pre.vbitrate != "" ? $"-b:v {pre.vbitrate}" : ""),
                (usedVcodec != "" ? $"-c:v {usedVcodec}" : ""),
                (pre.abitrate != "" ? $"-b:a {pre.abitrate}" : ""),
                (pre.acodec != "" ? $"-c:a {pre.acodec}" : ""),
                (vfArgs.Any() ? $"-vf \"{string.Join(",", vfArgs)}\"" : ""),
                otherArgs,
                $"\"{outputPath}\""
            };
            Action<UIFFMPEGOperationEntry, int> onFinished = null;
            if (deleteSource)
            {
                onFinished = (ui, exit) =>
                {
                    try
                    {
                        File.Delete(path);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error deleting source media: {ex.Message}");
                    }
                };
            }
            MainWindow.instance.EnqueueEncodeOperation(args, media.Duration, usedVcodec != "" ? usedVcodec : usedAcodec, outputPath, onFinished);
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.DataTransfer.TryGetFiles() is { } files)
            {
                foreach (var file in files)
                {
                    var path = file.Path.LocalPath;
                    try
                    {
                        ProcessFile(path);
                    }
                    catch (Exception ex)
                    {
                        PopupOK.Show($"Failed to process file:\n {ex.Message}", "Error", PopupStyle.Error);
                    }
                }
            }
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            await PresetUtils.PromptInstallPreset(this);
            LoadPresets();
        }
    }
}