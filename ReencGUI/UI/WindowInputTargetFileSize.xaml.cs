using System;
using System.Collections.Generic;
using System.Globalization;
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
    /// Logika interakcji dla klasy WindowInputTargetFileSize.xaml
    /// </summary>
    public partial class WindowInputTargetFileSize : Window
    {
        public double? result = null;

        public WindowInputTargetFileSize()
        {
            InitializeComponent();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            WindowUtil.SetWindowDarkMode(this);
        }

        private void Button_Confirm_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                result = double.Parse(Textbox_TargetSize.Text, CultureInfo.InvariantCulture);
                Close();
            } 
            catch (Exception)
            {
                MessageBox.Show("Invalid value", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
