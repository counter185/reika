using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

namespace ReencGUI
{
    public class WindowUtil
    {
        const int GWL_STYLE = -16;
        const int WS_SYSMENU = 0x00080000;

        [System.Runtime.InteropServices.DllImport("dwmapi.dll", PreserveSig = false)]
        private static extern void DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLongA(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        public static void SetWindowDarkMode(Window w)
        {
            try
            {
                WindowInteropHelper helper = new WindowInteropHelper(w);
                if (helper.Handle != IntPtr.Zero)
                {
                    int attribute = 20; // DWMWA_USE_IMMERSIVE_DARK_MODE
                    int value = 1; // Enable dark mode
                    DwmSetWindowAttribute(helper.Handle, attribute, ref value, sizeof(int));
                }
            }
            catch (Exception) { }
        }

        public static void RemoveCloseButton(Window w)
        {
            WindowInteropHelper helper = new WindowInteropHelper(w);
            if (helper.Handle != IntPtr.Zero)
            {
                SetWindowLong(helper.Handle, GWL_STYLE, GetWindowLongA(helper.Handle, GWL_STYLE) & ~WS_SYSMENU);
            }
        }
    }
}
