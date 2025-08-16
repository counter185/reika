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
    /// Logika interakcji dla klasy UIStreamEntry.xaml
    /// </summary>
    public partial class UIStreamEntry : UserControl
    {
        public StreamTarget streamTarget;
        public Uri thumbnailUri = null;

        public UIStreamEntry(StreamTarget streamTarget)
        {
            this.streamTarget = streamTarget;
            InitializeComponent();
            Label_Primary.Content = $"{streamTarget.streamInfo.mediaType.ToString()} Stream (#{streamTarget.indexInStream})";
            Label_Secondary.Content = $"{streamTarget.streamInfo.resolution} {streamTarget.streamInfo.bitrate}";
            Label_Details.Content = $"{streamTarget.streamInfo.encoderID} ({streamTarget.streamInfo.encoderName})";
            Label_Duration.Content = $"{streamTarget.mediaInfo.dH:D2}:{streamTarget.mediaInfo.dM:D2}:{streamTarget.mediaInfo.dS:D2}.{streamTarget.mediaInfo.dMS:D3}";
            Image_Thumbnail.Visibility = Visibility.Collapsed;
            if (streamTarget.streamInfo.mediaType == FFMPEG.CodecType.Video)
            {
                ulong durationMS = Utils.LengthToMS(streamTarget.mediaInfo.dH, streamTarget.mediaInfo.dM, streamTarget.mediaInfo.dS, streamTarget.mediaInfo.dMS);
                FFMPEG.ExtractThumbnailAsync(streamTarget.mediaInfo.fileName, durationMS == 0 ? "00" : "01", (uri)=>
                {
                    if (uri != null)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            thumbnailUri = uri;
                            Image_Thumbnail.Source = Utils.LoadToMemFromUri(uri);
                            Image_Thumbnail.Visibility = Visibility.Visible;
                        });
                    }
                });
            }
        }
    }
}
