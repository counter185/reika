using ReencGUI.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReencGUI
{
    public class CreateFilePreset
    {
        public string name;
        public List<string> vcodecs;
        public string vbitrate;
        public string acodec;
        public string abitrate;
    }

    public abstract class DynamicCreateFilePreset : CreateFilePreset
    {
        public abstract void Recalculate(WindowCreateFile session);
    }

    public class Discord10MBPreset : DynamicCreateFilePreset
    {
        public Discord10MBPreset()
        {
            name = "Discord 10MB";
            vcodecs = new List<string> { "h264_nvenc", "h264_amf", "libx264" };
            vbitrate = "10000k";
            acodec = "aac";
            abitrate = "128k";
        }
        public override void Recalculate(WindowCreateFile session)
        {
            ulong bps = Utils.CalculateBitsPerSecondForSize(Utils.Megabytes(10), session.GetDuration());
            if (bps > 128000)
            {
                bps -= 128000; //reserve 128kbps for audio
            }

            vbitrate = $"{Math.Max(1, bps / 1000)}k"; //convert to kbps
        }
    }
}
