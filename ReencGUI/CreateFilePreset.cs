using ReencGUI.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ReencGUI
{
    public class CreateFilePreset
    {
        public string name;
        public List<string> vcodecs;
        public string vbitrate;
        public string acodec;
        public string abitrate;
        public string otherArgs = null;

        public bool Save(string path)
        {
            XmlDocument doc = new XmlDocument();
            XmlElement root = doc.CreateElement("ReikaPreset");
            root.AppendChild(doc.CreateElement("Name")).InnerText = name;

            XmlElement vcodecs = doc.CreateElement("VideoCodecs");
            foreach (string codec in this.vcodecs)
            {
                XmlElement codecElement = doc.CreateElement("Codec");
                codecElement.InnerText = codec;
                vcodecs.AppendChild(codecElement);
            }
            root.AppendChild(vcodecs);

            root.AppendChild(doc.CreateElement("VideoBitrate")).InnerText = vbitrate;
            root.AppendChild(doc.CreateElement("AudioCodec")).InnerText = acodec;
            root.AppendChild(doc.CreateElement("AudioBitrate")).InnerText = abitrate;
            if (!string.IsNullOrEmpty(otherArgs))
            {
                root.AppendChild(doc.CreateElement("OtherArgs")).InnerText = otherArgs;
            }
            doc.AppendChild(root);
            doc.Save(path);
            return true;
        }

        public static CreateFilePreset Load(string path)
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(path);
                XmlElement root = doc.DocumentElement;
                CreateFilePreset preset = new CreateFilePreset();
                preset.name = root["Name"].InnerText;
                preset.vcodecs = new List<string>();
                foreach (XmlElement codecElement in root["VideoCodecs"].GetElementsByTagName("Codec"))
                {
                    preset.vcodecs.Add(codecElement.InnerText);
                }
                preset.vbitrate = root["VideoBitrate"].InnerText;
                preset.acodec = root["AudioCodec"].InnerText;
                preset.abitrate = root["AudioBitrate"].InnerText;
                if (root["OtherArgs"] != null)
                {
                    preset.otherArgs = root["OtherArgs"].InnerText;
                }
                return preset;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading preset from {path}: {ex.Message}");
                return null;
            }
        }
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
            ulong bps = Utils.CalculateBitsPerSecondForSize(Utils.Megabytes(9.7), session.GetDuration() + 1000); //+1s to be safe
            if (bps > 128000)
            {
                bps -= 128000; //reserve 128kbps for audio
            }

            vbitrate = $"{Math.Max(1, bps / 1000)}k"; //convert to kbps
        }
    }
}
