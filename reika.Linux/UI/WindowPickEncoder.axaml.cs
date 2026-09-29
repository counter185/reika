using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using reika.Core;
using System.Collections.Generic;
using System.Linq;

namespace reika.Linux.UI
{
    public partial class WindowPickEncoder : Window
    {
        public string result = null;

        static LinearGradientBrush nvidiaGradient =
            new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0.5, RelativeUnit.Relative),
                EndPoint = new RelativePoint(1, 0.5, RelativeUnit.Relative),
                GradientStops = new()
                {
                    new GradientStop(Color.FromArgb(30, 0, 255, 0), 0),
                    new GradientStop(Color.FromArgb(0, 0, 255, 0), 1)
                }
            };

        static LinearGradientBrush amdGradient =
            new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0.5, RelativeUnit.Relative),
                EndPoint = new RelativePoint(1, 0.5, RelativeUnit.Relative),
                GradientStops = new()
                {
                    new GradientStop(Color.FromArgb(30, 255, 0, 0), 0),
                    new GradientStop(Color.FromArgb(0, 255, 0, 0), 1)
                }
            };

        static LinearGradientBrush intelGradient =
            new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0.5, RelativeUnit.Relative),
                EndPoint = new RelativePoint(1, 0.5, RelativeUnit.Relative),
                GradientStops = new()
                {
                    new GradientStop(Color.FromArgb(30, 0, 0x94, 255), 0),
                    new GradientStop(Color.FromArgb(0, 0, 0x94, 255), 1)
                }
            };

        static LinearGradientBrush vulkanGradient =
            new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0.5, RelativeUnit.Relative),
                EndPoint = new RelativePoint(1, 0.5, RelativeUnit.Relative),
                GradientStops = new()
                {
                    new GradientStop(Color.FromArgb(30, 214, 112, 0), 0),
                    new GradientStop(Color.FromArgb(0, 214, 112, 0), 1)
                }
            };

        static LinearGradientBrush defaultGradient =
            new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0.5, RelativeUnit.Relative),
                EndPoint = new RelativePoint(1, 0.5, RelativeUnit.Relative),
                GradientStops = new()
                {
                    new GradientStop(Color.FromArgb(30, 80, 80, 80), 0),
                    new GradientStop(Color.FromArgb(0, 80, 80, 80), 1)
                }
            };

        //todo move this somewhere

        public static Brush GetGradientForCodecID(string id)
            => id != null ?
                (id.Contains("nvenc") ? nvidiaGradient
                : id.Contains("amf") ? amdGradient
                : id.Contains("qsv") ? intelGradient
                : id.Contains("vulkan") ? vulkanGradient
                : defaultGradient)
            : defaultGradient;

        int GetPriorityForID(FFMPEG.CodecType type, string id)
        {
            //video
            if (type == FFMPEG.CodecType.Video)
            {
                List<KeyValuePair<string, int>> videoKeywordPriorities = new List<KeyValuePair<string, int>>()
            {
                new KeyValuePair<string, int>("copy", 10),
                new KeyValuePair<string, int>("av1", 6),
                new KeyValuePair<string, int>("hevc", 5),
                new KeyValuePair<string, int>("h265", 5),
                new KeyValuePair<string, int>("264", 4),
                new KeyValuePair<string, int>("h26", 3),
                new KeyValuePair<string, int>("x26", 3),
                new KeyValuePair<string, int>("vp", 2),
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
            var validEncs = (from x in FFMPEGCodecs.encoders
                             where x.Type == type
                             select x).OrderByDescending(x => GetPriorityForID(type, x.ID)).ToList();

            foreach (var enc in validEncs)
            {
                UIEncoderEntry entry = new UIEncoderEntry(Utils.SanitizeForXAML(enc.ID), Utils.SanitizeForXAML(enc.Name));
                entry.Background = GetGradientForCodecID(enc.ID);
                entry.PointerPressed += (s, e) =>
                {
                    result = enc.ID;
                    this.Close();
                };

                Panel_Encoders.Items.Add(entry);
            }
        }
    }
}