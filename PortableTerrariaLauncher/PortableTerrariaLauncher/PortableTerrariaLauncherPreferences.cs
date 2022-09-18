using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace Sahlaysta.PortableTerrariaLauncher
{
    //manages the prefs.xml file
    class PortableTerrariaLauncherPreferences : IDisposable
    {
        // preferences + serialization
        [AttributeUsage(AttributeTargets.Property)]
        class SerializablePreferenceAttribute : Attribute { }
        [DataContract]
        class Preference
        {
            string _name;
            object _val;
            public Preference() : this(null, null) { }
            public Preference(string name, object value)
            {
                _name = name;
                _val = value;
            }
            [DataMember]
            public string Name { get => _name; set => _name = value; }
            [DataMember]
            public object Value { get => _val; set => _val = value; }
        }

        FileStream fs;
        static readonly string prefsFilePath =
            Path.Combine(FileHelper.ApplicationFolder, "prefs.xml");
        static readonly string documentsFolder =
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        static readonly string defaultSaveDir =
            Path.Combine(Path.Combine(documentsFolder, "My Games", "Terraria"));
        static readonly PortableTerrariaLauncherPreferences defaultPrefs
            = new PortableTerrariaLauncherPreferences(null);

        //constructor
        PortableTerrariaLauncherPreferences(FileStream fileStream)
        {
            fs = fileStream;
        }
        public static PortableTerrariaLauncherPreferences Read()
        {
            string filePath = prefsFilePath;

            //r prefsfile
            try
            {
                var tp = new PortableTerrariaLauncherPreferences(
                    File.OpenRead(filePath));
                using (tp)
                {
                    try
                    {
                        tp.readPrefs();
                    }
                    catch (Exception)
                    {
                        return DefaultPreferences;
                    }

                    return tp;
                }
            }
            catch (FileNotFoundException)
            {
                return DefaultPreferences;
            }
            catch (DirectoryNotFoundException)
            {
                return DefaultPreferences;
            }
        }
        public static PortableTerrariaLauncherPreferences OpenReadWrite()
        {
            string filePath = prefsFilePath;

            //rw prefsfile
            FileHelper.CreateDirectoryOfFilePath(filePath);
            var fs = new FileStream(
                filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            var tp = new PortableTerrariaLauncherPreferences(fs);

            //read exising preferences, if exists
            try
            {
                tp.readPrefs();
            }
            catch (Exception) { }

            //truncate
            fs.Position = 0;
            fs.SetLength(0);

            return tp;
        }

        //preference properties (and default values)
        [SerializablePreference]
        public bool IsTerrariaInstalled { get; set; } = false;

        [SerializablePreference]
        public string TerrariaSaveDirectory { get; set; } = defaultSaveDir;

        //public operations
        public static PortableTerrariaLauncherPreferences DefaultPreferences
        {
            get => defaultPrefs;
        }
        public void WritePrefs()
        {
            if (fs == null || !fs.CanWrite)
                return;

            //truncate
            fs.Position = 0;
            fs.SetLength(0);

            //write prefs
            writePrefs();
            fs.Flush();
        }
        public void Dispose()
        {
            if (fs == null)
                return;
            WritePrefs();
            fs.Dispose();
            fs = null;
        }


        //write prefs
        void writePrefs()
        {
            //serializable preferences
            var serialProperties = getSerialProperties();
            List<Preference> data =
                serialProperties.Select(
                    p => new Preference(p.Name, p.GetValue(this)))
                .ToList();

            //serialize xml
            var settings = new XmlWriterSettings()
            {
                Indent = true
            };
            using (var sw = new StreamWriter(fs, Encoding.UTF8, 4096, true))
            using (var xw = XmlWriter.Create(sw, settings))
            {
                var dcs = new DataContractSerializer(typeof(List<Preference>));
                dcs.WriteObject(xw, data);
            }
        }

        // read prefs
        void readPrefs()
        {
            List<Preference> data;

            //deserialize xml
            using (var sr = new StreamReader(fs, Encoding.UTF8, false, 4096, true))
            using (var xr = XmlReader.Create(sr))
            {
                var dcs = new DataContractSerializer(typeof(List<Preference>));
                data = (List<Preference>)dcs.ReadObject(xr);
            }

            //apply values
            var serialProperties = getSerialProperties();
            foreach (var item in data)
            {
                string name = item.Name;
                var property = serialProperties
                    .FirstOrDefault(p => p.Name == name);
                if (property != null)
                {
                    property.SetValue(this, item.Value);
                }
            }
        }

        //get properties with the SerializablePreference attribute
        static IEnumerable<PropertyInfo> getSerialProperties()
        {
            return
                typeof(PortableTerrariaLauncherPreferences).GetProperties()
                .Where(property => property.GetCustomAttributes(
                    typeof(SerializablePreferenceAttribute),
                    true)
                        .Any());
        }
    }
}
