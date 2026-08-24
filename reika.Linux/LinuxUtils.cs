using Avalonia.Input;
using System;
using System.Collections.Generic;
using System.Text;

namespace reika.Linux
{
    public static class LinuxUtils
    {
        public static string GetSystemHardwareInfo()
        {
            return "bimbux";
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
    }
}
