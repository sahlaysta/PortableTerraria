using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Sahlaysta.PortableTerrariaCommon
{

    /// <summary>
    /// Auto-detects the filepaths of the Terraria DLLs.
    /// </summary>
    internal static class TerrariaDllResolver
    {

        public class Dll
        {

            public readonly string Name;
            public readonly DllSource Source;

            private Dll(string name, DllSource source)
            {
                Name = name;
                Source = source;
            }

            public static readonly ReadOnlyCollection<Dll> All = new ReadOnlyCollection<Dll>(new List<Dll>
            {
                new Dll("D3DX9_33.dll", DllSource.DirectX),
                new Dll("D3DX9_41.dll", DllSource.DirectX),
                new Dll("X3DAudio1_7.dll", DllSource.DirectX),
                new Dll("xactengine3_6.dll", DllSource.DirectX),
                new Dll("XAudio2_6.dll", DllSource.DirectX),
                new Dll("xinput1_3.dll", DllSource.DirectX),
                
                new Dll("msvcp100.dll", DllSource.VisualCPP),
                new Dll("msvcr100.dll", DllSource.VisualCPP),

                new Dll("DSETUP.dll", DllSource.XnaFramework),
                new Dll("dsetup32.dll", DllSource.XnaFramework),
                new Dll("XnaNative.dll", DllSource.XnaFramework),
                new Dll("xnavisualizer.dll", DllSource.XnaFramework),
                new Dll("XnaVisualizerPS.dll", DllSource.XnaFramework),
                
                new Dll("Microsoft.Xna.Framework.dll", DllSource.XnaFramework),
                new Dll("Microsoft.Xna.Framework.Avatar.dll", DllSource.XnaFramework),
                new Dll("Microsoft.Xna.Framework.Game.dll", DllSource.XnaFramework),
                new Dll("Microsoft.Xna.Framework.GamerServices.dll", DllSource.XnaFramework),
                new Dll("Microsoft.Xna.Framework.Graphics.dll", DllSource.XnaFramework),
                new Dll("Microsoft.Xna.Framework.Input.Touch.dll", DllSource.XnaFramework),
                new Dll("Microsoft.Xna.Framework.Net.dll", DllSource.XnaFramework),
                new Dll("Microsoft.Xna.Framework.Storage.dll", DllSource.XnaFramework),
                new Dll("Microsoft.Xna.Framework.Video.dll", DllSource.XnaFramework),
                new Dll("Microsoft.Xna.Framework.Xact.dll", DllSource.XnaFramework)
            });

        }

        public enum DllSource
        {
            DirectX,
            XnaFramework,
            VisualCPP
        }

        private class ResolvedDll
        {

            public readonly Dll Dll;
            public readonly string Filepath;

            public ResolvedDll(Dll dll, string filepath)
            {
                Dll = dll;
                Filepath = filepath;
            }

        }

        private class ResolvedXnaDll
        {

            public readonly ResolvedDll ResolvedDll;
            public readonly string XnaFrameworkVersion;

            public ResolvedXnaDll(ResolvedDll resolvedDll, string xnaFrameworkVersion)
            {
                ResolvedDll = resolvedDll;
                XnaFrameworkVersion = xnaFrameworkVersion;
            }

        }

        public class XnaFrameworkVersion
        {

            public readonly string Version;

            private XnaFrameworkVersion(string version)
            {
                Version = version;
            }

            public static XnaFrameworkVersion[] FindXnaFrameworkVersionsOnSystem()
            {
                List<string> foundVersions = new List<string>();
                foreach (Dll dll in Dll.All)
                {
                    ResolvedXnaDll[] resolvedXnaDlls = ResolveXnaDlls(dll);
                    if (resolvedXnaDlls != null)
                    {
                        foreach (ResolvedXnaDll resolvedXnaDll in resolvedXnaDlls)
                        {
                            string xnaFrameworkVersion = resolvedXnaDll.XnaFrameworkVersion;
                            if (!foundVersions.Contains(xnaFrameworkVersion))
                            {
                                foundVersions.Add(xnaFrameworkVersion);
                            }
                        }
                    }
                }

                XnaFrameworkVersion[] sortedXnaFrameworkVersions =
                    foundVersions
                    .Select(x => new XnaFrameworkVersion(x))
                    .OrderByDescending(x => x, XnaFrameworkVersionComparer)
                    .ToArray();

                return sortedXnaFrameworkVersions;
            }

            public static XnaFrameworkVersion FindHighestXnaFrameworkVersionOnSystem()
            {
                XnaFrameworkVersion[] xnaFrameworkVersions = FindXnaFrameworkVersionsOnSystem();
                return xnaFrameworkVersions.Length == 0 ? null : xnaFrameworkVersions[0];
            }

            private static readonly XnaFrameworkVersionComparerImpl XnaFrameworkVersionComparer =
                new XnaFrameworkVersionComparerImpl();

            private class XnaFrameworkVersionComparerImpl : IComparer<XnaFrameworkVersion>
            {

                private static readonly Regex XnaVersionRegex = new Regex(
                    @"(?<=^v4\.0_)([0-9]{1,9})\.([0-9]{1,9})\.([0-9]{1,9})\.([0-9]{1,9})(?=__.*$)",
                    RegexOptions.Compiled);

                int IComparer<XnaFrameworkVersion>.Compare(XnaFrameworkVersion x, XnaFrameworkVersion y)
                {
                    if (object.ReferenceEquals(x, y)) return 0;

                    Match matchX = XnaVersionRegex.Match(x?.Version ?? "");
                    Match matchY = XnaVersionRegex.Match(y?.Version ?? "");

                    if (matchX.Success && !matchY.Success)
                        return 1;
                    else if (!matchX.Success && matchY.Success)
                        return -1;
                    else if (!matchX.Success && !matchY.Success)
                        return 0;

                    Version versionX = new Version(matchX.Value);
                    Version versionY = new Version(matchY.Value);
                    return versionX.CompareTo(versionY);
                }

            }

        }

        public static string FindDllFilepathOnSystem(Dll dll, XnaFrameworkVersion xnaFrameworkVersion)
        {
            if (dll == null)
            {
                return null;
            }

            ResolvedDll resolvedDll = ResolveDll(dll);
            if (resolvedDll != null)
            {
                return resolvedDll.Filepath;
            }
            
            if (xnaFrameworkVersion != null)
            {
                ResolvedXnaDll[] resolvedXnaDlls = ResolveXnaDlls(dll);
                if (resolvedXnaDlls != null)
                {
                    foreach (ResolvedXnaDll resolvedXnaDll in resolvedXnaDlls)
                    {
                        if (xnaFrameworkVersion.Version == resolvedXnaDll.XnaFrameworkVersion)
                        {
                            return resolvedXnaDll.ResolvedDll.Filepath;
                        }
                    }
                }
            }

            return null;
        }

        private static ResolvedDll ResolveDll(Dll dll)
        {
            string name = dll.Name;
            string dllFilepath = null;
            switch (name)
            {

                case "D3DX9_33.dll":
                case "D3DX9_41.dll":
                case "X3DAudio1_7.dll":
                case "xactengine3_6.dll":
                case "XAudio2_6.dll":
                case "xinput1_3.dll":
                case "msvcp100.dll":
                case "msvcr100.dll":
                    dllFilepath =
                        @"C:\Windows\System32\" + name;
                    break;

                case "DSETUP.dll":
                case "dsetup32.dll":
                    dllFilepath =
                        @"C:\Program Files (x86)\Microsoft XNA\XNA Game Studio\v4.0\Redist\DX Redist\" + name;
                    break;

                case "XnaNative.dll":
                    dllFilepath =
                        @"C:\Program Files (x86)\Common Files\Microsoft Shared\XNA\Framework\v4.0\" + name;
                    break;

                case "xnavisualizer.dll":
                case "XnaVisualizerPS.dll":
                    dllFilepath =
                        @"C:\Program Files (x86)\Common Files\Microsoft Shared\XNA\Framework\Shared\" + name;
                    break;

            }
            if (dllFilepath != null && File.Exists(dllFilepath))
            {
                return new ResolvedDll(dll, dllFilepath);
            }

            return null;
        }

        private static ResolvedXnaDll[] ResolveXnaDlls(Dll dll)
        {
            string name = dll.Name;
            string xnaBaseDir = null;
            switch (name)
            {

                case "Microsoft.Xna.Framework.dll":
                case "Microsoft.Xna.Framework.Game.dll":
                case "Microsoft.Xna.Framework.Graphics.dll":
                case "Microsoft.Xna.Framework.Xact.dll":
                    xnaBaseDir =
                        @"C:\Windows\Microsoft.NET\assembly\GAC_32\" + name.Replace(".dll", "");
                    break;

                case "Microsoft.Xna.Framework.Avatar.dll":
                case "Microsoft.Xna.Framework.GamerServices.dll":
                case "Microsoft.Xna.Framework.Input.Touch.dll":
                case "Microsoft.Xna.Framework.Net.dll":
                case "Microsoft.Xna.Framework.Storage.dll":
                case "Microsoft.Xna.Framework.Video.dll":
                    xnaBaseDir =
                        @"C:\Windows\Microsoft.NET\assembly\GAC_MSIL\" + name.Replace(".dll", "");
                    break;

            }
            if (xnaBaseDir == null || !Directory.Exists(xnaBaseDir))
            {
                return null;
            }

            List<ResolvedXnaDll> resolvedXnaDlls = new List<ResolvedXnaDll>();
            foreach (DirectoryInfo dir in new DirectoryInfo(xnaBaseDir).GetDirectories())
            {
                string xnaFrameworkVersion = dir.Name;
                string filepath = xnaBaseDir + "\\" + xnaFrameworkVersion + "\\" + name;
                if (File.Exists(filepath))
                {
                    resolvedXnaDlls.Add(new ResolvedXnaDll(new ResolvedDll(dll, filepath), xnaFrameworkVersion));
                }
            }
            return resolvedXnaDlls.Count > 0 ? resolvedXnaDlls.ToArray() : null;
        }

    }
}