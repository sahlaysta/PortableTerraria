using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Sahlaysta.PortableTerrariaCommon;

namespace Sahlaysta.PortableTerrariaLauncher
{

    /// <summary>
    /// Extracts Terraria from inside this launcher's embedded assembly resources.
    /// </summary>
    internal static class GuiTerrariaInstaller
    {

        public delegate void EndedCallback(Exception exception);

        public delegate void ProgressCallback(int dataProcessed, int dataTotal);

        public static void RunInNewThread(
            Assembly dotNetZipAssembly,
            string installDir,
            EndedCallback endedCallback,
            ProgressCallback progressCallback)
        {
            new Thread(() =>
            {
                Exception exception = null;
                try
                {
                    Run(dotNetZipAssembly, installDir, progressCallback);
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
            string installDir,
            ProgressCallback progressCallback)
        {
            if (dotNetZipAssembly == null || installDir == null)
            {
                throw new ArgumentNullException();
            }

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

            using (Stream zipStream = new GuiLauncherAssemblyReader.DotNetZipCompatibilityStream(
                GuiLauncherAssemblyReader.GetZipStream()))
            {
                string[] entryNames = DotNetZip.ReadZipArchiveEntryNames(dotNetZipAssembly, zipStream);

                zipStream.Position = 0;

                if (!entryNames.Contains("Terraria/"))
                {
                    throw new Exception("Entry name was not found: Terraria/");
                }
                if (!entryNames.Contains("Dlls/"))
                {
                    throw new Exception("Entry name was not found: Dlls/");
                }

                Directory.CreateDirectory(installDir);

                string terrariaFolderPrefix = "Terraria/";

                foreach (string entryName in entryNames
                    .Where(x => x.StartsWith(terrariaFolderPrefix) && x != terrariaFolderPrefix && x.EndsWith("/")))
                {
                    string path = Path.Combine(new string[] { installDir }
                        .Concat(entryName.Substring(terrariaFolderPrefix.Length).Split('/')).ToArray());
                    Directory.CreateDirectory(path);
                }

                string[] entryFileNames =
                    entryNames.Where(x => x.StartsWith(terrariaFolderPrefix) && !x.EndsWith("/")).ToArray();

                DotNetZip.DelegateExtractEntry entryExtractor =
                    (string entryName, out Stream stream, out bool closeStream) =>
                    {
                        string path = Path.Combine(new string[] { installDir }
                            .Concat(entryName.Substring(terrariaFolderPrefix.Length).Split('/')).ToArray());

                        string pathDirName = Path.GetDirectoryName(path);
                        if (pathDirName != null)
                        {
                            Directory.CreateDirectory(pathDirName);
                        }

                        stream = File.Open(path, FileMode.Create, FileAccess.Write);
                        closeStream = true;
                    };

                DotNetZip.ExtractZipArchive(
                    dotNetZipAssembly, zipStream, entryFileNames, entryExtractor, dotNetZipProgressCallback);

                string dllFolderPrefix = "Dlls/";

                string[] dirsToCopyDlls =
                    new string[] { installDir }.Concat(
                        Directory.EnumerateDirectories(installDir, "*", SearchOption.AllDirectories))
                    .Where(x => File.Exists(Path.Combine(x, "Terraria.exe"))
                        || File.Exists(Path.Combine(x, "tModLoader.exe")))
                    .ToArray();

                foreach (string dir in dirsToCopyDlls)
                {
                    zipStream.Position = 0;

                    string[] dllEntryNames =
                        entryNames
                        .Where(x => x.StartsWith(dllFolderPrefix) && x != dllFolderPrefix
                            && x.Count(xx => xx == '/') == 1)
                        .Where(x => !File.Exists(Path.Combine(dir, x.Substring(dllFolderPrefix.Length))))
                        .ToArray();

                    DotNetZip.DelegateExtractEntry dllEntryExtractor =
                        (string entryName, out Stream stream, out bool closeStream) =>
                        {
                            string path = Path.Combine(new string[] { dir }
                                .Concat(entryName.Substring(dllFolderPrefix.Length).Split('/')).ToArray());
                            stream = File.Open(path, FileMode.Create, FileAccess.Write);
                            closeStream = true;
                        };

                    DotNetZip.ExtractZipArchive(
                        dotNetZipAssembly, zipStream, dllEntryNames, dllEntryExtractor, null);
                }

            }
        }

    }
}