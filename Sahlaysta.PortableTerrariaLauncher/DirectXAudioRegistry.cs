using System;
using System.IO;
using Microsoft.Win32;

namespace Sahlaysta.PortableTerrariaLauncher
{

    /// <summary>
    /// Registers XAudio2_6.dll and xactengine3_6.dll DLLs to the HKCU registry.
    /// </summary>
    internal static class DirectXAudioRegistry
    {

        public static void RegisterAudioDllsToSystemRegistry(
            string xaudio26dllFilepath,
            string xactengine36dllFilepath)
        {
            if (xaudio26dllFilepath == null || xactengine36dllFilepath == null)
            {
                throw new ArgumentNullException();
            }

            if (!File.Exists(xaudio26dllFilepath))
            {
                throw new Exception("File does not exist: " + xaudio26dllFilepath);
            }

            if (!File.Exists(xactengine36dllFilepath))
            {
                throw new Exception("File does not exist: " + xactengine36dllFilepath);
            }

            xaudio26dllFilepath = Path.GetFullPath(xaudio26dllFilepath);
            xactengine36dllFilepath = Path.GetFullPath(xactengine36dllFilepath);

            using (RegistryKey hkcu = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry32))
            {
                using (RegistryKey clsid = hkcu.CreateSubKey(@"Software\Classes\CLSID\", true))
                {
                    using (RegistryKey key = clsid.CreateSubKey("{3eda9b49-2085-498b-9bb2-39a6778493de}", true))
                    {
                        key.SetValue(null, "XAudio2");
                        using (RegistryKey ips32 = key.CreateSubKey("InProcServer32", true))
                        {
                            ips32.SetValue(null, xaudio26dllFilepath);
                            ips32.SetValue("ThreadingModel", "Both");
                        }
                    }
                    using (RegistryKey key = clsid.CreateSubKey("{cecec95a-d894-491a-bee3-5e106fb59f2d}", true))
                    {
                        key.SetValue(null, "AudioReverb");
                        using (RegistryKey ips32 = key.CreateSubKey("InProcServer32", true))
                        {
                            ips32.SetValue(null, xaudio26dllFilepath);
                            ips32.SetValue("ThreadingModel", "Both");
                        }
                    }
                    using (RegistryKey key = clsid.CreateSubKey("{e48c5a3f-93ef-43bb-a092-2c7ceb946f27}", true))
                    {
                        key.SetValue(null, "AudioVolumeMeter");
                        using (RegistryKey ips32 = key.CreateSubKey("InProcServer32", true))
                        {
                            ips32.SetValue(null, xaudio26dllFilepath);
                            ips32.SetValue("ThreadingModel", "Both");
                        }
                    }
                    using (RegistryKey key = clsid.CreateSubKey("{248d8a3b-6256-44d3-a018-2ac96c459f47}", true))
                    {
                        key.SetValue(null, "XACT Engine");
                        using (RegistryKey ips32 = key.CreateSubKey("InProcServer32", true))
                        {
                            ips32.SetValue(null, xactengine36dllFilepath);
                            ips32.SetValue("ThreadingModel", "Both");
                        }
                    }
                }
            }
        }

    }
}