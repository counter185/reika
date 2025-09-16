using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Xml;
using System.Xml.Linq;

namespace ReencGUI
{
    public class Settings
    {
        public List<SettingsValue> settingsValues = new List<SettingsValue>
        {
            new SettingsValue ( "reika.presets.discord.useOpusInsteadOfAAC", "Discord presets: use opus audio instead of AAC", false ),
            new SettingsValue ( "reika.presets.sizetarget.videoCodec", "Custom file size target: video codec to use", "")
        };

        public class SettingsValue
        {
            public string Key { get; private set; }

            private string _Name;
            public string Name { get => _Name; private set => _Name = value + "   "; }    //datagrid cannot do padding so we do a little trolling

            private string _Value;
            public string Value { 
                get => _Value;
                set { 
                    if (ValidateValueFunc == null || ValidateValueFunc(value))
                    {
                        _Value = value;
                    } 
                } 
            }

            public Func<string, bool> ValidateValueFunc = null;
            

            public SettingsValue(string k, string n, string v)
            {
                Key = k; Name = n;
                Value = v;
            }
            public SettingsValue(string k, string n, bool v)
            {
                Key = k; Name = n;
                ValidateValueFunc = (val) => val == "0" || val == "1";
                Value = v ? "1" : "0";
            }

            public bool GetBool() => Value == "1";
            public string GetString() => Value;
        }

        private static Settings settingsInstance = null;
        public static Settings settings
        {
            get
            {
                if (settingsInstance == null)
                {
                    TryLoad();
                }
                return settingsInstance;
            }
            private set
            {
                settingsInstance = value;
            }
        }

        public static string settingsFilePath { 
            get {
                return Path.Combine(AppData.GetAppDataPath(), "settings.xml");
            }
        }

        public SettingsValue FromKey(string key) => settingsValues.Where(x => x.Key == key).FirstOrDefault() ?? new SettingsValue(key,"","invalid");

        public static void TryLoad()
        {
            settings = new Settings();
            string settingsPath = settingsFilePath;
            try
            {
                if (File.Exists(settingsPath))
                {
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.Load(settingsPath);

                    foreach (XmlElement v in xmlDoc.SelectSingleNode("ReikaSettings").SelectNodes("Setting"))
                    {
                        try
                        {
                            string key = v.GetAttribute("key");
                            string value = v.InnerText;
                            settings.FromKey(key).Value = value;
                        }
                        catch (Exception) { }
                    }
                }
            } catch (Exception ex)
            {
                Console.WriteLine($"Error reading settings: {ex.Message}");
            }

        }

        public bool Save()
        {
            try
            {
                string settingsPath = settingsFilePath;

                XmlDocument doc = new XmlDocument();
                XmlElement root = doc.CreateElement("ReikaSettings");
                doc.AppendChild(root);

                foreach (var settingsValue in settingsValues)
                {
                    XmlElement nnode = doc.CreateElement("Setting");
                    nnode.SetAttribute("key", settingsValue.Key);
                    nnode.InnerText = settingsValue.Value;
                    root.AppendChild(nnode);
                }

                doc.Save(settingsPath);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
