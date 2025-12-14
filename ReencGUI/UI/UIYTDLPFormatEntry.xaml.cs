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
        public string formatID = null;

        public UIYTDLPFormatEntry()
        {
            InitializeComponent();
        }

        public void ApplyFormat(YTDLP.YTDLPFormat format)
        {
            formatID = format.formatID;
            Label_FormatDisplayName.Content = format.formatDisplayName;
            Label_FormatID.Content = format.formatID;
            Label_Extension.Content = format.ext;
            if (format.vcodec != null && format.vcodec != "none")
            {
                string vdetails =
                    $"{(format.fps != null ? $"{format.fps} FPS, " : "")}{format.width}x{format.height} {(format.vbr != null ? $"{format.vbr} kbps, " : "")}{format.vcodec}";
                Label_VideoDetails.Content = vdetails;
            } else
            {
                Label_VideoDetails.Content = "<no video>";
            }

            if (format.acodec != null && format.acodec != "none")
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
