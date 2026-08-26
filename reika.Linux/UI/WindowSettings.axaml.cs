using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Microsoft.Win32;
using reika.Core;
using reika.Linux.UI.Popup;
using System;
using System.ComponentModel;

namespace reika.Linux.UI
{
    public partial class WindowSettings : Window
    {
        public WindowSettings()
        {
            InitializeComponent();
        }

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            SaveSettings();
            base.OnClosing(e);
        }

        private void SaveSettings()
        {
            Settings.settings.Save();
        }

        private void Button_OpenAppdataFolder_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(OperatingSystem.IsWindows() ? "explorer.exe" : "xdg-open", AppData.GetAppDataPath());
        }

        /*private void Button_AddPresetToList_Click(object sender, RoutedEventArgs e)
        {
            PresetUtils.PromptInstallPreset();
        }*/

        private void Button_ForceDLFFMPEG_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.instance.StartFFMPEGDownload(false);
        }

        private void Button_ForceDLYTDLP_Click(object? sender, RoutedEventArgs e)
        {
            MainWindow.instance.StartYTDLPDownload();
        }
    }
}