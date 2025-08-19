using ReencGUI.UI;
using System;
using System.Collections.Generic;
using System.IO;
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
        //used for windowcreatefile
        public abstract void Recalculate(WindowCreateFile session);
        //used for quick reencode
        public abstract void Recalculate(FFMPEG.MediaInfo singleMedia);
    }

    public abstract class TargetFilesizePreset : DynamicCreateFilePreset
    {
        private ulong targetSizeBytes;
        public TargetFilesizePreset(ulong targetSizeBytes)
        {
            this.targetSizeBytes = targetSizeBytes;
        }

        void RecalcFromTime(ulong time)
        {
            ulong bps = Utils.CalculateBitsPerSecondForSize(targetSizeBytes, time + 1000); //+1s to be safe
            if (bps > 128000)
            {
                bps -= 128000; //reserve 128kbps for audio
            }

            vbitrate = $"{Math.Max(1, bps / 1000)}k"; //convert to kbps
        }

        public override void Recalculate(WindowCreateFile session)
        {
            RecalcFromTime(session.GetDuration());
        }

        public override void Recalculate(FFMPEG.MediaInfo singleMedia)
        {
            RecalcFromTime(singleMedia.Duration);
        }
    }

    public class Discord10MBPreset : TargetFilesizePreset
    {
        public Discord10MBPreset() : base(Utils.Megabytes(9.7))
        {
            name = "Discord 10MB H264";
            vcodecs = new List<string> { /*"h264_nvenc", "h264_amf",*/ "libx264" }; //harware h264 overshoots 10mb way too often
            vbitrate = "10000k";
            acodec = "aac";
            abitrate = "128k";
        }
    }

    public class Discord50MBPreset : TargetFilesizePreset
    {
        public Discord50MBPreset() : base(Utils.Megabytes(48))
        {
            name = "Discord 50MB H264";
            vcodecs = new List<string> { /*"h264_nvenc", "h264_amf",*/ "libx264" }; //hardware may be way more viable here?
            vbitrate = "10000k";
            acodec = "aac";
            abitrate = "128k";
        }
    }

    public static class PresetManager
    {
        public static List<CreateFilePreset> LoadPresets()
        {
            var presets = new List<CreateFilePreset>();
            try
            {
                foreach (string file in Directory.GetFiles(AppData.GetAppDataSubdir("presets"), "*.reikapreset"))
                {
                    CreateFilePreset preset = CreateFilePreset.Load(file);
                    if (preset != null)
                    {
                        preset.name = System.IO.Path.GetFileNameWithoutExtension(file);
                        presets.Add(preset);
                    }
                    else
                    {
                        Console.WriteLine($"Failed to load preset: {file}");
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to load presets: {e.Message}");
            }

            presets.Add(new Discord10MBPreset());
            presets.Add(new Discord50MBPreset());
            presets.Add(new CreateFilePreset
            {
                name = "H264: Moderate",
                vbitrate = "12000k",
                vcodecs = new List<string> { "hevc_nvenc", "hevc_amf", "libx265" },
                acodec = "copy",
                abitrate = ""
            });
            presets.Add(new CreateFilePreset
            {
                name = "H265: Quality",
                vbitrate = "12000k",
                vcodecs = new List<string> { "hevc_nvenc", "hevc_amf", "libx265" },
                acodec = "copy",
                abitrate = ""
            });
            presets.Add(new CreateFilePreset
            {
                name = "H265: Moderate",
                vbitrate = "8000k",
                vcodecs = new List<string> { "hevc_nvenc", "hevc_amf", "libx265" },
                acodec = "copy",
                abitrate = ""
            });
            presets.Add(new CreateFilePreset
            {
                name = "H265: File size",
                vbitrate = "4000k",
                vcodecs = new List<string> { "hevc_nvenc", "hevc_amf", "libx265" },
                acodec = "copy",
                abitrate = ""
            });
            presets.Add(new CreateFilePreset
            {
                name = "H266: 2mpbs",
                vbitrate = "2000k",
                vcodecs = new List<string> { "libvvenc" },
                acodec = "copy",
                abitrate = ""
            });
            presets.Add(new CreateFilePreset
            {
                name = "VP9 YouTube quality",
                vbitrate = "2000k",
                vcodecs = new List<string> { "vp9_qsv", "libvpx-vp9", "vp9" },
                acodec = "libopus",
                abitrate = ""
            });
            /*presets.Add(new CreateFilePreset
            {
                name = "PSP",
                vbitrate = "1000k",
                vcodecs = new List<string> { "h264_nvenc", "h264_amf", "libx264" },
                acodec = "aac",
                abitrate = "128k",
                otherArgs = "-profile:v main -vf \"scale=480:272,setsar=1:1\""
            });*/
            return presets;
        }
    }
}
