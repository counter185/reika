using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReencGUI
{
    public static class AppData
    {

        static bool EnsureDir(string dir)
        {
            try
            {
                Directory.CreateDirectory(dir);
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine($"EnsureDir failed - {dir}: {e.Message}");
                return false;
            }
        }
        public static string GetAppDataPath()
        {
            string path = Path.Combine(Environment.GetEnvironmentVariable("APPDATA"), "reika");
            EnsureDir(path);
            return path;
        }

        public static string GetAppDataSubdir(string subdir)
        {
            string path = Path.Combine(GetAppDataPath(), subdir);
            EnsureDir(path);
            return path;
        }
    }
}
