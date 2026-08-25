using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using reika.Core;
using reika.Linux.UI.Popup;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace reika.Linux.UI
{
    public partial class WindowYTDLPDownload : Window
    {
        MainWindow caller;
        YTDLP.YTDLPVideo currentVideo = null;
        List<CreateFilePreset> presets;

        public WindowYTDLPDownload(MainWindow caller)
        {
            this.caller = caller;
            InitializeComponent();
            Input_URL.InputField.Text = "";
            LoadPresets();

            Input_URL.InputField.TextChanged += (a, b) => URLChanged();

            Input_URL.InputField.TextChanged += (a, b) => UpdateFullArgsLabel();
            Input_ExtraArgs.InputField.TextChanged += (a, b) => UpdateFullArgsLabel();
            ListBox_FormatList.SelectionChanged += (a, b) => UpdateFullArgsLabel();

            SetMetadata(null);

            metaFetchThread = new Thread(MetadataFetchThread);
            metaFetchThread.Start();
        }

        protected override void OnClosed(EventArgs e)
        {
            metaFetchThread.Interrupt();
            base.OnClosed(e);
        }

        Thread metaFetchThread;
        string requestedURLNow = "";
        string metaURLNow = null;
        void MetadataFetchThread()
        {
            try
            {
                while (true)
                {
                    if (requestedURLNow != metaURLNow)
                    {
                        string nextURL = requestedURLNow;
                        Dispatcher.Invoke(() =>
                        {
                            Label_VideoTitle.Content = "<fetching media info...>";
                            ListBox_FormatList.Items.Clear();
                            Label_Channel.Content = Label_ID.Content = "";
                            UpdateFullArgsLabel();
                        });

                        currentVideo = YTDLP.GetVideoInfo(new List<string> { nextURL });
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
            catch (ThreadInterruptedException) { }
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
                autoPick.Label_VideoDetails.IsVisible = false;
                autoPick.Label_AudioDetails.IsVisible = false;
                autoPick.Label_Extension.Content = v.autoExt;

                UIYTDLPFormatEntry customPick = new UIYTDLPFormatEntry();
                customPick.SetCustomFormatField();

                RadioButton autoRB = new RadioButton();
                autoRB.Content = autoPick;
                autoRB.GroupName = "FormatSel";
                autoRB.VerticalContentAlignment = VerticalAlignment.Center;
                autoRB.IsChecked = true;
                autoRB.IsCheckedChanged += (a, b) => UpdateFullArgsLabel();
                ListBox_FormatList.Items.Add(autoRB);

                RadioButton customRB = new RadioButton();
                customRB.Content = customPick;
                customRB.GroupName = "FormatSel";
                customRB.VerticalContentAlignment = VerticalAlignment.Center;
                customRB.IsCheckedChanged += (a, b) => UpdateFullArgsLabel();
                ListBox_FormatList.Items.Add(customRB);
                customPick.idTextBox.TextChanged += (a, b) => { if (customRB.IsChecked == true) UpdateFullArgsLabel(); };

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
                    rb.IsCheckedChanged += (a, b) => UpdateFullArgsLabel();
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
            string url = Input_URL.InputField.Text ?? "";
            if (url.Contains("youtu.be") || url.Contains("youtube.com"))
            {
                /*string denoVersion = YTDLP.GetDenoVersion();
                if (String.IsNullOrEmpty(denoVersion))
                {
                    PopupResult result = PopupYesNoCancel.Show(
                    "yt-dlp may require a JavaScript runtime to download from this site, and Deno was not found on your system.\n"
                    + "Install it now with winget?\n\n"
                    + "*This will open a cmd window, where you may need to confirm the installation.",
                    "YouTube Download Warning",
                    PopupStyle.Warning);

                    if (result == PopupResult.Yes)
                    {
                        caller.EnqueueOtherOperation((entry) =>
                        {
                            Dispatcher.Invoke(() =>
                            {
                                entry.Label_Primary.Text = "Install Deno...";
                            });
                            try
                            {
                                WindowsExternalDownloads.DenoInstallLatest(entry);
                            }
                            catch (Exception ex)
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    PopupOK.Show($"Failed to install Deno:\n {ex.Message}", "Error", PopupStyle.Error);
                                });
                            }
                        });
                    }
                    if (result != PopupResult.Cancel)
                    {
                        EnqueueDownload();
                    }

                }
                else*/
                {
                    EnqueueDownload();
                }

            }
            else
            {
                EnqueueDownload();
            }
        }

        private void EnqueueDownload()
        {
            if (Input_URL.InputField.Text != "")
            {
                var args = MakeYTDLPArgs();
                bool reencodeAfterDownload = false;// Checkbox_RunReenc.IsChecked == true;
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
                            try
                            {
                                WindowQuickReencode.QueueReencodeWithPreset(outputFile, reencPreset, true);
                            }
                            catch (Exception ex)
                            {
                                PopupOK.Show($"Failed to process file:\n {ex.Message}", "Error", PopupStyle.Error);
                            }
                        });
                    }
                });
                /*if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                {
                    Close();
                }*/
                Close();
            }
            else
            {
                PopupOK.Show("URL cannot be empty.", "Error", PopupStyle.Error);
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

            string cookiesFromBrowser = Settings.settings.FromKey("reika.ytdlp.cookiesFromBrowser").GetString();
            if (cookiesFromBrowser != "")
            {
                args.Add("--cookies-from-browser");
                args.Add(cookiesFromBrowser);
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

            /*args.Add("--ffmpeg-location");
            args.Add("./ffmpeg");*/

            args.Add($"\"{Input_URL.InputField.Text}\"");

            return args;
        }

        private void Button_OutputFolderPick_Click(object sender, RoutedEventArgs e)
        {
            /*using (System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog())
            {
                fbd.Description = "Select output folder for downloads";
                fbd.SelectedPath = Input_OutputFolder.InputField.Text;
                fbd.ShowNewFolderButton = true;
                System.Windows.Forms.DialogResult result = fbd.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    Input_OutputFolder.InputField.Text = fbd.SelectedPath;
                }
            }*/
        }

        private void LoadPreset_Click(object sender, RoutedEventArgs e)
        {
            //PresetManager.PromptInstallPreset();
            LoadPresets();
        }
    }
}