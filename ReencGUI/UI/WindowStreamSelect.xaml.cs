using System;
using System.Collections.Generic;
using System.IO.Packaging;
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
    /// Logika interakcji dla klasy WindowStreamSelect.xaml
    /// </summary>
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
                StreamTarget st = new StreamTarget {
                    mediaInfo = media,
                    streamInfo = stream,
                    indexInStream = i++
                };

                CheckBox cb = new CheckBox
                {
                    Content = new UIStreamEntry(st),
                    VerticalContentAlignment = VerticalAlignment.Center,
                };
                Panel_StreamList.Items.Add(cb);
                checkBoxes.Add(cb);
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            WindowUtil.SetWindowDarkMode(this);
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
