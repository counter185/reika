using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using reika.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace reika.Linux.UI
{
    public partial class WindowStreamSelect : Window
    {
        public List<StreamTarget> selectedStreams = null;

        List<CheckBox> checkBoxes = new List<CheckBox>();
        public WindowStreamSelect(string fileName, FFMPEG.MediaInfo media, List<FFMPEG.StreamInfo> streams)
        {
            InitializeComponent();
            int i = 0;
            foreach (var stream in streams)
            {
                StreamTarget st = new StreamTarget
                {
                    mediaInfo = media,
                    streamInfo = stream,
                    indexInStream = i++
                };

                CheckBox cb = new CheckBox
                {
                    Content = new UIStreamEntry(st),
                    VerticalContentAlignment = VerticalAlignment.Center,
                    IsChecked = true
                };
                Panel_StreamList.Items.Add(cb);
                checkBoxes.Add(cb);
            }
        }

        private void Button_Confirm_Click(object sender, RoutedEventArgs e)
        {
            selectedStreams = (from x in checkBoxes
                               where x.IsChecked == true
                               select ((UIStreamEntry)x.Content).streamTarget).ToList();
            this.Close();
        }

        private void Button_SelectAll_Click(object sender, RoutedEventArgs e)
        {
            bool set = checkBoxes[0].IsChecked != true;
            foreach (var cb in checkBoxes)
            {
                cb.IsChecked = set;
            }
        }
    }
}