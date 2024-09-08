using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Sahlaysta.PortableTerrariaCommon;

namespace Sahlaysta.PortableTerrariaCreator
{

    /// <summary>
    /// Creates a Portable Terraria Launcher EXE.
    /// 
    /// How it works:
    /// - Programmatically archive the entire Terraria directory (with DotNetZip)
    /// - Allocate the archive into many small blocks/splits (this avoids the memory problem with Mono.Cecil)
    /// - Embed into Portable Terraria Launcher's assembly
    /// - Write the new modified assembly to an EXE file
    /// 
    /// Uses temp file to avoid loading the entire archive into memory.
    /// </summary>
    internal static class GuiLauncherAssemblyWriter
    {

        public delegate void EndedCallback(Exception exception);

        public delegate void ProgressCallback(int dataProcessed, int dataTotal);

        public static void RunInNewThread(
            Assembly dotNetZipAssembly,
            Assembly cecilAssembly,
            byte[] portableTerrariaLauncherAssembly,
            string exeOutFile,
            string terrariaDir,
            Dictionary<TerrariaDllResolver.Dll, string> dllFilepaths,
            EndedCallback endedCallback,
            ProgressCallback progressCallback)
        {
            new Thread(() =>
            {
                Exception exception = null;
                try
                {
                    Run(dotNetZipAssembly, cecilAssembly, portableTerrariaLauncherAssembly, exeOutFile,
                        terrariaDir, dllFilepaths, progressCallback);
                }
                catch (Exception e)
                {
                    exception = e;
                }

                if (endedCallback != null)
                {
                    try
                    {
                        endedCallback(exception);
                    }
                    catch (Exception e)
                    {
                        Console.Error.WriteLine(e);
                    }
                }
                else
                {
                    if (exception != null)
                    {
                        Console.Error.WriteLine(exception);
                    }
                }
            }).Start();
        }

        private static void Run(
            Assembly dotNetZipAssembly,
            Assembly cecilAssembly,
            byte[] portableTerrariaLauncherAssembly,
            string exeOutFile,
            string terrariaDir,
            Dictionary<TerrariaDllResolver.Dll, string> dllFilepaths,
            ProgressCallback progressCallback)
        {
            if (dotNetZipAssembly == null || cecilAssembly == null || portableTerrariaLauncherAssembly == null
                || exeOutFile == null || terrariaDir == null)
            {
                throw new ArgumentNullException();
            }

            if (!Directory.Exists(terrariaDir))
                throw new ArgumentException("Directory does not exist: " + terrariaDir);

            dllFilepaths =
                dllFilepaths == null ? null : new Dictionary<TerrariaDllResolver.Dll, string>(dllFilepaths);

            if (dllFilepaths != null)
            {
                foreach (KeyValuePair<TerrariaDllResolver.Dll, string> entry in dllFilepaths)
                {
                    if (entry.Key == null)
                    {
                        throw new ArgumentException("Null key in dictionary");
                    }
                    if (entry.Value == null)
                    {
                        throw new ArgumentException("Null value in dictionary");
                    }
                    if (!File.Exists(entry.Value))
                    {
                        throw new ArgumentException("File does not exist: " + entry.Value);
                    }
                }
            }

            const int MaxTotalResourcesSize = 2000000000;
            const int SplitSize             = 1000000;
            const int NumberOfSplits =
                MaxTotalResourcesSize / SplitSize + (MaxTotalResourcesSize % SplitSize == 0 ? 0 : 1);

            List<string> entryNames = new List<string>();
            entryNames.Add("Terraria/");
            entryNames.Add("Dlls/");
            Dictionary<string, string> entryNamesToFilepath = new Dictionary<string, string>();
            GetDirectoryZipEntriesRecursively(terrariaDir, "Terraria/", entryNames, entryNamesToFilepath);
            if (dllFilepaths != null)
            {
                foreach (KeyValuePair<TerrariaDllResolver.Dll, string> entry in dllFilepaths)
                {
                    TerrariaDllResolver.Dll dll = entry.Key;
                    string dllFilepath = entry.Value;
                    string entryName = "Dlls/" + dll.Name;
                    entryNames.Add(entryName);
                    entryNamesToFilepath.Add(entryName, dllFilepath);
                }
            }

            if (entryNamesToFilepath.Values.Sum(x => new FileInfo(x).Length) > 2000000000)
            {
                throw new Exception("Directory is greater than 2 GB");
            }

            DotNetZip.DelegateWriteEntry entryWriter =
                (string entryName, out Stream stream, out bool closeStream) =>
                {
                    string file = entryNamesToFilepath[entryName];
                    stream = File.OpenRead(file);
                    closeStream = true;
                };

            DotNetZip.ProgressCallback dotNetZipProgressCallback;
            if (progressCallback == null)
            {
                dotNetZipProgressCallback = null;
            }
            else
            {
                dotNetZipProgressCallback = (entriesProcessed, entriesTotal) =>
                {
                    progressCallback(entriesProcessed, entriesTotal);
                };
            }

            Stream pipedZipStream = ThreadStreamPiper.ReadPipedWriteInNewThread((stream) =>
            {
                DotNetZip.WriteZipArchive(dotNetZipAssembly, stream, entryNames.ToArray(), entryWriter,
                    dotNetZipProgressCallback);
            });

            string exeTempFile = exeOutFile + ".tmp1";
            string cecilMemoryTempFile = exeOutFile + ".tmp2";

            using (pipedZipStream)
            using (TempFileStream exeOutStream = new TempFileStream(exeOutFile))
            using (TempFileStream exeTempStream = new TempFileStream(exeTempFile))
            using (TempFileStream cecilMemoryStream = new TempFileStream(cecilMemoryTempFile))
            {

                string launcherGuid = Guid.NewGuid().ToString();

                Cecil.DelegateRead assemblyIn = (out Stream stream, out bool closeStream) =>
                {
                    stream = new MemoryStream(portableTerrariaLauncherAssembly);
                    closeStream = true;
                };

                Cecil.DelegateWrite assemblyOut = (out Stream stream, out bool closeStream) =>
                {
                    stream = exeTempStream;
                    closeStream = false;
                };

                Cecil.DelegateReadWrite overrideCecilMemoryStream = (out Stream stream, out bool closeStream) =>
                {
                    stream = cecilMemoryStream;
                    closeStream = false;
                };

                List<string> addResourceNames = new List<string>();

                addResourceNames.Add("GUID");

                using (SHA256 sha256 = SHA256.Create())
                {
                    for (int i = 0; i < NumberOfSplits; i++)
                    {
                        string namePrefix = "ZIP_";

                        byte[] hash = sha256.ComputeHash(Encoding.Unicode.GetBytes(launcherGuid + i));
                        string hashAsString = string.Join("", hash.Select(x => x.ToString("X2")));
                        string nameSuffix = hashAsString;

                        string resourceName = namePrefix + nameSuffix;
                        addResourceNames.Add(resourceName);
                    }
                }

                byte[] splitHeaderBuffer = new byte[112];
                byte[] splitBuffer = new byte[SplitSize - splitHeaderBuffer.Length];
                byte[] emptyByteArray = new byte[0];
                bool wroteGuid = false;
                int splitId = 0;
                UTF8Encoding utf8Encoding = new UTF8Encoding(false, true);
                Cecil.DelegateReadResource resourceReader =
                    (string resourceName, out Stream stream, out bool closeStream) =>
                    {
                        if (resourceName.StartsWith("ZIP"))
                        {
                            int bytesRead = ReadFully(pipedZipStream, splitBuffer);
                            if (bytesRead == 0)
                            {
                                stream = new MemoryStream(emptyByteArray);
                                closeStream = true;
                            }
                            else
                            {
                                splitId += 1;

                                MemoryStream split = new MemoryStream(splitBuffer, 0, bytesRead);

                                for (int i = 0; i < splitHeaderBuffer.Length; i++) { splitHeaderBuffer[i] = 0x00; }

                                using (StreamWriter headerWriter =
                                    new StreamWriter(new MemoryStream(splitHeaderBuffer), utf8Encoding))
                                {
                                    headerWriter.Write("\"FileSplitHeader\"\0\"HeaderSize=");
                                    headerWriter.Write(splitHeaderBuffer.Length);
                                    headerWriter.Write("\"\0\"SplitId=");
                                    headerWriter.Write(splitId);
                                    headerWriter.Write("\"\0\"NumberOfSplits=");
                                    headerWriter.Write(NumberOfSplits);
                                    headerWriter.Write("\"");
                                }

                                MemoryStream header = new MemoryStream(splitHeaderBuffer);

                                stream = StreamConcatenator.ConcatenateStreams(header, split);
                                closeStream = true;
                            }
                        }
                        else if (resourceName == "GUID")
                        {
                            stream = new MemoryStream(utf8Encoding.GetBytes(launcherGuid));
                            closeStream = true;
                            wroteGuid = true;
                        }
                        else
                        {
                            throw new Exception("Bad internal name: " + resourceName);
                        }
                    };

                Cecil.AssemblyRewriteEmbeddedResources(cecilAssembly, assemblyIn, assemblyOut,
                    addResourceNames.ToArray(), null, resourceReader, overrideCecilMemoryStream);

                if (pipedZipStream.ReadByte() != -1)
                {
                    throw new Exception("Failed to write entire file");
                }

                if (!wroteGuid)
                {
                    throw new Exception("Failed to write GUID");
                }

                exeTempStream.DeleteFileOnClose = false;
            }
            File.Move(exeTempFile, exeOutFile);
        }

        private static void GetDirectoryZipEntriesRecursively(
            string dir, string entryNamePrefix,
            List<string> entryNames, Dictionary<string, string> entryNamesToFilepath)
        {
            foreach (string file in Directory.EnumerateFiles(dir))
            {
                FileInfo fileInfo = new FileInfo(file);
                string fileName = fileInfo.Name;
                string fullFilepath = fileInfo.FullName;
                string entryName = entryNamePrefix + fileName;
                entryNames.Add(entryName);
                entryNamesToFilepath.Add(entryName, fullFilepath);
            }
            foreach (string subdir in Directory.EnumerateDirectories(dir))
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(subdir);
                string directoryName = directoryInfo.Name;
                string entryName = entryNamePrefix + directoryName + "/";
                entryNames.Add(entryName);
                GetDirectoryZipEntriesRecursively(subdir, entryName, entryNames, entryNamesToFilepath);
            }
        }

        private static int ReadFully(Stream stream, byte[] buffer)
        {
            int bytesRead = 0;
            while (true)
            {
                int r = stream.Read(buffer, bytesRead, buffer.Length - bytesRead);
                if (r == 0 || bytesRead == buffer.Length)
                {
                    return bytesRead;
                }
                bytesRead += r;
            }
        }

        private class TempFileStream : FileStream
        {

            public readonly string Filepath;
            public bool DeleteFileOnClose = true;
            private bool disposed;

            public TempFileStream(string filepath) : base(filepath, FileMode.Create, FileAccess.ReadWrite)
            {
                Filepath = filepath;
            }

            protected override void Dispose(bool disposing)
            {
                if (disposed) return;
                disposed = true;
                base.Dispose(disposing);
                if (DeleteFileOnClose)
                {
                    File.Delete(Filepath);
                }
            }

        }

    }
}