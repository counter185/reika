using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ReencGUI.UI
{
    /// <summary>
    /// Logika interakcji dla klasy UIFFMPEGOperationEntry.xaml
    /// </summary>
    public partial class UIFFMPEGOperationEntry : UserControl
    {
        public UIFFMPEGOperationEntry()
        {
            InitializeComponent();
        }

        public void UpdateProgressBasedOnLogKVs(Dictionary<string, string> logOutputKVs, ulong fileDuration)
        {
            List<string> secondaryTextDetails = new List<string>();
            List<string> secondaryText2Details = new List<string>();

            if (logOutputKVs.ContainsKey("time"))
            {
                int h, m, s, ms;
                Match timeMatch = Regex.Match(logOutputKVs["time"], @"(\d+):(\d+):(\d+).(\d+)");
                if (timeMatch.Success)
                {
                    h = int.Parse(timeMatch.Groups[1].Value);
                    m = int.Parse(timeMatch.Groups[2].Value);
                    s = int.Parse(timeMatch.Groups[3].Value);
                    ms = int.Parse(timeMatch.Groups[4].Value);
                    ulong currentTimeMS = Utils.LengthToMS(h, m, s, ms);
                    double progress = (double)currentTimeMS / fileDuration;
                    ProgressBar_Operation.Value = progress * 100;
                }
            }

            if (logOutputKVs.ContainsKey("frame"))
            {
                secondaryTextDetails.Add($"{logOutputKVs["frame"]} frames");
            }

            if (logOutputKVs.ContainsKey("fps"))
            {
                secondaryTextDetails.Add($"{logOutputKVs["fps"]} FPS");
            }

            if (logOutputKVs.ContainsKey("size"))
            {
                secondaryTextDetails.Add($"{logOutputKVs["size"]}");
            }

            if (logOutputKVs.ContainsKey("bitrate"))
            {
                secondaryText2Details.Add($"{logOutputKVs["bitrate"]}");
            }

            if (logOutputKVs.ContainsKey("speed"))
            {
                secondaryText2Details.Add($"{logOutputKVs["speed"]}");
            }

            Label_Secondary.Content = string.Join(", ", secondaryTextDetails);
            Label_Secondary2.Content = string.Join(", ", secondaryText2Details);
        }
    }
}
