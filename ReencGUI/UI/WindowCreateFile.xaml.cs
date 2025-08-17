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
                Input_AcodecName.InputField,
                Input_Abitrate.InputField,
                Input_OutFileName.InputField,
                Input_TrimFrom.InputField,
                Input_TrimTo.InputField,
                Input_OtherArgs.InputField,
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
                FFMPEG.MediaInfo targetMedia = streamTargets.Where(x => x.streamInfo.mediaType == FFMPEG.CodecType.Video).FirstOrDefault()?.mediaInfo;
                if (targetMedia != null && ValidateTimestamp(timestamp))
                {
                    FFMPEG.ExtractThumbnailAsync(targetMedia.fileName, timestamp, (uri)=>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            fetchingFromThumbnail = false;
                            fromThumbnailTimestampNow = timestamp;
                            try
                            {
                                Image_FromThumb.Source = Utils.LoadToMemFromUri(uri);
                                disposeUrisOnClose.Add(uri.LocalPath);
                            } catch (Exception ex)
                            {
                                Console.WriteLine($"Failed to load thumbnail for '{timestamp}': {ex.Message}");
                            }
                            FetchFromTimeThumbnail();
                        });
                    });
                }
            }
        }

        void FetchToTimeThumbnail()
        {
            string timestamp = Input_TrimTo.InputField.Text;
            if (!fetchingToThumbnail && toThumbnailTimestampNow != timestamp)
            {
                FFMPEG.MediaInfo targetMedia = streamTargets.Where(x => x.streamInfo.mediaType == FFMPEG.CodecType.Video).FirstOrDefault()?.mediaInfo;
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
            try
            {
                foreach (string file in Directory.GetFiles(AppData.GetAppDataSubdir("presets"), "*.reikapreset"))
                {
                    CreateFilePreset preset = CreateFilePreset.Load(file);
                    if (preset != null)
                    {
                        preset.name = System.IO.Path.GetFileNameWithoutExtension(file);
                        presets.Add(preset);
                    }
                    else
                    {
                        Console.WriteLine($"Failed to load preset: {file}");
                    }
                }
            } catch (Exception e)
            {
                Console.WriteLine($"Failed to load presets: {e.Message}");
            }

            presets.Add(new Discord10MBPreset());
            presets.Add(new CreateFilePreset
            {
                name = "H264: Moderate",
                vbitrate = "12000k",
                vcodecs = new List<string> { "hevc_nvenc", "hevc_amf", "libx265" },
                acodec = "copy",
                abitrate = ""
            });
            presets.Add(new CreateFilePreset
            {
                name = "H265: Quality",
                vbitrate = "12000k",
                vcodecs = new List<string> { "hevc_nvenc", "hevc_amf", "libx265" },
                acodec = "copy",
                abitrate = ""
            });
            presets.Add(new CreateFilePreset
            {
                name = "H265: Moderate",
                vbitrate = "8000k",
                vcodecs = new List<string> { "hevc_nvenc", "hevc_amf", "libx265" },
                acodec = "copy",
                abitrate = ""
            });
            presets.Add(new CreateFilePreset
            {
                name = "H265: File size",
                vbitrate = "4000k",
                vcodecs = new List<string> { "hevc_nvenc", "hevc_amf", "libx265" },
                acodec = "copy",
                abitrate = ""
            });
            presets.Add(new CreateFilePreset
            {
                name = "H266: 2mpbs",
                vbitrate = "2000k",
                vcodecs = new List<string> { "libvvenc" },
                acodec = "copy",
                abitrate = ""
            });
            presets.Add(new CreateFilePreset
            {
                name = "VP9 YouTube quality",
                vbitrate = "2000k",
                vcodecs = new List<string> { "vp9_qsv", "libvpx-vp9", "vp9" },
                acodec = "libopus",
                abitrate = ""
            });
            /*presets.Add(new CreateFilePreset
            {
                name = "PSP",
                vbitrate = "1000k",
                vcodecs = new List<string> { "h264_nvenc", "h264_amf", "libx264" },
                acodec = "aac",
                abitrate = "128k",
                otherArgs = "-profile:v main -vf \"scale=480:272,setsar=1:1\""
            });*/

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
            Input_VcodecName.InputField.Text = (from x in preset.vcodecs
                                                where MainWindow.instance.encoders.Any(y=>y.ID == x)
                                                select x).First();
            Input_Vbitrate.InputField.Text = preset.vbitrate;
            Input_AcodecName.InputField.Text = preset.acodec;
            Input_Abitrate.InputField.Text = preset.abitrate;
            Input_OtherArgs.InputField.Text = preset.otherArgs;
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

            videoAvailable = streamTargets.Any(x => x.streamInfo.mediaType == FFMPEG.CodecType.Video);
            Input_VcodecName.InputField.IsEnabled = videoAvailable;
            Input_Vbitrate.InputField.IsEnabled = videoAvailable;

            audioAvailable = streamTargets.Any(x => x.streamInfo.mediaType == FFMPEG.CodecType.Audio);
            Input_AcodecName.InputField.IsEnabled = audioAvailable;
            Input_Abitrate.InputField.IsEnabled = audioAvailable;

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
                MainWindow.instance.EnqueueEncodeOperation(args, duration, onFinishAction);
                Close();
            }
            else
            {
                MessageBox.Show("No streams to encode", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        CreateFilePreset PresetFromCurrentData()
        {
            string vcodec = Input_VcodecName.InputField.Text;
            string vbitrate = Input_Vbitrate.InputField.Text;
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
                acodec = acodec,
                abitrate = abitrate,
                otherArgs = otherArgs
            };
            return preset;
        }

        private List<string> MakeFFMPEGArgs()
        {
            string outputFileName = Input_OutFileName.InputField.Text;

            string vcodec = "";
            string vbitrate = "";
            if (videoAvailable)
            {
                vcodec = Input_VcodecName.InputField.Text;
                vbitrate = Input_Vbitrate.InputField.Text;
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
            if (trimFrom != "")
            {
                ffmpegArgs.Add("-ss");
                ffmpegArgs.Add(trimFrom);
            }
            if (trimTo != "")
            {
                ffmpegArgs.Add("-to");
                ffmpegArgs.Add(trimTo);
            }
            if (otherArgs != "")
            {
                ffmpegArgs.Add(otherArgs);
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
                Filter = "Video Files|*.mp4;*.mkv;*.avi;*.mov;*.flv;*.webm;*.wmv|All Files|*.*",
                Title = "reika: save output file",
                OverwritePrompt = true,
            };
            saveFileDialog.ShowDialog();
            if (!string.IsNullOrEmpty(saveFileDialog.FileName))
            {
                Input_OutFileName.InputField.Text = saveFileDialog.FileName;
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
    }
}
