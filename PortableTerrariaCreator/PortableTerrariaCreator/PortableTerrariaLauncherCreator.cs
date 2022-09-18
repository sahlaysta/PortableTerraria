using Sahlaysta.PortableTerrariaCommon;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Sahlaysta.PortableTerrariaCreator
{
    //async creates PortableTerrariaLauncher
    class PortableTerrariaLauncherCreator : GuiProgressibleOperation
    {
        //creator options
        public struct Options
        {
            readonly string _outputPath;
            readonly string _terrariaPath;
            readonly IEnumerable<TerrariaResolver.Dll> _dlls;
            readonly bool _registerHKCU;
            readonly string _scriptPath;

            //constructor
            public Options(
                string outputPath,
                string terrariaPath,
                IEnumerable<TerrariaResolver.Dll> dlls,
                bool registerHKCU,
                string scriptPath)
            {
                _outputPath = outputPath;
                _terrariaPath = terrariaPath;
                _dlls = dlls;
                _registerHKCU = registerHKCU;
                _scriptPath = scriptPath;
            }

            //public operations
            public string OutputPath { get { return _outputPath; } }
            public string TerrariaPath { get { return _terrariaPath; } }
            public IEnumerable<TerrariaResolver.Dll> Dlls { get { return _dlls; } }
            public bool RegisterHKCU { get { return _registerHKCU; } }
            public string ScriptPath { get { return _scriptPath; } }
        }

        readonly Options options;

        //constructor
        public PortableTerrariaLauncherCreator(Options options)
        {
            this.options = options;
        }

        //creation + processing
        protected override void run()
        {
            //file streams
            string outputPath = options.OutputPath;
            string outputTmpPath = outputPath + ".tmp";
            string outputTmp2Path = outputPath + ".tmp2";
            File.Delete(outputTmpPath);
            File.Delete(outputTmp2Path);
            Stream stream = File.Open(
                outputTmpPath, FileMode.CreateNew, FileAccess.Write);
            Stream tmp2Stream = File.Open(
                outputTmp2Path, FileMode.CreateNew, FileAccess.ReadWrite);

            //run streams
            MonoCecilAssembly.EmbeddedResource
                .EmbeddedResourceStream = tmp2Stream;
            using (tmp2Stream)
            {
                using (stream)
                {
                    createPTL(stream);
                }
            }
            MonoCecilAssembly.EmbeddedResource
                .EmbeddedResourceStream = null;

            //cancel requested, delete tmp files
            if (cancelRequested())
            {
                File.Delete(outputTmpPath);
                File.Delete(outputTmp2Path);
                return;
            }

            //rename tmp
            File.Delete(outputPath);
            File.Move(outputTmpPath, outputPath);

            //delete tmp2
            File.Delete(outputTmp2Path);
        }
        void createPTL(Stream stream)
        {
            //edit exe embedded resources
            using (var exe = GuiHelper.GetResourceStream(
                "Sahlaysta.PortableTerrariaLauncher.exe"))
            using (var ad = MonoCecilAssembly
                .AssemblyDefinition.ReadAssembly(exe))
            {
                //add resources
                string namep = ad.Name.Name + '.';
                var rsrcs = ad.MainModule.Resources;
                const MonoCecilAssembly.ManifestResourceAttributes mra =
                    MonoCecilAssembly.ManifestResourceAttributes.Public;

                //resource flags
                bool hkcu = options.RegisterHKCU;
                bool hasScript = options.ScriptPath != null;
                bool hasTerraria =
                    options.TerrariaPath != null || options.Dlls != null;

                //add resource flags
                var rsrcMap = new (string, bool)[]
                {
                    ("hkcu.info.txt", hkcu),
                    ("script.info.txt", hasScript),
                    ("terraria.info.txt", hasTerraria)
                };
                foreach (var mapping in rsrcMap)
                {
                    string name = namep + mapping.Item1;
                    byte[] data = Encoding.UTF8.GetBytes(mapping.Item2.ToString());

                    var er = new MonoCecilAssembly.EmbeddedResource(name, mra, data);
                    MonoCecilAssembly.Collection.AddEmbeddedResource(rsrcs, er);
                }

                //write terraria resource
                if (hasTerraria)
                {
                    //split terraria.zip into multiple resources, each max 2 mb
                    int splitSize = 2097152; //2 mb
                    long terrariaSize = //get dir size
                        options.TerrariaPath == null ? 0
                        : new DirectoryInfo(options.TerrariaPath)
                        .EnumerateFiles(
                            "*",
                            SearchOption.AllDirectories)
                        .Sum(file => file.Length);
                    long dllSize = //get dll size
                        options.Dlls == null ? 0
                        : options.Dlls.Sum(
                            d => d.Status ? ((int)new FileInfo(d.FilePath).Length) : 0);
                    dllSize *= 2; //tmodloader
                    long zipSize = terrariaSize + dllSize;
                    long splitCount = (zipSize / splitSize) + 1;//max split count
                    splitCount *= 2;//just in case

                    //get digit count for file naming leading zeros
                    long digitCount = Math.Max(2, splitCount.ToString().Length);
                    string format = 'D' + digitCount.ToString();

                    //run SplitOutStream
                    void writeDataToStream(Stream s)
                    {
                        zipTerraria(s);
                    }
                    var sos = new SplitOutStream(writeDataToStream, splitSize);
                    for (long i = 0; i < splitCount; i++)
                    {
                        string name = namep + "terraria.zip." + (i + 1).ToString(format);
                        var data = sos.Stream;

                        var er = new MonoCecilAssembly.EmbeddedResource(name, mra, data);
                        MonoCecilAssembly.Collection.AddEmbeddedResource(rsrcs, er);
                    }
                }

                //write script.csx resource
                string scriptPath = options.ScriptPath;
                if (scriptPath != null)
                {
                    string name = namep + "script.csx";
                    var ms = new MemoryStream();
                    using (ms)
                    {
                        var fs = File.OpenRead(scriptPath);
                        using (fs)
                        {
                            fs.CopyTo(ms);
                        }
                    }
                    var data = ms.ToArray();

                    var er = new MonoCecilAssembly.EmbeddedResource(name, mra, data);
                    MonoCecilAssembly.Collection.AddEmbeddedResource(rsrcs, er);
                }

                //write .exe
                ad.Write(stream);
            }
        }
        void zipTerraria(Stream stream)
        {
            //script stream override
            Stream overrideStream = scriptOverrideStream(stream);

            //run stream
            if (overrideStream != null && !ReferenceEquals(overrideStream, stream))
            {
                using (stream)
                using (overrideStream)
                    zipTerraria2(overrideStream);
            }
            else
            {
                using (stream)
                    zipTerraria2(stream);
            }
        }
        void zipTerraria2(Stream stream)
        {
            using (var zip = new DotNetZipAssembly.ZipFile())
            {
                //terraria dir
                if (options.TerrariaPath != null)
                    zip.AddDirectory(options.TerrariaPath, "Terraria");

                //dll files
                var dlls = options.Dlls;
                if (dlls != null)
                    foreach (var dll in options.Dlls)
                        if (dll.Status)
                            zip.AddFile(dll.FilePath, "Dlls");

                //write .zip
                zip.SaveProgress += (o, e) =>
                {
                    //when user cancels
                    if (cancelRequested())
                    {
                        e.Cancel = true;
                        return;
                    }

                    //progress
                    if (e.EventTypeIsSaving_AfterWriteEntry)
                    {
                        progress(e.EntriesSaved, e.EntriesTotal);
                    }
                };
                zip.Save(stream);
            }
        }

        //csx scripting to override stream out
        Stream scriptOverrideStream(Stream baseStream)
        {
            string scriptFilePath = options.ScriptPath;
            if (scriptFilePath == null)
                return null;

            var scripting = Scripting.Create(scriptFilePath);
            return scripting.InvokePublicStaticMethod<Stream>(
                "PortableTerrariaScript.Script",
                "OverrideTerrariaStreamOut",
                new Type[] { typeof(Stream) },
                new object[] { baseStream });
        }
    }
}
