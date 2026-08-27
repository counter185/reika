using Avalonia.Controls;
using Avalonia.Platform.Storage;
using reika.Core;
using reika.Linux.UI.Popup;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace reika.Linux
{
    public static class PresetUtils
    {
        //this needs to be async on linux
        public async static Task<string> PromptInstallPreset(Window parent)
        {
            var filePickerResult = await parent.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "reika: install preset",
                AllowMultiple = true,
                FileTypeFilter = new[] { 
                    new FilePickerFileType("reika Preset") { Patterns = new[] { "*.reikapreset" } }
                }

            });

            var dir = AppData.GetAppDataSubdir("presets");
            string ret = null;
            foreach (string file in filePickerResult.Select(x=>x.Path.LocalPath))
            {
                if (ret == null)
                {
                    ret = file;
                }
                try
                {
                    File.Copy(file, Path.Combine(dir, Path.GetFileName(file)), true);
                }
                catch (Exception ex)
                {
                    PopupOK.Show($"Failed to copy preset file {file}:\n {ex.Message}", "Error", PopupStyle.Error);
                }
            }
            return ret;
        }
    }
}
