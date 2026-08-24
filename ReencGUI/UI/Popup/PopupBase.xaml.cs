using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
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

namespace ReencGUI.UI.Popup
{
    public enum PopupStyle
    {
        None, Info, Error, Warning
    }
    public enum PopupResult
    {
        None, Yes, No, Cancel
    }

    /// <summary>
    /// Logika interakcji dla klasy PopupBase.xaml
    /// </summary>
    public partial class PopupBase : DarkWindow
    {
        protected PopupResult result;
        public PopupBase()
        {
            InitializeComponent();
        }
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            WindowUtil.RemoveCloseButton(this);
        }

        protected void SetStyle(PopupStyle style)
        {
            Style = (Style)FindResource(
                style == PopupStyle.Warning ? "PopupWarning"
                : style == PopupStyle.Error ? "PopupError"
                : "ReencWindowStyle"
            );

            if (style == PopupStyle.Error)
            {
                SystemSounds.Hand.Play();
            } 
            else if (style == PopupStyle.Warning)
            {
                SystemSounds.Exclamation.Play();
            }
            else
            {
                SystemSounds.Question.Play();
            }
        }

        protected void AddButton(string text, Action action)
        {
            Button newButton = new Button();
            newButton.Content = text;
            newButton.MinWidth = 70;
            newButton.Height = 25;
            newButton.Margin = new Thickness(5, 0, 5, 0);
            newButton.Style = (Style)FindResource("ReencButtonStyle");
            newButton.Click += (a,b)=>action();
            Panel_Buttons.Children.Add(newButton);
        }
    }
}
