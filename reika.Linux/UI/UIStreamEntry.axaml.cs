using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using reika.Core;
using System;

namespace reika.Linux.UI
{
    public partial class UIStreamEntry : UserControl
    {
        public StreamTarget streamTarget;
        public Uri thumbnailUri = null;

        public UIStreamEntry(StreamTarget streamTarget)
        {
            this.streamTarget = streamTarget;
            InitializeComponent();
            Label_Primary.Content = $"{streamTarget.streamInfo.mediaType.ToString()} Stream (#{streamTarget.indexInStream})";
            Label_Secondary.Content = $"{streamTarget.streamInfo.resolution}";
            Label_Details.Content = $"{streamTarget.streamInfo.encoderID} {streamTarget.streamInfo.bitrate} {streamTarget.streamInfo.encoderName}";
            Label_Duration.Content = $"{streamTarget.mediaInfo.dH:D2}:{streamTarget.mediaInfo.dM:D2}:{streamTarget.mediaInfo.dS:D2}.{streamTarget.mediaInfo.dMS:D3}";
            Label_FileName.Content = System.IO.Path.GetFileName(streamTarget.mediaInfo.fileName);
            Image_Thumbnail.IsVisible = false;
            if (streamTarget.streamInfo.mediaType == FFMPEG.CodecType.Video)
            {
                ulong durationMS = Utils.LengthToMS(streamTarget.mediaInfo.dH, streamTarget.mediaInfo.dM, streamTarget.mediaInfo.dS, streamTarget.mediaInfo.dMS);
                ThumbnailUtil.FFMPEGExtractThumbnailAsync(streamTarget.mediaInfo.fileName, durationMS == 0 ? "00" : "01", (uri) =>
                {
                    if (uri != null)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            thumbnailUri = uri;
                            Image_Thumbnail.Source = LinuxUtils.LoadToMemFromUri(uri);
                            Image_Thumbnail.IsVisible = true;
                        });
                    }
                });
            }
        }
    }
}