using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Text;

namespace reika.Linux.UI.Popup
{
    public class PopupOK : PopupBase
    {
        public PopupOK()
        {
            AddButton("OK", () => { this.Close(); });
        }

        public static void Show(string message, string caption, PopupStyle style = PopupStyle.None)
        {
            Window placeholderWindow = new Window();
            placeholderWindow.WindowDecorations = WindowDecorations.None;
            placeholderWindow.Width = placeholderWindow.Height = 2;
            placeholderWindow.Show();
            //todo: make img affect color or something
            PopupOK p = new PopupOK();
            p.Label_MainText.Content = message;
            p.Title = caption;
            p.SetStyle(style);
            p.ShowDialog(placeholderWindow);
            //placeholderWindow.Close();
        }
    }
}
