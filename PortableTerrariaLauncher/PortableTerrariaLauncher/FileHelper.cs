using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sahlaysta.PortableTerrariaLauncher
{
    //utils for system.io
    static class FileHelper
    {
        // %localappdata%\PortableTerrariaLauncher\
        public static string ApplicationFolder { get => ptlPath; }
        public static string DirectXAudioFolder { get => dxAudioPath; }

        static readonly string localAppData =
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        static readonly string ptlPath =
            Path.Combine(localAppData, "PortableTerrariaLauncher");
        static readonly string dxAudioPath =
            Path.Combine(localAppData, "DirectXAudio");

        // Create the directory for a file, to be able to create it
        public static void CreateDirectoryOfFilePath(string filePath)
        {
            if (filePath == null)
                throw new ArgumentNullException("Null file path");
            string dir = Path.GetDirectoryName(filePath);
            if (dir != null && dir.Length > 0)
                Directory.CreateDirectory(dir);
        }

        // Deletes a directory, deleting its contents first
        public static void DeleteDirectory(
            string dir,
            Action<int, int> progressChanged = null,
            Func<bool> requestCancel = null)
        {
            //get dir map
            var dirs = new List<(int, string)>
            {
                (0, dir)
            };
            addDirs(dir, dirs, 1);

            //sort by depth (nested folders first)
            dirs = dirs.OrderByDescending(o => o.Item1).ToList();

            //get all files in all folders
            var allFiles = dirs.SelectMany(d => Directory.EnumerateFiles(d.Item2));

            //progress
            int count = 0;
            int total = allFiles.Count() + dirs.Count();

            //delete all files
            foreach (var file in allFiles)
            {
                if (requestCancel != null && requestCancel())
                    return;
                count++;
                File.Delete(file);
                progressChanged?.Invoke(count, total);
            }

            //delete all folders
            foreach (var d in dirs)
            {
                if (requestCancel != null && requestCancel())
                    return;
                count++;
                Directory.Delete(d.Item2);
                progressChanged?.Invoke(count, total);
            }
        }
        static void addDirs(string dir, IList<(int, string)> dirs, int depth)
        {
            foreach (var d in Directory.EnumerateDirectories(dir))
            {
                dirs.Add((depth, d));
                addDirs(d, dirs, depth + 1);
            }
        }
    }
}
