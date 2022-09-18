using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Threading.Tasks;

namespace Sahlaysta.PortableTerrariaCommon
{
    //resolve terraria files
    static class TerrariaResolver
    {

        //resolve terraria dlls

        //dll
        public struct Dll
        {
            readonly Program _program;
            string _filePath, _name;
            bool _status;

            //constructors
            public Dll(Program program, string filePath)
            {
                _program = program;
                _filePath = filePath;
                _name = filePath == null ? null : Path.GetFileName(filePath);
                _status = filePath == null ? false : File.Exists(filePath);
            }
            public Dll(Program program, string filePath, string name)
            {
                _program = program;
                _filePath = filePath;
                _name = name;
                _status = filePath == null ? false : File.Exists(filePath);
            }

            //public operations
            public Program Program { get { return _program; } }
            public string FilePath { get { return _filePath; } }
            public string Name { get { return _name; } }
            public bool Status { get { return _status; } }
        }

        //program
        public struct Program
        {
            readonly string _name, _link;

            public Program(string name, string link)
            {
                _name = name;
                _link = link;
            }

            //public fields
            public string Name { get { return _name; } }
            public string Link { get { return _link; } }

            //programs
            public static Program DirectX { get { return _directX; } }
            public static Program XnaFramework { get { return _xnaFramework; } }
            public static Program VisualCPP { get { return _visualCPP; } }

            static readonly Program _directX = new Program(
                "Microsoft DirectX",
                "https://www.microsoft.com/en-us/download/details.aspx?id=35");
            static readonly Program _xnaFramework = new Program(
                "Microsoft XNA Framework Redistributable 4.0",
                "https://www.microsoft.com/en-us/download/details.aspx?id=20914");
            static readonly Program _visualCPP = new Program(
                "Microsoft Visual C++ 2010 Service Pack 1" +
                    " Redistributable Package MFC Security Update",
                "https://www.microsoft.com/en-us/download/details.aspx?id=26999");
        }

        //dll fields
        static readonly (Program, string)[] dlls =
        {
            (Program.DirectX,
                "C:\\Windows\\System32\\D3DX9_33.dll"),
            (Program.DirectX,
                "C:\\Windows\\System32\\D3DX9_41.dll"),
            (Program.DirectX,
                "C:\\Windows\\System32\\X3DAudio1_7.dll"),
            (Program.DirectX,
                "C:\\Windows\\System32\\xactengine3_6.dll"),
            (Program.DirectX,
                "C:\\Windows\\System32\\XAudio2_6.dll"),
            (Program.DirectX,
                "C:\\Windows\\System32\\xinput1_3.dll"),

            (Program.VisualCPP,
                "C:\\Windows\\System32\\msvcp100.dll"),
            (Program.VisualCPP,
                "C:\\Windows\\System32\\msvcr100.dll"),

            (Program.XnaFramework,
                "C:\\Program Files (x86)\\Common Files" +
                    "\\Microsoft Shared\\XNA\\Framework\\v4.0\\XnaNative.dll"),
            (Program.XnaFramework,
                "C:\\Program Files (x86)\\Common Files" +
                    "\\Microsoft Shared\\XNA\\Framework\\Shared\\xnavisualizer.dll"),
            (Program.XnaFramework,
                "C:\\Program Files (x86)\\Common Files" +
                    "\\Microsoft Shared\\XNA\\Framework\\Shared\\XnaVisualizerPS.dll"),
            (Program.XnaFramework,
                "C:\\Program Files (x86)\\Microsoft XNA" +
                    "\\XNA Game Studio\\v4.0\\Redist\\DX Redist\\DSETUP.dll"),
            (Program.XnaFramework,
                "C:\\Program Files (x86)\\Microsoft XNA" +
                    "\\XNA Game Studio\\v4.0\\Redist\\DX Redist\\dsetup32.dll")
        };

        const string xnaPath = "C:\\Windows\\Microsoft.NET\\assembly";
        static readonly (string, string[])[] xnaDlls =
        {
            ("GAC_32", new string[] {
                "Microsoft.Xna.Framework",
                "Microsoft.Xna.Framework.Game",
                "Microsoft.Xna.Framework.Graphics",
                "Microsoft.Xna.Framework.Xact"
            }),
            ("GAC_MSIL", new string[] {
                "Microsoft.Xna.Framework.Avatar",
                "Microsoft.Xna.Framework.GamerServices",
                "Microsoft.Xna.Framework.Input.Touch",
                "Microsoft.Xna.Framework.Net",
                "Microsoft.Xna.Framework.Storage",
                "Microsoft.Xna.Framework.Video"
            })
        };

        //resolve
        public static Dll[] ResolveTerrariaDlls(string xnaFrameworkVersion = null)
        {
            var terrariaDlls = new List<Dll>();

            //default xna framework version
            if (xnaFrameworkVersion == null)
            {
                string[] xnaFrameworkVersions = ResolveXnaFrameworkVersions();
                if (xnaFrameworkVersions.Length > 0)
                    xnaFrameworkVersion = xnaFrameworkVersions[0];
            }

            //dlls
            foreach (var dllTuple in dlls)
            {
                Program program = dllTuple.Item1;
                string path = dllTuple.Item2;
                terrariaDlls.Add(new Dll(program, path));
            }

            //xnadlls
            foreach (var xnaTuple in xnaDlls)
            {
                string dirName = xnaTuple.Item1;
                string[] dllNames = xnaTuple.Item2;

                string xnaDir = Path.Combine(xnaPath, dirName);
                foreach (var dllName in dllNames)
                {
                    Dll dll;
                    if (xnaFrameworkVersion == null)//no version
                    {
                        dll = new Dll(
                            Program.XnaFramework,
                            null,
                            dllName + ".dll");
                    }
                    else
                    {
                        string dllPath = Path.Combine(Path.Combine(Path.Combine(
                            xnaDir, dllName), xnaFrameworkVersion), dllName + ".dll");
                        dll = new Dll(
                            Program.XnaFramework,
                            dllPath,
                            dllName + ".dll");
                    }
                    terrariaDlls.Add(dll);
                }
            }

            return terrariaDlls.ToArray();
        }

        //find the xna framework versions on the pc, and make sure the version has all dlls
        public static string[] ResolveXnaFrameworkVersions()
        {
            var vers = new HashSet<string>();
            bool first = true;
            foreach (var xnaTuple in xnaDlls)
            {
                string dirName = xnaTuple.Item1;
                string[] dllNames = xnaTuple.Item2;

                string xnaDir = Path.Combine(xnaPath, dirName);
                foreach (var dllName in dllNames)
                {
                    string dllDir = Path.Combine(xnaDir, dllName);

                    //check dir contains dll
                    IEnumerable<string> verNames;
                    try
                    {
                        verNames = Directory.GetDirectories(dllDir)
                        .Where(dir => File.Exists(Path.Combine(dir, dllName + ".dll")))
                        .Select(dir => Path.GetFileName(dir));
                    } catch (Exception)
                    {
                        verNames = new string[0];
                    }

                    if (first)
                    {
                        first = false;
                        foreach (var verName in verNames)
                        {
                            vers.Add(verName);
                        }
                    }
                    else
                    {
                        //remove uncontained
                        vers.RemoveWhere(ver => !verNames.Contains(ver));
                    }

                    //no valid versions
                    if (vers.Count() == 0)
                        return new string[0];
                }
            }
            return vers.ToArray();
        }
    }
}
