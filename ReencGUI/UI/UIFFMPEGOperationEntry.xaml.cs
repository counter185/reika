using System;
using System.Collections.Generic;
using System.Globalization;
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
        Dictionary<int, Brush> encoderChannelBackgrounds = new Dictionary<int, Brush>
        {
            { 0, Brushes.Transparent},
            { 1, new LinearGradientBrush(Color.FromArgb(0x60, 0, 0x7e, 0x15), Color.FromArgb(0x10, 0, 0x7e, 0x15), 0) },
            { 2, new LinearGradientBrush(Color.FromArgb(0x60, 0x7e, 0, 0), Color.FromArgb(0x10, 0x7e, 0, 0), 0) },
            { 3, new LinearGradientBrush(Color.FromArgb(0x60, 0, 0x5e, 0x7e), Color.FromArgb(0x10, 0, 0x5e, 0x7e), 0) },
            { 4, new LinearGradientBrush(Color.FromArgb(0x60, 0x7e, 0x7a, 0), Color.FromArgb(0x10, 0x7e, 0x7a, 0), 0) },
        };

        public UIFFMPEGOperationEntry()
        {
            InitializeComponent();
        }

        public void BackgroundFromEncoderChannel(int channel)
        {
            if (encoderChannelBackgrounds.ContainsKey(channel))
            {
                Background = encoderChannelBackgrounds[channel];
            }
        }

        public void UpdateProgressBasedOnLogKVs(Dictionary<string, string> logOutputKVs, ulong fileDuration)
        {
            List<string> secondaryTextDetails = new List<string>();
            List<string> secondaryText2Details = new List<string>();

            ulong remainingDuration = fileDuration;

            if (logOutputKVs.ContainsKey("time"))
            {
                try
                {
                    ulong currentTimeMS = Utils.ParseDuration(logOutputKVs["time"]);
                    remainingDuration = fileDuration - currentTimeMS;
                    double progress = (double)currentTimeMS / fileDuration;
                    ProgressBar_Operation.Value = progress * 100;
                }
                catch (Exception) { }
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
                secondaryTextDetails.Add(Utils.KiBStringToFriendlySizeString(logOutputKVs["size"]));
            }

            if (logOutputKVs.ContainsKey("bitrate"))
            {
                secondaryText2Details.Add($"{logOutputKVs["bitrate"]}");
            }

            if (logOutputKVs.ContainsKey("speed"))
            {
                string speedS = logOutputKVs["speed"];
                secondaryText2Details.Add($"{speedS}");
                if (fileDuration != 0)
                {
                    try
                    {
                        Match m = Regex.Match(speedS, @"(\d+\.\d+)x");
                        if (m.Success)
                        {
                            double speedD = double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
                            ulong msRemaining = (ulong)(remainingDuration / speedD);
                            secondaryText2Details.Add($"ETA {Utils.FriendlyDurationString(msRemaining)}");
                        }
                    }
                    catch (Exception) { }
                } else
                {
                    secondaryText2Details.Add($"ETA ???");
                }
            }

            Label_Secondary.Content = string.Join(", ", secondaryTextDetails);
            Label_Secondary2.Content = string.Join(", ", secondaryText2Details);
        }
    }
}
