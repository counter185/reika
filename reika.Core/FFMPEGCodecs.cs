using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace reika.Core
{
    public static class FFMPEGCodecs
    {
        public static List<FFMPEG.CodecInfo> decoders;
        public static List<FFMPEG.CodecInfo> encoders;

        public static void LoadCodecList()
        {
            decoders = FFMPEG.GetAvailableDecoders();
            encoders = FFMPEG.GetAvailableEncoders();

            encoders.Insert(0, new FFMPEG.CodecInfo
            {
                Name = "Copy (same as source)",
                ID = "copy",
                Type = FFMPEG.CodecType.Video,
            });

            encoders.Insert(0, new FFMPEG.CodecInfo
            {
                Name = "Copy (same as source)",
                ID = "copy",
                Type = FFMPEG.CodecType.Audio,
            });
        }

        public static void TestHWEncoders(IOperationEntryUI progressCallback)
        {
            progressCallback.SetTextPrimary("Testing HW encoders");
            progressCallback.SetTextSecondary("");
            progressCallback.SetTextSecondary2("");
            string[] hwEncKeywords = new string[]
            {
                "nvenc", "amf", "qsv", "vaapi", "_mf", "_vulkan", "d3d1"
            };
            List<FFMPEG.CodecInfo> encodersCopy = encoders.ToList();
            var targetEncoders = encodersCopy.Where(x => hwEncKeywords.Any(y => x.ID.Contains(y))).ToList();
            List<string> compatible = new List<string>(), incompatible = new List<string>();
            int i = 0;
            foreach (var enc in targetEncoders)
            {
                progressCallback.SetTextSecondary(Utils.SanitizeForXAML(enc.ID));
                progressCallback.SetProgress(100 * ((double)(i++) / targetEncoders.Count));
                string[] args =
                {
                    "-loglevel", "error",
                    "-f", "lavfi",
                    "-i", (enc.Type == FFMPEG.CodecType.Video ? "color=black:s=640x360" : "sine=frequency=1000:duration=1"),
                    (enc.Type == FFMPEG.CodecType.Video ? "-vframes 1" : ""),
                    (enc.Type == FFMPEG.CodecType.Video ? "-an" : ""),
                    (enc.Type == FFMPEG.CodecType.Video ? "-c:v" : "-c:a"), enc.ID,
                    "-f", "null",
                    "-"
                };
                List<string> output = FFMPEG.RunFFMPEGCommandlineForOutput(args);
                if (output.Any(x => x.ToLower().Contains("error")))
                {
                    incompatible.Add(enc.ID);
                    progressCallback.SetTextSecondary2($"compat. {compatible.Count}/{incompatible.Count} incompat.");
                    encodersCopy.Remove(enc);
                }
                else
                {
                    compatible.Add(enc.ID);
                }
            }
            Console.WriteLine($"Compatible HW encoders:\n{string.Join("\n", compatible)}");
            Console.WriteLine($"Incompatible HW encoders:\n{string.Join("\n", incompatible)}");
            encoders = encodersCopy;
        }
    }
}
