using System;
using System.Collections.Generic;
using System.Linq;
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

        LinearGradientBrush nvidiaGradient = new LinearGradientBrush(
            Color.FromArgb(80, 0, 255, 0), 
            Color.FromArgb(0, 0, 255, 0), 
            new Point(0, 0.5), 
            new Point(1, 0.5));

        LinearGradientBrush amdGradient = new LinearGradientBrush(
            Color.FromArgb(80, 255, 0, 0), 
            Color.FromArgb(0, 255, 0, 0), 
            new Point(0, 0.5), 
            new Point(1, 0.5));

        LinearGradientBrush intelGradient = new LinearGradientBrush(
            Color.FromArgb(80, 0, 0x94, 255), 
            Color.FromArgb(0, 0, 0x94, 255), 
            new Point(0, 0.5), 
            new Point(1, 0.5));

        Brush GetGradientForCodecID(string id)
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

            return null;
        }

        int GetPriorityForID(string id)
        {
            if (id.Contains("hevc") || id.Contains("265"))
            {
                return 4;
            }
            if (id.Contains("264"))
            {
                return 3;
            }
            if (id.Contains("vp"))
            {
                return 2;
            }
            if (id.Contains("av1"))
            {
                return 1;
            }

            return 0;
        }

        public WindowPickEncoder(FFMPEG.CodecType type)
        {
            InitializeComponent();
            var validEncs = (from x in MainWindow.instance.encoders
                             where x.Type == type
                             select x).OrderByDescending(x=>GetPriorityForID(x.ID)).ToList();

            foreach (var enc in validEncs)
            {
                UIEncoderEntry entry = new UIEncoderEntry();
                entry.Text_Primary.Content = Utils.SanitizeForXAML(enc.ID);
                entry.Text_Secondary.Content = Utils.SanitizeForXAML(enc.Name);
                entry.Background = GetGradientForCodecID(enc.ID);
                entry.MouseDoubleClick += (s, e) =>
                {
                    result = enc.ID;
                    this.Close();
                };

                Panel_Encoders.Items.Add(entry);
            }
        }
    }
}
