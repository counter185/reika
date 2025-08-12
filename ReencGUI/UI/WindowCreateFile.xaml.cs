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

namespace ReencGUI.UI
{
    /// <summary>
    /// Logika interakcji dla klasy WindowCreateFile.xaml
    /// </summary>
    public partial class WindowCreateFile : Window
    {

        List<StreamTarget> streamTargets = new List<StreamTarget>();
        List<CreateFilePreset> presets = new List<CreateFilePreset>();

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
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            WindowUtil.SetWindowDarkMode(this);
        }

        void LoadPresets()
        {
            presets.Add(new Discord10MBPreset());

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
            Panel_Streams.Items.Clear();
            foreach (var stream in streamTargets)
            {
                Panel_Streams.Items.Add(new UIStreamEntry(stream));
            }
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
            MainWindow.instance.EnqueueEncodeOperation(ffmpegArgs, duration);
            Close();
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
    }
}
