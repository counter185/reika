using Microsoft.Win32;
using reika.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace ReencGUI
{
    public static class WindowsUtils
    {
        public static BitmapImage LoadToMemFromUri(Uri uri)
        {
            if (uri != null)
            {
                try
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = uri;
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    return bitmap;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading image from URI: {uri}. Exception: {ex.Message}");
                }
            }
            return null;
        }
        public static BitmapImage LoadImageFromUri(Uri u) => new BitmapImage(u);

        public static string GetSystemHardwareInfo()
        {
            //cpu name: HARDWARE\DESCRIPTION\System\CentralProcessor\0 key ProcessorNameString
            //video drivers: SYSTEM\CurrentControlSet\Control\Video\{guid}\0000
            //    key DriverDesc for gpu name
            //    key DriverVersion for gpu driver version

            try
            {
                string cpuName = "";
                using (var cpuKey = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor", false))
                {
                    if (cpuKey != null)
                    {
                        using (RegistryKey cpuKey0 = cpuKey.OpenSubKey("0", false))
                        {
                            cpuName = cpuKey0.GetValue("ProcessorNameString")?.ToString() ?? "";
                        }
                        cpuName += $"(x {cpuKey.SubKeyCount})";
                    }
                }


                List<string> gpuNames = new List<string>();

                using (var videoKeys = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Video", false))
                {
                    string[] subKeys = videoKeys.GetSubKeyNames();
                    foreach (string gpuKeyStr in subKeys)
                    {
                        using (var gpuKey = videoKeys.OpenSubKey($"{gpuKeyStr}\\0000", false))
                        {
                            if (gpuKey != null)
                            {
                                string infSection = gpuKey.GetValue("InfSection")?.ToString() ?? "";
                                //ignore meta virtual monitor whatever
                                if (!infSection.StartsWith("VirtualScreen"))
                                {
                                    string gpuName = gpuKey.GetValue("DriverDesc")?.ToString() ?? "";
                                    string gpuVersion = gpuKey.GetValue("DriverVersion")?.ToString() ?? "";
                                    if (gpuName != "")
                                    {
                                        gpuNames.Add($"{gpuName} (driver {gpuVersion})");
                                    }
                                }
                            }
                        }
                    }
                }

                return string.Join("\n", (new string[] { cpuName }).Concat(gpuNames));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving system hardware info: {ex.Message}");
                return "<error getting hardware info>";
            }
        }
    }
}
