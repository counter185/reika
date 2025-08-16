using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace ReencGUI
{
    public class Utils
    {
        public static ulong LengthToMS(int hours, int minutes, int seconds, int ms)
        {
            return (ulong)(ms + seconds * 1000 + minutes * 60 * 1000 + hours * 60 * 60 * 1000);
        }

        public static ulong CalculateBitsPerSecondForSize(ulong sizeInBytes, ulong durationMS)
        {
            if (durationMS == 0)
            {
                return 0;
            }
            ulong sizeInBits = sizeInBytes * 8;
            return sizeInBits / (ulong)Math.Ceiling(durationMS / 1000.0);
        }
        
        public static ulong Megabytes(ulong mb)
        {
            return mb * 1024 * 1024; //convert megabytes to bytes
        }

        public static string SanitizeForXAML(string input)
        {
            if (input == null) return null;
            return input.Replace("_", "__");
        }

        public static BitmapImage LoadToMemFromUri(Uri uri)
        {
            if (uri != null)
            {
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = uri;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
            return null;
        }
    }
}
