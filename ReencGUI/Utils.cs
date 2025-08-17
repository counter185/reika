using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

        public static ulong ParseDuration(string s)
        {
            var match = Regex.Match(s, @"^(?:(\d{2}):)?(?:(\d{2}):)?(\d{2})(\.\d{1,3})?$");
            if (match.Success)
            {
                List<string> groups = new List<string>();
                foreach (Group group in match.Groups)
                {
                    string value = group.Value;
                    if (value != null && value.Length > 0)
                    {
                        groups.Add(value);
                    }
                }
                groups = groups.Skip(1).ToList();
                if (!groups.Last().StartsWith("."))
                {
                    groups.Add(".000");
                }
                groups.Reverse();
                ulong denom = 1;
                ulong ret = 0;
                foreach (string group in groups)
                {
                    if (group.StartsWith("."))
                    {
                        denom = 1000;
                        ret += ulong.Parse(group.Substring(1));
                    } else
                    {
                        ret += ulong.Parse(group) * denom;
                        denom *= 60;
                    }
                }
                return ret;
            } 
            else
            {
                throw new ArgumentException("invalid duration format");
            }
        }

        public static ulong ParseBitrate(string ffbitrate)
        {
            Match match = Regex.Match(ffbitrate, @"^(\d+)([kKmMgG]?)$");
            if (match.Success)
            {
                Dictionary<string, ulong> denoms = new Dictionary<string, ulong>
                {
                    { "k", 1000 },
                    { "m", 1000000 },
                    { "g", 1000000000 }
                };
                return ulong.Parse(match.Groups[1].Value)
                    * (match.Groups.Count > 2 ? denoms[match.Groups[2].Value.ToLower()] : 1);
            } else
            {
                throw new ArgumentException("Invalid bitrate format");
            }
        }

        public static string ByteCountToFriendlyString(ulong byteCount)
        {
            if (byteCount < 1024)
            {
                return $"{byteCount} B";
            }
            else if (byteCount < 1024 * 1024)
            {
                return $"{Math.Round(byteCount / 1024.0, 2)} KB";
            }
            else if (byteCount < 1024 * 1024 * 1024)
            {
                return $"{Math.Round(byteCount / (1024.0 * 1024.0), 2)} MB";
            }
            else
            {
                return $"{Math.Round(byteCount / (1024.0 * 1024.0 * 1024.0), 2)} GB";
            }
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
        
        public static ulong Megabytes(double mb)
        {
            return (ulong)(mb * 1024 * 1024); //convert megabytes to bytes
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

        public static string EncodeUTF8ForCommandLine(string a)
        {
            return Encoding.Default.GetString(Encoding.UTF8.GetBytes(a));
        }
    }
}
