using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

namespace ReencGUI
{
    public class WindowUtil
    {
        [System.Runtime.InteropServices.DllImport("dwmapi.dll", PreserveSig = false)]
        private static extern void DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

        public static void SetWindowDarkMode(Window w)
        {
            WindowInteropHelper helper = new WindowInteropHelper(w);
            if (helper.Handle != IntPtr.Zero)
            {
                int attribute = 20; // DWMWA_USE_IMMERSIVE_DARK_MODE
                int value = 1; // Enable dark mode
                DwmSetWindowAttribute(helper.Handle, attribute, ref value, sizeof(int));
            }
        }
    }
}
