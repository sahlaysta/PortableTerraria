using System;
using System.IO;

namespace Sahlaysta.PortableTerrariaCommon
{

    /// <summary>
    /// A case-insensitive and system-safe solution for testing path equality
    /// and finding the relation between two paths.
    /// </summary>
    internal static class PathRelativity
    {

        public enum Result
        {
            Path1IsInsidePath2,
            Path2IsInsidePath1,
            Equal,
            Unrelated,
            Error
        }

        public static PathRelativity.Result RelatePaths(string path1, string path2)
        {
            try
            {

                //(get the full absolute path, without any trailing backslash!)
                string fullPath1 = Directory.GetParent(Path.Combine(Path.GetFullPath(path1), "a")).FullName;
                string fullPath2 = Directory.GetParent(Path.Combine(Path.GetFullPath(path2), "a")).FullName;

                //Uris provide a safe way to check path equality
                Uri uri1 = new Uri(fullPath1, UriKind.Absolute);
                Uri uri2 = new Uri(fullPath2, UriKind.Absolute);

                if (uri1 == uri2)
                {
                    return PathRelativity.Result.Equal;
                }

                DirectoryInfo directoryInfo;

                directoryInfo = Directory.GetParent(fullPath2);
                while (directoryInfo != null)
                {
                    if (uri1 == new Uri(directoryInfo.FullName, UriKind.Absolute))
                    {
                        return PathRelativity.Result.Path2IsInsidePath1;
                    }
                    directoryInfo = Directory.GetParent(directoryInfo.FullName);
                }

                directoryInfo = Directory.GetParent(fullPath1);
                while (directoryInfo != null)
                {
                    if (uri2 == new Uri(directoryInfo.FullName, UriKind.Absolute))
                    {
                        return PathRelativity.Result.Path1IsInsidePath2;
                    }
                    directoryInfo = Directory.GetParent(directoryInfo.FullName);
                }

                return PathRelativity.Result.Unrelated;
            }
            catch
            {
                return PathRelativity.Result.Error;
            }
        }

    }
}