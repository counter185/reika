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
using System.Windows.Shapes;

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
            presets.Add(new Discord10MBPreset());
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

            Input_VcodecName.InputField.IsEnabled = streamTargets.Any(x => x.streamInfo.mediaType == FFMPEG.CodecType.Video);
            Input_Vbitrate.InputField.IsEnabled = streamTargets.Any(x => x.streamInfo.mediaType == FFMPEG.CodecType.Video);

            Input_AcodecName.InputField.IsEnabled = streamTargets.Any(x => x.streamInfo.mediaType == FFMPEG.CodecType.Audio);
            Input_Abitrate.InputField.IsEnabled = streamTargets.Any(x => x.streamInfo.mediaType == FFMPEG.CodecType.Audio);
        }

        public void AddStream(StreamTarget target)
        {
            streamTargets.Add(target);
            CreateStreamsList();
        }

        public ulong GetDuration()
        {
            //todo: -ss and -to
            return streamTargets.Select(x => Utils.LengthToMS(x.mediaInfo.dH, x.mediaInfo.dM, x.mediaInfo.dS, x.mediaInfo.dMS))
                .Max();
        }

        public void RunEncode()
        {
            if (streamTargets.Any())
            {
                ulong duration = GetDuration();
                List<string> args = MakeFFMPEGArgs();
                MainWindow.instance.EnqueueEncodeOperation(args, duration);
                Close();
            }
            else
            {
                MessageBox.Show("No streams to encode", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private List<string> MakeFFMPEGArgs()
        {
            string outputFileName = Input_OutFileName.InputField.Text;
            string vcodec = Input_VcodecName.InputField.Text;
            string vbitrate = Input_Vbitrate.InputField.Text;
            string acodec = Input_AcodecName.InputField.Text;
            string abitrate = Input_Abitrate.InputField.Text;

            string trimFrom = Input_TrimFrom.InputField.Text;
            string trimTo = Input_TrimTo.InputField.Text;

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
                    if (media != null)
                    {
                        WindowStreamSelect pickStreams = new WindowStreamSelect(file, media, media.streams);
                        pickStreams.ShowDialog();
                        foreach (var stm in pickStreams.selectedStreams)
                        {
                            AddStream(stm);
                        }
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
    }
}
