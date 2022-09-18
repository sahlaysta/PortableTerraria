using Microsoft.Win32;
using Sahlaysta.PortableTerrariaCommon;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Sahlaysta.PortableTerrariaLauncher
{
    //async installs terraria
    class TerrariaInstaller : GuiProgressibleOperation
    {
        readonly string installDir;

        //constructor
        public TerrariaInstaller(string installationDirectory)
        {
            installDir = installationDirectory;
        }

        //creation + processing
        protected override void run()
        {
            //rw prefs
            var prefs = PortableTerrariaLauncherPreferences.OpenReadWrite();

            //installation
            using (prefs)
            {
                //prefs set to not installed
                prefs.IsTerrariaInstalled = false;
                prefs.WritePrefs();

                //delete install dir
                if (Directory.Exists(installDir))
                    FileHelper.DeleteDirectory(installDir);

                //process the terraria zip
                processTerrariaZip();
                
                //cancel check
                if (cancelRequested())
                {
                    //delete when canceled
                    if (Directory.Exists(installDir))
                        FileHelper.DeleteDirectory(installDir);
                    return;
                }

                //hkcu register
                if (getResourceFlag("hkcu.info.txt"))
                    hkcuRegister();

                //prefs set to installed
                prefs.IsTerrariaInstalled = true;
            }

        }
        void processTerrariaZip()
        {
            bool hasTerraria = getResourceFlag("terraria.info.txt");
            if (!hasTerraria)
                return;

            //terraria.zip stream input
            Stream input = getTerrariaZipStream();

            //script stream override
            Stream overrideStream = scriptOverrideStream(input);

            //terraria zip stream output
            string terrariaZip = Path.Combine(
                FileHelper.ApplicationFolder, "terraria.zip.tmp");
            Stream output = File.Open(
                terrariaZip, FileMode.Create, FileAccess.Write);
            using (output)
            {
                //run stream
                if (overrideStream != null
                    && !ReferenceEquals(overrideStream, input))
                {
                    using (input)
                    using (overrideStream)
                        overrideStream.CopyTo(output);
                }
                else
                {
                    using (input)
                        input.CopyTo(output);
                }
            }

            //unzip terraria zip
            output = File.OpenRead(terrariaZip);
            using (output)
            {
                unzipTerraria(output);
            }

            //delete terraria zip after extracted / canceled
            File.Delete(terrariaZip);
        }
        Stream getTerrariaZipStream()
        {
            //join split stream
            string[] names = getTerrariaZipResourceNames();
            int i = 0, len = names.Length;
            Stream getNextStream()
            {
                if (i >= len)
                    return null;
                return GuiHelper.GetResourceStream(names[i++]);
            }
            var sis = new SplitInStream(getNextStream);
            return sis;
        }
        string[] getTerrariaZipResourceNames()
        {
            //get embedded resource split files
            var result = new List<string>();
            var assembly = Assembly.GetExecutingAssembly();
            const string rsrcsp = "terraria.zip.";
            string namep = assembly.GetName().Name + '.' + rsrcsp;
            int namepLen = namep.Length;
            var names =
                GuiHelper.GetResourceNames()
                    .Where(n => n.StartsWith(namep));
            int digitCount = names.First().Length - namepLen;
            string format = 'D' + digitCount.ToString();
            int nameCount = names.Count();
            for (int i = 0; i < nameCount; i++)
            {
                string name = rsrcsp + (i + 1).ToString(format);
                result.Add(name);
            }
            return result.ToArray();
        }
        void unzipTerraria(Stream stream)
        {
            //extract zip
            Directory.CreateDirectory(installDir);
            using (var zip = DotNetZipAssembly.ZipFile.Read(stream))
            {
                //progress
                int processed = 0;
                int total = zip.Count;

                //extract entries
                foreach (var zipEntry in zip)
                {
                    //terraria installation
                    const string terrDir = "Terraria/";
                    const string dllDir = "Dlls/";

                    //terr extract
                    if (!extractZipEntry(
                        zipEntry, installDir, terrDir))
                    {
                        //dll extract
                        string tModLoaderDir = Path.Combine(
                            installDir, "tModLoader");
                        extractZipEntry(
                            zipEntry, installDir, dllDir);
                        extractZipEntry(
                            zipEntry, tModLoaderDir, dllDir);
                    }

                    //progress update
                    processed++;
                    progress(processed, total);

                    //cancel check
                    if (cancelRequested())
                        return;
                }
            }
        }
        static bool extractZipEntry(
            DotNetZipAssembly.ZipEntry zipEntry,
            string extractDir,
            string baseDir)
        {
            //ensure base directory
            string fileName = zipEntry.FileName;
            if (!fileName.Equals(
                baseDir,
                StringComparison.InvariantCultureIgnoreCase)
                &&
                fileName.StartsWith(
                baseDir,
                StringComparison.InvariantCultureIgnoreCase))
            {
                //get extract file path
                string entryPath = fileName.Substring(baseDir.Length);
                string extrPath = Path.Combine(extractDir, entryPath);

                //no overwrite
                if (File.Exists(extrPath))
                    return false;

                //extraction
                if (fileName.EndsWith("/")) //is dir
                {
                    Directory.CreateDirectory(extrPath);
                }
                else //is file
                {
                    //mkdirs
                    FileHelper.CreateDirectoryOfFilePath(extrPath);

                    //extract to file path
                    var fs = new FileStream(
                        extrPath, FileMode.Create, FileAccess.Write);
                    using (fs)
                    {
                        zipEntry.Extract(fs);
                    }
                }
                return true;
            }

            return false;
        }

        //csx scripting to override stream in
        Stream scriptOverrideStream(Stream baseStream)
        {
            bool hasScript = getResourceFlag("script.info.txt");
            if (!hasScript)
                return null;

            var scripting = Scripting.Create(
                GuiHelper.GetResourceStream("script.csx"));
            return scripting.InvokePublicStaticMethod<Stream>(
                "PortableTerrariaScript.Script",
                "OverrideTerrariaStreamIn",
                new Type[] { typeof(Stream) },
                new object[] { baseStream });
        }

        //reads bool string in resource .info.txt
        static bool getResourceFlag(string rsrcName)
        {
            using (var stream = GuiHelper.GetResourceStream(rsrcName))
            using (var sr = new StreamReader(stream))
            {
                string str = sr.ReadToEnd();
                return bool.Parse(str);
            }
        }

        // HKCU register XAudio2_6.dll and xactengine3_6.dll
        void hkcuRegister()
        {
            //XAudio2_6.dll and xactengine3_6.dll must exist
            string srcxAudio2_6_dll = Path.Combine(
                installDir, "XAudio2_6.dll");
            string srcxactengine3_6_dll = Path.Combine(
                installDir, "xactengine3_6.dll");
            if (!File.Exists(srcxAudio2_6_dll) ||
                !File.Exists(srcxactengine3_6_dll))
            {
                return;
            }

            //check registered
            string dllDir = FileHelper.DirectXAudioFolder;
            string registered = Path.Combine(dllDir, "registered");
            if (File.Exists(registered))
                return;

            //copy dlls
            Directory.CreateDirectory(dllDir);
            string newxAudio2_6_dll = Path.Combine(
                dllDir, Path.GetFileName(srcxAudio2_6_dll));
            string newxactengine3_6_dll = Path.Combine(
                dllDir, Path.GetFileName(srcxactengine3_6_dll));
            File.Copy(srcxAudio2_6_dll, newxAudio2_6_dll, true);
            File.Copy(srcxactengine3_6_dll, newxactengine3_6_dll, true);

            //register dlls
            registerAudioDlls(newxAudio2_6_dll, newxactengine3_6_dll);

            //file that tells its registered
            new FileStream(registered, FileMode.Create, FileAccess.Write);
        }
        //registers XAudio2_6.dll and xactengine3_6.dll to HKCU
        static void registerAudioDlls(
            string xAudio26dllFilePath, string xactengine36dllFilePath)
        {
            string xAudio2_6_dll = xAudio26dllFilePath;
            string xactengine3_6_dll = xactengine36dllFilePath;
            var hkcu = RegistryKey.OpenBaseKey(
                RegistryHive.CurrentUser, RegistryView.Registry32);
            using (hkcu)
            {
                var clsid = hkcu.CreateSubKey(
                    @"Software\Classes\CLSID\", true);
                using (clsid)
                {
                    //3eda
                    var key3eda = clsid.CreateSubKey(
                        "{3eda9b49-2085-498b-9bb2-39a6778493de}", true);
                    using (key3eda)
                    {
                        key3eda.SetValue(null, "XAudio2");

                        //ips32
                        var key3eda_ips32 = key3eda.CreateSubKey(
                            "InProcServer32", true);
                        using (key3eda_ips32)
                        {
                            key3eda_ips32.SetValue(null, xAudio2_6_dll);
                            key3eda_ips32.SetValue("ThreadingModel", "Both");
                        }
                    }

                    //cece
                    var keycece = clsid.CreateSubKey(
                        "{cecec95a-d894-491a-bee3-5e106fb59f2d}", true);
                    using (keycece)
                    {
                        keycece.SetValue(null, "AudioReverb");

                        //ips32
                        var keycece_ips32 = keycece.CreateSubKey(
                            "InProcServer32", true);
                        using (keycece_ips32)
                        {
                            keycece_ips32.SetValue(null, xAudio2_6_dll);
                            keycece_ips32.SetValue("ThreadingModel", "Both");
                        }
                    }

                    //e48c
                    var keye48c = clsid.CreateSubKey(
                        "{e48c5a3f-93ef-43bb-a092-2c7ceb946f27}", true);
                    using (keye48c)
                    {
                        keye48c.SetValue(null, "AudioVolumeMeter");

                        //ips32
                        var keye48c_ips32 = keye48c.CreateSubKey(
                            "InProcServer32", true);
                        using (keye48c_ips32)
                        {
                            keye48c_ips32.SetValue(null, xAudio2_6_dll);
                            keye48c_ips32.SetValue("ThreadingModel", "Both");
                        }
                    }

                    //248d
                    var key248d = clsid.CreateSubKey(
                        "{248d8a3b-6256-44d3-a018-2ac96c459f47}", true);
                    using (key248d)
                    {
                        key248d.SetValue(null, "XACT Engine");

                        //ips32
                        var key248d_ips32 = key248d.CreateSubKey(
                            "InProcServer32", true);
                        using (key248d_ips32)
                        {
                            key248d_ips32.SetValue(null, xactengine3_6_dll);
                            key248d_ips32.SetValue("ThreadingModel", "Both");
                        }
                    }
                }
            }
        }
    }
}
