using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace reika.Linux.UI.Popup
{
    public class PopupYesNoCancel : PopupBase
    {
        public PopupYesNoCancel()
        {
            AddButton("Yes", () => { result = PopupResult.Yes; this.Close(); });
            AddButton("No", () => { result = PopupResult.No; this.Close(); });
            AddButton("Cancel", () => { result = PopupResult.Cancel; this.Close(); });
        }

        public static PopupResult Show(string message, string caption, PopupStyle style)
        {
            //todo: make img affect color or something
            PopupYesNoCancel p = new PopupYesNoCancel();
            p.Label_MainText.Content = message;
            p.Title = caption;
            p.SetStyle(style);
            p.ShowDialog(MainWindow.instance);
            return p.result;
        }
    }

    public class PopupYesNo : PopupBase
    {
        public PopupYesNo()
        {
            AddButton("Yes", () => { result = PopupResult.Yes; this.Close(); });
            AddButton("No", () => { result = PopupResult.No; this.Close(); });
        }

        public static PopupResult Show(string message, string caption, PopupStyle style = PopupStyle.None)
        {
            //todo: make img affect color or something
            PopupYesNo p = new PopupYesNo();
            p.Label_MainText.Content = message;
            p.Title = caption;
            p.SetStyle(style);
            LinuxUtils.ShowDialog(p, MainWindow.instance);
            return p.result;
        }
    }
}
