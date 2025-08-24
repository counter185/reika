using ReencGUI.UI;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;

namespace ReencGUI
{
    /// <summary>
    /// Logika interakcji dla klasy App.xaml
    /// </summary>
    public partial class App : Application
    {
        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();

        protected override void OnStartup(StartupEventArgs e)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            base.OnStartup(e);
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            List<string> targetFiles = new List<string>();
            foreach (string arg in e.Args)
            {
                switch (arg)
                {
                    case "-output":
                        AllocConsole();
                        break;
                    default:
                        if (File.Exists(arg))
                        {
                            targetFiles.Add(arg);
                        }
                        break;
                }
            }
            MainWindow mainWindow = new MainWindow();
            if (targetFiles.Any())
            {
                foreach (string target in targetFiles)
                {
                    mainWindow.OpenCreateFileWindowForFile(target);
                }
            }
            mainWindow.Show();
        }
    }
}
