using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using reika.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace reika.Linux
{
    public static class LinuxUtils
    {
        const int SIGTERM = 15;

        [DllImport("libc")]
        private static extern int kill(int pid, int signal);

        public static void SendProcessSIGTERM(Process p)
        {
            kill(p.Id, SIGTERM);
        }

        public static string GetSystemHardwareInfo()
        {
            try
            {
                var output = FFMPEG.RunCommandAndGetOutput("lsb_release", new string[] { "-a" });
                string? desc = output.Where(x => x.StartsWith("Description:")).Select(x => x.Split(":")[1].Trim()).FirstOrDefault();

                var cpuinfo = FFMPEG.RunCommandAndGetOutput("cat", new string[] { "/proc/cpuinfo" });
                string? cpu = cpuinfo.Where(x => x.StartsWith("model name")).Select(x => x.Split(":")[1].Trim()).FirstOrDefault();

                return
                    $"{(desc ?? "<unknown OS>")}\n" +
                    $"{(cpu ?? "<unknown CPU>")}";
            } catch (Exception)
            {
                return "<unknown>";
            }
        }

        public static void FFMPEGCleanupThumbnails()
        {

        }


        public static void AddRightMouseButtonDownHandler(Avalonia.Interactivity.Interactive o, Action action)
        {
            o.AddHandler(InputElement.PointerPressedEvent,
                (a, b) => {
                    if (b.Properties.PointerUpdateKind == PointerUpdateKind.RightButtonPressed)
                    {
                        action();
                    }
                },
                Avalonia.Interactivity.RoutingStrategies.Tunnel);
        }

        public static void ShowDialog(Window w, Window parent)
        {
            bool createdPlaceholderWindow = false;
            if (parent == null || !parent.IsVisible)
            {
                createdPlaceholderWindow = true;
                parent = new Window();
                parent.Width = parent.Height = 1;
                parent.WindowDecorations = WindowDecorations.None;
                parent.Background = Brushes.Transparent;
                parent.Show();
            }
            using (var source = new CancellationTokenSource())
            {
                w.ShowDialog(parent).ContinueWith(t => source.Cancel(), TaskScheduler.FromCurrentSynchronizationContext());
                Dispatcher.UIThread.MainLoop(source.Token);
            }
            if (createdPlaceholderWindow)
            {
                parent.Close();
            }
        }
    }
}
