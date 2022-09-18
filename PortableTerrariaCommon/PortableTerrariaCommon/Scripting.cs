using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Sahlaysta.PortableTerrariaCommon
{
    //run .csx script
    class Scripting
    {
        readonly CompilerResults crs;

        //constructor
        Scripting(string script)
        {
            crs = compile(script);
        }
        public static Scripting Create(string filePath)
        {
            string script = File.ReadAllText(filePath);
            return new Scripting(script);
        }
        public static Scripting Create(Stream stream)
        {
            string script;
            using (var sr = new StreamReader(stream))
            {
                script = sr.ReadToEnd();
            }
            return new Scripting(script);
        }

        //public operations
        public T InvokePublicStaticMethod<T>(
            string typeName, string methodName,
            Type[] methodTypes, object[] parameters)
        {
            var type = crs.CompiledAssembly.GetType(typeName);
            var method = type.GetMethod(methodName, methodTypes);
            return (T)method.Invoke(null, parameters);
        }

        //initializations
        CompilerResults compile(string script)
        {
            //compiler options
            var cps = new CompilerParameters()
            {
                GenerateInMemory = true,
                GenerateExecutable = false
            };
            var assemblyDlls =
                AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetModules()
                .Where(a2 => a2.Name.EndsWith(
                    ".dll",
                    StringComparison.InvariantCultureIgnoreCase)))
                .Select(a3 => a3.Name);
            foreach (var assembly in assemblyDlls)
            {
                cps.ReferencedAssemblies.Add(assembly);
            }

            //compiler results
            using (var cscp = new CSharpCodeProvider())
            {
                var results = cscp.CompileAssemblyFromSource(cps, script);
                if (results.Errors.Count > 0)
                {
                    throw new ArgumentException(
                        "Script error:\n\n" +
                        results.Errors[0].ToString());
                }
                return results;
            }
        }
    }
}
