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
    /// Logika interakcji dla klasy UIEncoderEntry.xaml
    /// </summary>
    public partial class UIEncoderEntry : UserControl
    {
        public string PrimaryText
        {
            get { return (string)GetValue(PrimaryTextProperty); }
            set { SetValue(PrimaryTextProperty, value); }
        }

        public string SecondaryText;

        public static readonly DependencyProperty PrimaryTextProperty =
            DependencyProperty.Register("PrimaryText", typeof(string), typeof(UIEncoderEntry), new PropertyMetadata("Primary Text"));

        public UIEncoderEntry(string primary, string secondary)
        {
            DataContext = this;
            PrimaryText = primary;
            SecondaryText = secondary;
            InitializeComponent();

            Text_Primary.Content = primary;
            Text_Secondary.Text = secondary;
        }
    }
}
