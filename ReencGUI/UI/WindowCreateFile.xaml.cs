using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
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

namespace ReencGUI.UI
{
    /// <summary>
    /// Logika interakcji dla klasy WindowCreateFile.xaml
    /// </summary>
    public partial class WindowCreateFile : Window
    {

        List<StreamTarget> streamTargets = new List<StreamTarget>();
        List<UIStreamEntry> streamEntries = new List<UIStreamEntry>();
        List<CreateFilePreset> presets = new List<CreateFilePreset>();

        string fromThumbnailTimestampNow = null;
        volatile bool fetchingFromThumbnail = false;
        
        string toThumbnailTimestampNow = null;
        volatile bool fetchingToThumbnail = false;

        List<string> disposeUrisOnClose = new List<string>();

        bool videoAvailable = false;
        bool audioAvailable = false;

        public WindowCreateFile()
        {
            InitializeComponent();
            LoadPresets();
        }
        public WindowCreateFile(IEnumerable<StreamTarget> streams) : this()
        {
            foreach (var stream in streams)
            {
                AddStream(stream);
            }

            UpdateCommandLabel();
            Control[] updateLogOnChange = new Control[]
            {
                Input_VcodecName.InputField,
                Input_Vbitrate.InputField,
                Input_Vres.InputField,
                Input_AcodecName.InputField,
                Input_Abitrate.InputField,
                Input_OutFileName.InputField,
                Input_TrimFrom.InputField,
                Input_TrimTo.InputField,
                Input_OtherArgs.InputField,
                Input_Crop.InputField,
                Tbox_Extension,
                Combo_Preset,
                Combo_VbitrateUnits,
                Combo_AbitrateUnits
            };

            Input_TrimFrom.InputField.TextChanged += (s, e) =>
            {
                Image_FromThumb.Source = null;
                FetchFromTimeThumbnail();
            };
            Input_TrimTo.InputField.TextChanged += (s, e) =>
            {
                Image_ToThumb.Source = null;   
                FetchToTimeThumbnail();
            };

            Input_Vbitrate.InputField.TextChanged += (s, e) => EstimateFileSize();
            Input_Abitrate.InputField.TextChanged += (s, e) => EstimateFileSize();
            Input_TrimFrom.InputField.TextChanged += (s, e) => EstimateFileSize();
            Input_TrimTo.InputField.TextChanged += (s, e) => EstimateFileSize();

            foreach (Control c in updateLogOnChange)
            {
                if (c is TextBox textBox)
                {
                    textBox.TextChanged += (s, e) => UpdateCommandLabel();
                }
                else if (c is ComboBox comboBox)
                {
                    comboBox.SelectionChanged += (s, e) => UpdateCommandLabel();
                }
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            WindowUtil.SetWindowDarkMode(this);
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            AddCurrentStreamListThumbnailsToDeletionTargets();
            GC.Collect();
            foreach (string uri in disposeUrisOnClose)
            {
                FFMPEG.ManualDeleteThumbnail(uri);
            }
        }

        void AddCurrentStreamListThumbnailsToDeletionTargets()
        {
            foreach (UIStreamEntry prevEntry in streamEntries)
            {
                if (prevEntry.thumbnailUri != null)
                {
                    disposeUrisOnClose.Add(prevEntry.thumbnailUri.LocalPath);
                }
            }
        }

        //todo clean these two methods
        void FetchFromTimeThumbnail()
        {
            string timestamp = Input_TrimFrom.InputField.Text;
            if (!fetchingFromThumbnail && fromThumbnailTimestampNow != timestamp)
            {
                FFMPEG.MediaInfo targetMedia = GetPreviewVideoMedia();
                if (targetMedia != null && ValidateTimestamp(timestamp))
                {
                    FFMPEG.ExtractThumbnailAsync(targetMedia.fileName, timestamp, (uri) =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            fetchingFromThumbnail = false;
                            fromThumbnailTimestampNow = timestamp;
                            try
                            {
                                Image_FromThumb.Source = Utils.LoadToMemFromUri(uri);
                                disposeUrisOnClose.Add(uri.LocalPath);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Failed to load thumbnail for '{timestamp}': {ex.Message}");
                            }
                            FetchFromTimeThumbnail();
                        });
                    });
                }
            }
        }

        public FFMPEG.MediaInfo GetPreviewVideoMedia()
        {
            return streamTargets.Where(x => x.streamInfo.mediaType == FFMPEG.CodecType.Video).FirstOrDefault()?.mediaInfo;
        }

        void FetchToTimeThumbnail()
        {
            string timestamp = Input_TrimTo.InputField.Text;
            if (!fetchingToThumbnail && toThumbnailTimestampNow != timestamp)
            {
                FFMPEG.MediaInfo targetMedia = GetPreviewVideoMedia();
                if (targetMedia != null && ValidateTimestamp(timestamp))
                {
                    FFMPEG.ExtractThumbnailAsync(targetMedia.fileName, timestamp, (uri)=>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            fetchingToThumbnail = false;
                            toThumbnailTimestampNow = timestamp;
                            try
                            {
                                Image_ToThumb.Source = Utils.LoadToMemFromUri(uri);
                                disposeUrisOnClose.Add(uri.LocalPath);
                            } catch (Exception ex)
                            {
                                Console.WriteLine($"Failed to load thumbnail for '{timestamp}': {ex.Message}");
                            }
                            FetchToTimeThumbnail();
                        });
                    });
                }
            }
        }

        bool ValidateTimestamp(string timestamp)
        {
            return Regex.IsMatch(timestamp, @"^(?:\d{2}:)?(?:\d{2}:)?(\d{2})(\.\d{1,3})?$");
        }

        void LoadPresets()
        {
            presets = PresetManager.LoadPresets();

            foreach (var preset in presets)
            {
                Combo_Preset.Items.Add(preset.name);
            }
        }

        void ApplyPreset(CreateFilePreset preset)
        {
            if (preset is DynamicCreateFilePreset dynamicPreset)
            {
                dynamicPreset.Recalculate(this);
            }
            Tbox_Extension.Text = preset.requiredExtension ?? Tbox_Extension.Text;
            Input_VcodecName.InputField.Text = (from x in preset.vcodecs
                                                where MainWindow.instance.encoders.Any(y=>y.ID == x)
                                                select x).FirstOrDefault() ?? Input_VcodecName.InputField.Text;
            Input_Vbitrate.InputField.Text = preset.vbitrate;
            Input_Vres.InputField.Text = preset.vresolution ?? Input_Vres.InputField.Text;
            Input_Crop.InputField.Text = preset.cropString ?? Input_Crop.InputField.Text;
            Input_AcodecName.InputField.Text = preset.acodec;
            Input_Abitrate.InputField.Text = preset.abitrate;
            Input_OtherArgs.InputField.Text = preset.otherArgs ?? Input_OtherArgs.InputField.Text;
        }

        void CreateStreamsList()
        {
            AddCurrentStreamListThumbnailsToDeletionTargets();
            streamEntries.Clear();
            Panel_Streams.Items.Clear();
            foreach (var stream in streamTargets)
            {
                UIStreamEntry streamEntry = new UIStreamEntry(stream);
                streamEntry.MouseRightButtonDown += (s, e) =>
                {
                    streamTargets.Remove(stream);
                    CreateStreamsList();
                };
                Panel_Streams.Items.Add(streamEntry);
                streamEntries.Add(streamEntry);
            }

            Control[] videoControls = new Control[]
            {
                Input_VcodecName.InputField,
                Input_Vbitrate.InputField,
                Input_TrimFrom.InputField,
                Input_TrimTo.InputField,
                Input_Vres.InputField,
                Input_Crop.InputField,
                Button_VcodecSelect,
                Button_Crop
            };
            Control[] audioControls = new Control[]
            {
                Input_AcodecName.InputField,
                Input_Abitrate.InputField,
                Button_AcodecSelect
            };

            videoAvailable = streamTargets.Any(x => x.streamInfo.mediaType == FFMPEG.CodecType.Video);
            foreach (var vc in videoControls)
            {
                vc.IsEnabled = videoAvailable;
            }

            audioAvailable = streamTargets.Any(x => x.streamInfo.mediaType == FFMPEG.CodecType.Audio);
            foreach (var ac in audioControls)
            {
                ac.IsEnabled = audioAvailable;
            }

            EstimateFileSize();
        }

        void EstimateFileSize()
        {
            ulong durationMS = GetDuration();
            double durationS = durationMS / 1000.0;
            ulong vbitrate = 12000000;//safe default estimate 12mbps
            try
            {
                vbitrate = Utils.ParseBitrate(Input_Vbitrate.InputField.Text);
            }
            catch (Exception) { }

            ulong abitrate = 128000; //safe default estimate 128kbps
            try
            {
                abitrate = Utils.ParseBitrate(Input_Abitrate.InputField.Text);
            }
            catch (Exception) { }

            ulong videoSize = (ulong)(videoAvailable ? vbitrate * durationS / 8 : 0);
            ulong audioSize = (ulong)(audioAvailable ? abitrate * durationS / 8 : 0);

            ulong estimatedFileSize = videoSize + audioSize;

            Label_EstFileSize.Content = $"Estimated file size: {Utils.ByteCountToFriendlyString(estimatedFileSize)}" +
                $"\n({Utils.ByteCountToFriendlyString(videoSize)} video, {Utils.ByteCountToFriendlyString(audioSize)} audio)";
        }
        public void AddStream(StreamTarget target)
        {
            streamTargets.Add(target);
            CreateStreamsList();
        }

        public ulong GetDuration()
        {
            if (!streamTargets.Any())
            {
                return 0;
            }

            ulong wholeDuration = streamTargets.Select(x => Utils.LengthToMS(x.mediaInfo.dH, x.mediaInfo.dM, x.mediaInfo.dS, x.mediaInfo.dMS))
                .Max();

            ulong ret = wholeDuration;

            try
            {
                ulong ss = Utils.ParseDuration(Input_TrimFrom.InputField.Text);
                ret -= ss;
            }
            catch (Exception) { }   //invalid -ss

            try
            {
                ulong to = Utils.ParseDuration(Input_TrimTo.InputField.Text);
                if (to <= wholeDuration)
                {
                    ret -= (wholeDuration - to);
                }
            }
            catch (Exception) { }   //invalid -to

            return ret;
        }

        public void RunEncode()
        {
            if (streamTargets.Any())
            {
                ulong duration = GetDuration();
                List<string> args = MakeFFMPEGArgs();
                Action<UIFFMPEGOperationEntry, int> onFinishAction = null;
                if (Check_DeleteMedia.IsChecked == true)
                {
                    onFinishAction = (ui, exit) =>
                    {
                        var files = streamTargets.Select(x => x.mediaInfo.fileName).Distinct().ToList();
                        foreach (var file in files)
                        {
                            try
                            {
                                File.Delete(file);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Failed to delete file {file}: {ex.Message}");
                            }
                        }
                    };
                }
                string encoderID = Input_VcodecName.InputField.IsEnabled ? Input_VcodecName.InputField.Text : Input_AcodecName.InputField.Text;
                //todo get rid of args.last()
                MainWindow.instance.EnqueueEncodeOperation(args, duration, encoderID, args.Last().Trim('\"'), onFinishAction);
            }
            else
            {
                MessageBox.Show("No streams to encode", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void RunPreview()
        {
            var ffplayArgs = MakeFFPlayPreviewArgs();
            ffplayArgs = new string[] { "/c", FFMPEG.GetCommandPath("ffmpeg") }.Concat(ffplayArgs).ToList();
            FFMPEG.RunCommandWithAsyncOutput("cmd", ffplayArgs, (line) => {
                //Console.WriteLine(line);
            });
        }

        CreateFilePreset PresetFromCurrentData()
        {
            string requiredExtension = Tbox_Extension.Text;

            string vcodec = Input_VcodecName.InputField.Text;
            string vbitrate = Input_Vbitrate.InputField.Text;
            string vresolution = Input_Vres.InputField.Text;
            string vcrop = Input_Crop.InputField.Text;
            string acodec = Input_AcodecName.InputField.Text;
            string abitrate = Input_Abitrate.InputField.Text;

            string trimFrom = Input_TrimFrom.InputField.Text;
            string trimTo = Input_TrimTo.InputField.Text;

            string otherArgs = Input_OtherArgs.InputField.Text;
            CreateFilePreset preset = new CreateFilePreset
            {
                name = "Custom preset",
                vcodecs = new List<string> { vcodec },
                vbitrate = vbitrate,
                vresolution = vresolution,
                acodec = acodec,
                abitrate = abitrate,
                otherArgs = otherArgs,
                requiredExtension = requiredExtension,
                cropString = vcrop
            };
            return preset;
        }

        private List<string> MakeFFPlayPreviewArgs()
        {
            string trimFrom = Input_TrimFrom.InputField.Text;
            string trimTo = Input_TrimTo.InputField.Text;

            string vbitrate = "";//Input_Vbitrate.InputField.Text;

            string vresolution = Input_Vres.InputField.Text;
            string cropString = Input_Crop.InputField.Text;

            var distinctFiles = streamTargets.Select(x => x.mediaInfo.fileName).Distinct().ToList();
            Dictionary<string, int> fileIndexMap = new Dictionary<string, int>();

            List<string> vfArgs = new List<string>();

            List<string> ret = new List<string>();

            int i = 0;
            //add -i for all files
            foreach (var fileName in distinctFiles)
            {
                ret.Add("-i");
                ret.Add($"\"{fileName}\"");
                fileIndexMap[fileName] = i++;
            }

            //-map streams
            foreach (var stream in streamTargets)
            {
                ret.Add($"-map");
                ret.Add($"{fileIndexMap[stream.mediaInfo.fileName]}:{stream.indexInStream}");
            }

            bool onlyOneMediaFileAndIsMP4 = distinctFiles.Count == 1 && distinctFiles[0].ToLower().EndsWith(".mp4");

            if (cropString != "")
            {
                vfArgs.Add($"crop={cropString}");
            }
            if (vresolution != "")
            {
                try
                {
                    var dimensions = Regex.Match(vresolution, @"^(\d+)(?:x|:)(\d+)$").Groups.OfType<Group>().Skip(1).Select(g => int.Parse(g.Value)).ToList();
                    vfArgs.Add($"scale={dimensions[0]}:{dimensions[1]}");
                    vfArgs.Add("setsar=1");
                }
                catch (Exception) { }
            }

            if (trimFrom != "")
            {
                var ssArg = new string[] { "-ss", trimFrom };
                int insertAt = onlyOneMediaFileAndIsMP4 ? 0 : ret.Count;
                ret.InsertRange(insertAt, ssArg);
            }
            if (trimTo != "")
            {
                var toArg = new string[] { "-to", trimTo };
                int insertAt = onlyOneMediaFileAndIsMP4 ? 0 : ret.Count;
                ret.InsertRange(insertAt, toArg);
            }

            var vcodecs = MainWindow.instance.encoders.Where(x => x.Type == FFMPEG.CodecType.Video && x.ID.Contains("264"))
                .OrderByDescending(x=>new string[] { "nvenc", "amf", "qsv", "mf" }.Any(y=>x.ID.Contains(y)) ? 1 : 0);

            if (vbitrate != "")
            {
                ret.Add("-b:v");
                ret.Add(vbitrate);
            } else
            {
                //ret.AddRange(new string[] { "-crf", "20" });
                ret.AddRange(new string[] { "-b:v", "20000k" });
            }

            ret.AddRange(new string[] { "-c:v", vcodecs.First().ID });

            if (vfArgs.Any())
            {
                ret.Add("-vf");
                ret.Add($"\"{string.Join(",", vfArgs)}\"");
            }

            ret.AddRange(new string[] { "-f", "avi" });
            ret.Add("-");

            ret.AddRange(new string[] { "|", FFMPEG.GetCommandPath("ffplay"), "-autoexit", "-" });

            return ret;
        }

        private List<string> MakeFFMPEGArgs()
        {
            string outputFileName = Input_OutFileName.InputField.Text + Tbox_Extension.Text;

            List<string> vfArgs = new List<string>();

            string vcodec = "";
            string vbitrate = "";
            string vresolution = "";
            if (videoAvailable)
            {
                vcodec = Input_VcodecName.InputField.Text;
                vbitrate = Input_Vbitrate.InputField.Text;
                vresolution = Input_Vres.InputField.Text;
                if (!Regex.IsMatch(vresolution, @"^\d+(?:x|:)\d+$"))
                {
                    vresolution = "";
                }
            }

            string acodec = "";
            string abitrate = "";
            if (audioAvailable)
            {
                acodec = Input_AcodecName.InputField.Text;
                abitrate = Input_Abitrate.InputField.Text;
            }

            string trimFrom = Input_TrimFrom.InputField.Text;
            string trimTo = Input_TrimTo.InputField.Text;

            string cropString = Input_Crop.InputField.Text;

            string otherArgs = Input_OtherArgs.InputField.Text;

            List<string> ffmpegArgs = new List<string>();
            var distinctFiles = streamTargets.Select(x => x.mediaInfo.fileName).Distinct().ToList();
            Dictionary<string, int> fileIndexMap = new Dictionary<string, int>();

            ulong duration = GetDuration();
            ffmpegArgs.Add("-y");

            int i = 0;
            //add -i for all files
            foreach (var fileName in distinctFiles)
            {
                ffmpegArgs.Add("-i");
                ffmpegArgs.Add($"\"{fileName}\"");
                fileIndexMap[fileName] = i++;
            }

            //-map streams
            foreach (var stream in streamTargets)
            {
                ffmpegArgs.Add($"-map");
                ffmpegArgs.Add($"{fileIndexMap[stream.mediaInfo.fileName]}:{stream.indexInStream}");
            }

            if (vcodec != "")
            {
                ffmpegArgs.Add("-c:v");
                ffmpegArgs.Add(vcodec);
            }
            if (vbitrate != "")
            {
                ffmpegArgs.Add("-b:v");
                ffmpegArgs.Add(vbitrate);
            }
            if (cropString != "")
            {
                vfArgs.Add($"crop={cropString}");
            }
            if (vresolution != "")
            {
                try
                {
                    var dimensions = Regex.Match(vresolution, @"^(\d+)(?:x|:)(\d+)$").Groups.OfType<Group>().Skip(1).Select(g => int.Parse(g.Value)).ToList();
                    vfArgs.Add($"scale={dimensions[0]}:{dimensions[1]}");
                    vfArgs.Add("setsar=1");
                }
                catch (Exception) { }
            }
            if (acodec != "")
            {
                ffmpegArgs.Add("-c:a");
                ffmpegArgs.Add(acodec);
            }
            if (abitrate != "")
            {
                ffmpegArgs.Add("-b:a");
                ffmpegArgs.Add(abitrate);
            }

            bool onlyOneMediaFileAndIsMP4 = distinctFiles.Count == 1 && distinctFiles[0].ToLower().EndsWith(".mp4");

            if (trimFrom != "")
            {
                var ssArg = new string[] { "-ss", trimFrom };
                int insertAt = onlyOneMediaFileAndIsMP4 ? 0 : ffmpegArgs.Count;
                ffmpegArgs.InsertRange(insertAt, ssArg);
            }
            if (trimTo != "")
            {
                var toArg = new string[] { "-to", trimTo };
                int insertAt = onlyOneMediaFileAndIsMP4 ? 0 : ffmpegArgs.Count;
                ffmpegArgs.InsertRange(insertAt, toArg);
            }

            if (outputFileName.EndsWith(".mp4"))
            {
                ffmpegArgs.Add("-metadata:g");
                ffmpegArgs.Add("encoding_tool=reika");
            }

            if (otherArgs != "")
            {
                string regexVFArgs = @"-vf\s+(?:(?:([^""=]+=[^\s""]+))|(?:""([^=]+=[^""]+)""))\s*";
                Match otherVFArgs = Regex.Match(otherArgs, regexVFArgs);
                while (otherVFArgs.Success)
                {
                    vfArgs.Add(otherVFArgs.Groups[1].Value);
                    otherVFArgs = otherVFArgs.NextMatch();
                }
                otherArgs = Regex.Replace(otherArgs, regexVFArgs, "").Trim();

                ffmpegArgs.Add(otherArgs);
            }

            if (vfArgs.Any())
            {
                ffmpegArgs.Add("-vf");
                ffmpegArgs.Add($"\"{string.Join(",", vfArgs)}\"");
            }

            ffmpegArgs.Add($"\"{outputFileName}\"");
            return ffmpegArgs;
        }

        void UpdateCommandLabel()
        {
            Label_FullCommand.Text = "ffmpeg " + string.Join(" ", MakeFFMPEGArgs());
        }

        private void Button_Start_Click(object sender, RoutedEventArgs e)
        {
            RunEncode();
            if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                Close();
            }
        }

        private void Combo_Preset_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int index = Combo_Preset.SelectedIndex;

            if (index >= 0 && index < presets.Count)
            {
                ApplyPreset(presets[index]);
            }
        }

        private void Button_VcodecSelect_Click(object sender, RoutedEventArgs e)
        {
            WindowPickEncoder pickEnc = new WindowPickEncoder(FFMPEG.CodecType.Video);
            pickEnc.ShowDialog();
            if (pickEnc.result != null)
            {
                Input_VcodecName.InputField.Text = pickEnc.result;
            }
        }

        private void Button_AcodecSelect_Click(object sender, RoutedEventArgs e)
        {
            WindowPickEncoder pickEnc = new WindowPickEncoder(FFMPEG.CodecType.Audio);
            pickEnc.ShowDialog();
            if (pickEnc.result != null)
            {
                Input_AcodecName.InputField.Text = pickEnc.result;
            }
        }

        private void Panel_Streams_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (string file in files)
                {
                    FFMPEG.MediaInfo media = FFMPEG.GetMediaInfoForFile(file);
                    if (media != null && media.streams.Count != 0)
                    {
                        if (media.streams.Count == 1)
                        {
                            AddStream(new StreamTarget
                            {
                                mediaInfo = media,
                                streamInfo = media.streams[0],
                                indexInStream = 0
                            });
                        }
                        else
                        {
                            WindowStreamSelect pickStreams = new WindowStreamSelect(file, media, media.streams);
                            pickStreams.ShowDialog();
                            if (pickStreams.selectedStreams != null)
                            {
                                foreach (var stm in pickStreams.selectedStreams)
                                {
                                    AddStream(stm);
                                }
                            }
                        }
                    } else
                    {
                        MessageBox.Show($"Failed to get media info for file: {file}\nThe file might not be a valid media file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void Button_OutFileSelect_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                FileName = Input_OutFileName.InputField.Text + Tbox_Extension.Text,
                Filter = "Video Files|*.mp4;*.mkv;*.avi;*.mov;*.flv;*.webm;*.wmv|All Files|*.*",
                Title = "reika: save output file",
                OverwritePrompt = true,
            };
            saveFileDialog.ShowDialog();
            if (!string.IsNullOrEmpty(saveFileDialog.FileName))
            {
                string extension = Path.GetExtension(saveFileDialog.FileName);
                Input_OutFileName.InputField.Text = saveFileDialog.FileName.Substring(0, saveFileDialog.FileName.Length - extension.Length);
                Tbox_Extension.Text = extension;
            }
        }

        private void Button_SavePreset_Click(object sender, RoutedEventArgs e)
        {
            CreateFilePreset preset = PresetFromCurrentData();
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "reika Preset|*.reikapreset",
                Title = "reika: save preset",
                OverwritePrompt = true,
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    preset.Save(saveFileDialog.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to save preset: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

        }

        private void Button_LoadPreset_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "reika Preset|*.reikapreset",
                Title = "reika: load preset",
                Multiselect = false,
            };
            openFileDialog.ShowDialog();
            if (!string.IsNullOrEmpty(openFileDialog.FileName))
            {
                CreateFilePreset preset = CreateFilePreset.Load(openFileDialog.FileName);
                if (preset != null)
                {
                    ApplyPreset(preset);
                }
                else
                {
                    MessageBox.Show("Failed to load preset.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Button_Preview_Click(object sender, RoutedEventArgs e)
        {
            RunPreview();
        }

        private void Button_Crop_Click(object sender, RoutedEventArgs e)
        {
            new WindowSetCrop(this).ShowDialog();
        }
    }
}
