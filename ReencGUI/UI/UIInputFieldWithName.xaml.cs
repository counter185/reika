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
    /// Logika interakcji dla klasy UIInputFieldWithName.xaml
    /// </summary>
    public partial class UIInputFieldWithName : UserControl
    {
        public string InputFieldName
        {
            get { return (string)GetValue(InputFieldNameProperty); }
            set { SetValue(InputFieldNameProperty, value); }
        }

        public static readonly DependencyProperty InputFieldNameProperty =
            DependencyProperty.Register("InputFieldName", typeof(string), typeof(UIInputFieldWithName), new PropertyMetadata("Name"));

        public UIInputFieldWithName()
        {
            DataContext = this;
            InitializeComponent();
        }
    }
}
