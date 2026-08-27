using ReencGUI.UI;
using ReencGUI.UI.Popup;
using reika.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace ReencGUI
{
    public class CustomTargetSizePreset : TargetFilesizePreset
    {
        static double? sessionDefault = null;

        public CustomTargetSizePreset() : base(1)
        {
            name = "Custom file size target";
            vcodecs = new List<string>() { Settings.settings.FromKey("reika.presets.sizetarget.videoCodec").GetString() };
            acodec = "aac";
            abitrate = "128k";
        }

        protected override void RecalcFromTime(ulong time)
        {
            if (sessionDefault == null)
            {
                WindowInputTargetFileSize wd = new WindowInputTargetFileSize();
                wd.ShowDialog();
                targetSizeBytes = Utils.Megabytes(wd.result != null ? wd.result.Value : Utils.Megabytes(10));
                if (wd.result != null && wd.Checkbox_DontAskAgain.IsChecked == true)
                {
                    sessionDefault = wd.result.Value;
                }
            }
            else
            {
                targetSizeBytes = Utils.Megabytes(sessionDefault.Value);
            }
            targetSizeBytes = (ulong)(targetSizeBytes * 0.97);
            base.RecalcFromTime(time);
        }
    }

    public static class PresetUtils
    {
        public static void PromptInstallPreset()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "reika Preset|*.reikapreset",
                Title = "reika: install preset",
                Multiselect = true,
            };
            openFileDialog.ShowDialog();
            var dir = AppData.GetAppDataSubdir("presets");
            foreach (string file in openFileDialog.FileNames)
            {
                try
                {
                    File.Copy(file, Path.Combine(dir, Path.GetFileName(file)), true);
                }
                catch (Exception ex)
                {
                    PopupOK.Show($"Failed to copy preset file {file}:\n {ex.Message}", "Error", PopupStyle.Error);
                }
            }
        }

        public static List<CreateFilePreset> LoadWindowsPresets()
        {
            var presets = new List<CreateFilePreset>();
            presets.Add(new CustomTargetSizePreset());
            return presets;
        }
    }
}
