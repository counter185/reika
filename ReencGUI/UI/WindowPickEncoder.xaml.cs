using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
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
    /// Logika interakcji dla klasy WindowPickEncoder.xaml
    /// </summary>
    public partial class WindowPickEncoder : Window
    {
        public string result = null;

        static LinearGradientBrush nvidiaGradient = new LinearGradientBrush(
            Color.FromArgb(30, 0, 255, 0), 
            Color.FromArgb(0, 0, 255, 0), 
            new Point(0, 0.5), 
            new Point(1, 0.5));

        static LinearGradientBrush amdGradient = new LinearGradientBrush(
            Color.FromArgb(30, 255, 0, 0), 
            Color.FromArgb(0, 255, 0, 0), 
            new Point(0, 0.5), 
            new Point(1, 0.5));

        static LinearGradientBrush intelGradient = new LinearGradientBrush(
            Color.FromArgb(30, 0, 0x94, 255), 
            Color.FromArgb(0, 0, 0x94, 255), 
            new Point(0, 0.5), 
            new Point(1, 0.5));

        //todo move this somewhere
        public static Brush GetGradientForCodecID(string id)
        {
            if (id != null)
            {
                if (id.Contains("nvenc"))
                {
                    return nvidiaGradient;
                }
                if (id.Contains("amf"))
                {
                    return amdGradient;
                }
                if (id.Contains("qsv"))
                {
                    return intelGradient;
                }
            }

            return Brushes.Transparent;
        }

        int GetPriorityForID(FFMPEG.CodecType type, string id)
        {
            //video
            if (type == FFMPEG.CodecType.Video)
            {
                List<KeyValuePair<string, int>> videoKeywordPriorities = new List<KeyValuePair<string, int>>()
            {
                new KeyValuePair<string, int>("copy", 10),
                new KeyValuePair<string, int>("hevc", 5),
                new KeyValuePair<string, int>("h265", 5),
                new KeyValuePair<string, int>("264", 4),
                new KeyValuePair<string, int>("h26", 3),
                new KeyValuePair<string, int>("x26", 3),
                new KeyValuePair<string, int>("vp", 2),
                new KeyValuePair<string, int>("av1", 1),
            };
                foreach (var kvp in videoKeywordPriorities)
                {
                    if (id.Contains(kvp.Key))
                    {
                        return kvp.Value;
                    }
                }
            }

            //audio
            if (type == FFMPEG.CodecType.Audio)
            {
                List<KeyValuePair<string, int>> audioKeywordPriorities = new List<KeyValuePair<string, int>>()
            {
                new KeyValuePair<string, int>("copy", 10),
                new KeyValuePair<string, int>("opus", 4),
                new KeyValuePair<string, int>("flac", 3),
                new KeyValuePair<string, int>("mp3", 2),
                new KeyValuePair<string, int>("aac", 2),
                new KeyValuePair<string, int>("vorbis", 1),
            };
                foreach (var kvp in audioKeywordPriorities)
                {
                    if (id.Contains(kvp.Key))
                    {
                        return kvp.Value;
                    }
                }
            }

            return 0;
        }

        public WindowPickEncoder(FFMPEG.CodecType type)
        {
            InitializeComponent();
            var validEncs = (from x in MainWindow.instance.encoders
                             where x.Type == type
                             select x).OrderByDescending(x=>GetPriorityForID(type, x.ID)).ToList();

            foreach (var enc in validEncs)
            {
                UIEncoderEntry entry = new UIEncoderEntry(Utils.SanitizeForXAML(enc.ID), Utils.SanitizeForXAML(enc.Name));
                entry.Background = GetGradientForCodecID(enc.ID);
                entry.MouseDoubleClick += (s, e) =>
                {
                    result = enc.ID;
                    this.Close();
                };

                Panel_Encoders.Items.Add(entry);
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            WindowUtil.SetWindowDarkMode(this);
        }
    }
}
