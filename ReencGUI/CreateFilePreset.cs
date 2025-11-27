using Microsoft.Win32;
using ReencGUI.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;

namespace ReencGUI
{
    public class CreateFilePreset
    {
        public string name;
        public List<string> vcodecs;
        public string requiredExtension = null;
        public string vbitrate;
        public string vresolution = null;
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

            if (!string.IsNullOrEmpty(requiredExtension))
            {
                root.AppendChild(doc.CreateElement("RequiredExtension")).InnerText = requiredExtension;
            }

            root.AppendChild(doc.CreateElement("VideoBitrate")).InnerText = vbitrate;
            if (!string.IsNullOrEmpty(vresolution))
            {
                root.AppendChild(doc.CreateElement("VideoResolution")).InnerText = vresolution;
            }
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
                if (root["RequiredExtension"] != null)
                {
                    preset.requiredExtension = root["RequiredExtension"].InnerText;
                }
                preset.vbitrate = root["VideoBitrate"].InnerText;
                if (root["VideoResolution"] != null)
                {
                    preset.vresolution = root["VideoResolution"].InnerText;
                }
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
        protected ulong targetSizeBytes;
        public TargetFilesizePreset(ulong targetSizeBytes)
        {
            this.targetSizeBytes = targetSizeBytes;
        }

        protected virtual void RecalcFromTime(ulong time)
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

    public class CustomTargetSizePreset : TargetFilesizePreset
    {
        static double? sessionDefault = null;

        public CustomTargetSizePreset() : base(1) {
            name = "Custom file size target";
            vcodecs = new List<string>() { Settings.settings.FromKey("reika.presets.sizetarget.videoCodec").GetString() };
            acodec = "aac";
            abitrate = "128k";
        }

        protected override void RecalcFromTime(ulong time)
        {
            if (sessionDefault == null)
            {
                WindowInputTargetFileSize wd = new WindowInputTargetFileSize();
                wd.ShowDialog();
                targetSizeBytes = Utils.Megabytes(wd.result != null ? wd.result.Value : Utils.Megabytes(10));
                if (wd.result != null && wd.Checkbox_DontAskAgain.IsChecked == true)
                {
                    sessionDefault = wd.result.Value;
                }
            } else
            {
                targetSizeBytes = Utils.Megabytes(sessionDefault.Value);
            }
            targetSizeBytes = (ulong)(targetSizeBytes * 0.97);
            base.RecalcFromTime(time);
        }
    }

    public class DiscordPreset : TargetFilesizePreset
    {
        public DiscordPreset(string name, List<string> encoders, ulong size)  : base(size)
        {
            this.name = name;
            vcodecs = encoders;
            requiredExtension = ".mp4";
            acodec = Settings.settings.FromKey("reika.presets.discord.useOpusInsteadOfAAC").GetBool() ? "libopus" : "aac";
            abitrate = "128k";
        }
    }
    public static class PresetManager
    {
        public static void PromptInstallPreset()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "reika Preset|*.reikapreset",
                Title = "reika: install preset",
                Multiselect = true,
            };
            openFileDialog.ShowDialog();
            var dir = AppData.GetAppDataSubdir("presets");
            foreach (string file in openFileDialog.FileNames)
            {
                try
                {
                    File.Copy(file, Path.Combine(dir, Path.GetFileName(file)), true);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to copy preset file {file}:\n {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

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

            bool discordAllowHW = Settings.settings.FromKey("reika.presets.discord.allowHW").GetBool();
            var h264HwList = new List<string> { "h264_nvenc", "h264_amf", "h264_qsv" };
            var h265HwList = new List<string> { "hevc_nvenc", "hevc_amf", "hevc_qsv" };
            var av1HwList = new List<string> { "av1_nvenc", "av1_amf", "av1_qsv" };

            var discordH264 = discordAllowHW ? h264HwList.Append("libx264").ToList()
                : new List<string> { "libx264" };
            presets.Add(new DiscordPreset("Discord 10MB Fast (H264)",
                discordH264, 
                Utils.Megabytes(discordAllowHW ? 8.8 : 9.7))
            );
            presets.Add(new DiscordPreset("Discord 50MB Fast (H264)",
                discordH264, 
                Utils.Megabytes(discordAllowHW ? 45 : 48))
            );

            var discordH265 = discordAllowHW ? h265HwList.Append("libx265").ToList()
                : new List<string> { "libx265" };
            presets.Add(new DiscordPreset("Discord 10MB Quality (H265)",
                discordH265,
                Utils.Megabytes(discordAllowHW ? 8.8 : 9.7))
            );
            presets.Add(new DiscordPreset("Discord 50MB Quality (H265)",
                discordH265,
                Utils.Megabytes(discordAllowHW ? 45 : 48))
            );

            var discordAV1 = discordAllowHW ? av1HwList.Append("libsvtav1").ToList()
                : new List<string> { "libsvtav1" };
            presets.Add(new DiscordPreset("Discord 10MB Extra quality (AV1)",
                discordAV1,
                Utils.Megabytes(discordAllowHW ? 8.8 : 9.7))
            );
            presets.Add(new DiscordPreset("Discord 50MB Extra quality (AV1)",
                discordAV1,
                Utils.Megabytes(discordAllowHW ? 45 : 48))
            );

            presets.Add(new DiscordPreset("Discord 10MB VP9", new List<string> { "libvpx-vp9", "vp9_qsv", "vp9" }, Utils.Megabytes(9.5)));
            presets.Add(new DiscordPreset("Discord 50MB VP9", new List<string> { "libvpx-vp9", "vp9_qsv", "vp9" }, Utils.Megabytes(48)));
            presets.Add(new CustomTargetSizePreset());
            presets.Add(new CreateFilePreset
            {
                name = "H264: Moderate",
                vbitrate = "12000k",
                requiredExtension = ".mp4",
                vcodecs = new List<string> { "hevc_nvenc", "hevc_amf", "libx265" },
                acodec = "copy",
                abitrate = ""
            });
            presets.Add(new CreateFilePreset
            {
                name = "H265: Quality",
                vbitrate = "12000k",
                requiredExtension = ".mp4",
                vcodecs = new List<string> { "hevc_nvenc", "hevc_amf", "libx265" },
                acodec = "copy",
                abitrate = ""
            });
            presets.Add(new CreateFilePreset
            {
                name = "H265: Moderate",
                vbitrate = "8000k",
                requiredExtension = ".mp4",
                vcodecs = new List<string> { "hevc_nvenc", "hevc_amf", "libx265" },
                acodec = "copy",
                abitrate = ""
            });
            presets.Add(new CreateFilePreset
            {
                name = "H265: File size",
                vbitrate = "4000k",
                requiredExtension = ".mp4",
                vcodecs = new List<string> { "hevc_nvenc", "hevc_amf", "libx265" },
                acodec = "copy",
                abitrate = ""
            });
            presets.Add(new CreateFilePreset
            {
                name = "H266: 2mpbs",
                vbitrate = "2000k",
                requiredExtension = ".mp4",
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
