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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ReencGUI.UI
{
    /// <summary>
    /// Logika interakcji dla klasy UIYTDLPFormatEntry.xaml
    /// </summary>
    public partial class UIYTDLPFormatEntry : UserControl
    {
        private string _formatID = null;
        public string formatID { 
            get { return idTextBox != null ? idTextBox.Text : _formatID; }
            private set { _formatID = value; } }

        public TextBox idTextBox = null;

        public static SolidColorBrush brushVideoOnly = new SolidColorBrush(Color.FromRgb(0,0xC0,0));
        public static SolidColorBrush brushAudioOnly = new SolidColorBrush(Colors.DodgerBlue);
        public static SolidColorBrush brushAudioAndVideo = new SolidColorBrush(Colors.Yellow);
        public static SolidColorBrush brushAIUpscaled = new SolidColorBrush(Color.FromRgb(0x50,0x50,0x50));

        public UIYTDLPFormatEntry()
        {
            InitializeComponent();
        }

        public void SetCustomFormatField()
        {
            Label_FormatDisplayName.Content = "Custom";
            idTextBox = new TextBox() { Text = "", Width = 100.0 };
            Label_VideoDetails.Content = idTextBox;
            Label_FormatID.Visibility = Visibility.Collapsed;
            Label_Extension.Visibility = Visibility.Collapsed;
            Label_AudioDetails.Visibility = Visibility.Collapsed;
        }

        public void ApplyFormat(YTDLP.YTDLPFormat format)
        {
            formatID = format.formatID;
            Label_FormatDisplayName.Content = format.formatDisplayName;
            Label_FormatID.Content = format.formatID;
            Label_Extension.Content = format.ext;

            bool hasVideo = format.vcodec != null && format.vcodec != "none";
            bool hasAudio = format.acodec != null && format.acodec != "none";

            Label_FormatDisplayName.Foreground =
                format.formatDisplayName.ToLower().Contains("ai-upscaled") ? brushAIUpscaled :
                hasVideo && hasAudio ? brushAudioAndVideo :
                hasVideo ? brushVideoOnly :
                hasAudio ? brushAudioOnly :
                new SolidColorBrush(Colors.White);

            if (hasVideo)
            {
                string vdetails =
                    $"{(format.fps != null ? $"{format.fps} FPS, " : "")}{format.width}x{format.height} {(format.vbr != null ? $"{format.vbr} kbps, " : "")}{format.vcodec}";
                Label_VideoDetails.Content = vdetails;
            } else
            {
                Label_VideoDetails.Content = "<no video>";
            }

            if (hasAudio)
            {
                string adetails =
                    $"{format.asr} Hz {format.abr} kbps, {format.acodec}";
                Label_AudioDetails.Content = adetails;
            } else
            {
                Label_AudioDetails.Content = "<no audio>";
            }
        }
    }
}
