using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using System;

namespace reika.Linux.UI.Popup
{
    public enum PopupStyle
    {
        None, Info, Error, Warning
    }
    public enum PopupResult
    {
        None, Yes, No, Cancel
    }

    public partial class PopupBase : Window
    {
        protected PopupResult result;
        public PopupBase()
        {
            InitializeComponent();
        }

        protected void SetStyle(PopupStyle style)
        {
            Classes.Clear();
            Classes.Add(
                style == PopupStyle.Warning ? "reikaPopupWarning"
                : style == PopupStyle.Error ? "reikaPopupError"
                : "reikaWindow"
            );
        }

        protected void AddButton(string text, Action action)
        {
            Button newButton = new Button();
            newButton.Content = text;
            newButton.MinWidth = 70;
            newButton.Height = 32;
            newButton.Margin = new Thickness(5, 0, 5, 0);
            newButton.Classes.Add("reikaButton");
            newButton.Click += (a, b) => action();
            Panel_Buttons.Children.Add(newButton);
        }
    }
}