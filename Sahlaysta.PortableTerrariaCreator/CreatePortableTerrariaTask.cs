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
    internal static class CreatePortableTerrariaTask
    {

        public delegate void EndedCallback(Exception exception);

        public delegate void ProgressCallback(int nDataProcessed, int nDataTotal);

        public static void RunInNewThread(
            Assembly dotNetZipAssembly,
            Assembly cecilAssembly,
            byte[] portableTerrariaLauncherAssembly,
            string exeOutFilePath,
            string terrariaDir,
            string[] dllFilePaths,
            EndedCallback endedCallback,
            ProgressCallback progressCallback)
        {
            new Thread(() =>
            {
                Exception exception = null;
                try
                {
                    Run(dotNetZipAssembly, cecilAssembly, portableTerrariaLauncherAssembly, exeOutFilePath,
                        terrariaDir, dllFilePaths, progressCallback);
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
            string exeOutFilePath,
            string terrariaDir,
            string[] dllFilePaths,
            ProgressCallback progressCallback)
        {
            if (dotNetZipAssembly == null || cecilAssembly == null || portableTerrariaLauncherAssembly == null
                || exeOutFilePath == null || terrariaDir == null)
            {
                throw new ArgumentException("Null");
            }

            if (!Directory.Exists(terrariaDir))
                throw new ArgumentException("Directory does not exist: " + terrariaDir);

            dllFilePaths = (string[])(dllFilePaths ?? new string[] { }).Clone();

            if (dllFilePaths.Contains(null))
                throw new ArgumentException("Null");

            foreach (string dllFilePath in dllFilePaths)
                if (!File.Exists(dllFilePath))
                    throw new ArgumentException("File does not exist: " + dllFilePath);

            const int MaxTotalResourcesSize = 2000000000;
            const int SplitSize             = 1000000;
            const int NumberOfSplits =
                MaxTotalResourcesSize / SplitSize + (MaxTotalResourcesSize % SplitSize == 0 ? 0 : 1);

            List<string> entryNames = new List<string>();
            Dictionary<string, string> entryNamesToFilePath = new Dictionary<string, string>();
            GetDirectoryZipEntriesRecursively(terrariaDir, entryNames, entryNamesToFilePath);

            if (entryNamesToFilePath.Values.Sum(x => new FileInfo(x).Length) > 2000000000)
            {
                throw new Exception("Directory is greater than 2 GB");
            }

            DotNetZip.DelegateWriteEntry entryWriter = (entryName, stream) =>
            {
                string fullFilePath = entryNamesToFilePath[entryName];
                using (FileStream fileStream = File.OpenRead(fullFilePath))
                {
                    fileStream.CopyTo(stream);
                }
            };

            DotNetZip.ProgressCallback dotNetZipProgressCallback;
            if (progressCallback == null)
            {
                dotNetZipProgressCallback = null;
            }
            else
            {
                dotNetZipProgressCallback = (nEntriesProcessed, nEntriesTotal) =>
                {
                    progressCallback(nEntriesProcessed, nEntriesTotal);
                };
            }

            Stream pipedZipStream = ThreadStreamPiper.ReadPipedWriteInNewThread((stream) =>
            {
                DotNetZip.WriteZipArchive(dotNetZipAssembly, stream, entryNames.ToArray(), entryWriter,
                    dotNetZipProgressCallback);
            });

            string exeTempFile = exeOutFilePath + ".tmp1";
            string cecilMemoryTempFile = exeOutFilePath + ".tmp2";

            using (pipedZipStream)
            using (TempFileStream exeOutStream = new TempFileStream(exeTempFile))
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
                    stream = exeOutStream;
                    closeStream = false;
                };

                Cecil.DelegateReadWrite overrideCecilMemoryStream = (out Stream stream, out bool closeStream) =>
                {
                    stream = cecilMemoryStream;
                    closeStream = false;
                };

                List<string> addResourceNames = new List<string>();

                addResourceNames.Add("GUID");

                for (int i = 0; i < NumberOfSplits; i++)
                {
                    addResourceNames.Add("ZIP_" + HashSHA256(launcherGuid + i));
                }

                byte[] splitHeaderBuffer = new byte[80];
                byte[] splitBuffer = new byte[SplitSize - splitHeaderBuffer.Length];
                byte[] emptyByteArray = new byte[0];
                bool wroteGuid = false;
                int splitsWritten = 0;
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
                                splitsWritten += 1;

                                MemoryStream split = new MemoryStream(splitBuffer, 0, bytesRead);

                                for (int i = 0; i < splitHeaderBuffer.Length; i++) { splitHeaderBuffer[i] = 0x00; }
                                using (StreamWriter headerWriter =
                                    new StreamWriter(new MemoryStream(splitHeaderBuffer), utf8Encoding))
                                {
                                    headerWriter.Write("\"FileSplitHeader\"\0\"HeaderSize=");
                                    headerWriter.Write(splitHeaderBuffer.Length);
                                    headerWriter.Write("\"\0\"SplitID=");
                                    headerWriter.Write(splitsWritten);
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

                exeOutStream.DeleteFileOnClose = false;
            }
            File.Delete(exeOutFilePath);
            File.Move(exeTempFile, exeOutFilePath);
        }

        private static void GetDirectoryZipEntriesRecursively(
            string dir, List<string> entryNames, Dictionary<string, string> entryNamesToFilePath)
        {
            GetDirectoryZipEntriesRecursively(dir, "", entryNames, entryNamesToFilePath);
        }

        private static void GetDirectoryZipEntriesRecursively(
            string dir, string entryNamePrefix,
            List<string> entryNames, Dictionary<string, string> entryNamesToFilePath)
        {
            foreach (string file in Directory.EnumerateFiles(dir))
            {
                FileInfo fileInfo = new FileInfo(file);
                string fileName = fileInfo.Name;
                string fullFilePath = fileInfo.FullName;
                string entryName = entryNamePrefix + fileName;
                entryNames.Add(entryName);
                entryNamesToFilePath.Add(entryName, fullFilePath);
            }
            foreach (string subdir in Directory.EnumerateDirectories(dir))
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(subdir);
                string directoryName = directoryInfo.Name;
                string entryName = entryNamePrefix + directoryName + "/";
                entryNames.Add(entryName);
                GetDirectoryZipEntriesRecursively(subdir, entryName, entryNames, entryNamesToFilePath);
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

        private static readonly SHA256 SHA256 = SHA256.Create();
        private static string HashSHA256(string str)
        {
            byte[] hash = SHA256.ComputeHash(Encoding.Unicode.GetBytes(str));
            return string.Join("", hash.Select(x => x.ToString("X2")));
        }

        private class TempFileStream : FileStream
        {

            public readonly string FilePath;
            public bool DeleteFileOnClose = true;
            private bool disposed;

            public TempFileStream(string filePath) : base(filePath, FileMode.Create, FileAccess.ReadWrite)
            {
                FilePath = filePath;
            }

            override protected void Dispose(bool disposing)
            {
                if (disposed) return;
                disposed = true;
                base.Dispose(disposing);
                if (DeleteFileOnClose)
                {
                    File.Delete(FilePath);
                }
            }

        }

    }
}