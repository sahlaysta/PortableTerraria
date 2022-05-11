using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Sahlaysta.PortableTerrariaCommon
{
    // Mono.Cecil assembly reflection class
    static class MonoCecilAssembly
    {
        //reflection AssemblyDefinition
        public class AssemblyDefinition : IDisposable
        {
            //reflection constructor
            AssemblyDefinition(IDisposable instance)
            {
                this.instance = instance;
            }
            public static AssemblyDefinition ReadAssembly(Stream arg0)
            {
                return new AssemblyDefinition(
                    (IDisposable)rReadAssembly
                        .Invoke(null, new object[] { arg0 }));
            }

            //public operations
            public ModuleDefinition MainModule
            {
                get => new ModuleDefinition(
                    (IDisposable)rMainModule.GetValue(instance));
            }
            public AssemblyNameDefinition Name
            {
                get => new AssemblyNameDefinition(
                    rName2.GetValue(instance));
            }
            public void Write(Stream arg0)
            {
                rWrite.Invoke(instance, new object[] { arg0 });
            }
            public void Dispose()
            {
                instance.Dispose();
            }

            readonly IDisposable instance;
        }

        //reflection ModuleDefinition
        public class ModuleDefinition : IDisposable
        {
            //reflection constructor
            internal ModuleDefinition(IDisposable instance)
            {
                this.instance = instance;
            }

            //public operations
            public IEnumerable<Resource> Resources
            {
                get => new ResourceEnumerable(this);
            }
            public void Dispose()
            {
                instance.Dispose();
            }

            //reflection enumerable
            internal class ResourceEnumerable : IEnumerable<Resource>
            {
                internal ResourceEnumerable(ModuleDefinition moduleDefinition)
                {
                    this.moduleDefinition = moduleDefinition;
                }
                IEnumerator<Resource> IEnumerable<Resource>.GetEnumerator()
                {
                    return moduleDefinition.getResourceEnumerator();
                }
                IEnumerator IEnumerable.GetEnumerator()
                {
                    return moduleDefinition.getResourceEnumerator();
                }
                internal readonly ModuleDefinition moduleDefinition;
            }

            //reflection enumerator
            IEnumerator<Resource> getResourceEnumerator()
            {
                return new ResourceEnumerator(
                    ((IEnumerable)rResources.GetValue(instance))
                        .GetEnumerator());
            }
            class ResourceEnumerator : IEnumerator<Resource>
            {
                //reflection constructor
                public ResourceEnumerator(IEnumerator enumerator)
                {
                    ie = enumerator;
                }

                //interface operations
                Resource IEnumerator<Resource>.Current => getCurrent();
                object IEnumerator.Current => getCurrent();
                void IDisposable.Dispose() => ((IDisposable)ie).Dispose();
                bool IEnumerator.MoveNext()
                {
                    bool result = ie.MoveNext();
                    if (result)
                        current = null;
                    return result;
                }
                void IEnumerator.Reset()
                {
                    ie.Reset();
                    current = null;
                }

                Resource getCurrent()
                {
                    return current ?? (current = new Resource(ie.Current));
                }

                readonly IEnumerator ie;
                Resource current;
            }

            internal readonly IDisposable instance;
        }

        //reflection Resource
        public class Resource
        {
            //reflection constructor
            internal Resource(object instance)
            {
                this.instance = instance;
            }

            //public reflection operations
            public string Name
            {
                get => (string)rName.GetValue(instance);
            }

            readonly object instance;
        }

        //reflection ManifestResourceAttributes
        public enum ManifestResourceAttributes
        {
            Private,
            Public,
            VisibilityMask
        }
        static object getManifestResourceAttributes(
            ManifestResourceAttributes manifestResourceAttributes)
        {
            switch (manifestResourceAttributes)
            {
                case ManifestResourceAttributes.Private:
                    return rManifestResourceAttributesPrivate
                        .GetValue(null);
                case ManifestResourceAttributes.Public:
                    return rManifestResourceAttributesPublic
                        .GetValue(null);
                case ManifestResourceAttributes.VisibilityMask:
                    return rManifestResourceAttributesVisibilityMask
                        .GetValue(null);
                default:
                    return null;
            }
        }

        //reflection EmbeddedResource
        public class EmbeddedResource
        {
            //reflection constructor
            public EmbeddedResource(
                String arg0,
                ManifestResourceAttributes arg1,
                byte[] arg2)
            {
                instance = rEmbeddedResourceConstr.Invoke(
                    new object[]
                    {
                        arg0,
                        getManifestResourceAttributes(arg1),
                        arg2
                    });
            }
            public EmbeddedResource(
                String arg0,
                ManifestResourceAttributes arg1,
                Stream arg2)
            {
                instance = rEmbeddedResourceConstr2.Invoke(
                    new object[]
                    {
                        arg0,
                        getManifestResourceAttributes(arg1),
                        arg2
                    });
            }

            //public reflection operations
            public static Stream EmbeddedResourceStream
            {
                get => (Stream)rEmbeddedResourceStream.
                    GetValue(null);
                set => rEmbeddedResourceStream
                    .SetValue(null, value);
            }

            internal readonly object instance;
        }

        //reflection Collection
        public static class Collection
        {
            //public reflection operations
            public static void AddEmbeddedResource(
                IEnumerable<Resource> resources,
                EmbeddedResource embeddedResource)
            {
                var re = (ModuleDefinition.
                    ResourceEnumerable)resources;
                var md = re.moduleDefinition;
                var ie = rResources.GetValue(md.instance);
                rAdd.Invoke(
                    ie,
                    new object[] { embeddedResource.instance });
            }
        }

        //reflection AssemblyNameDefinition
        public class AssemblyNameDefinition
        {
            //reflection constructor
            internal AssemblyNameDefinition(object instance)
            {
                this.instance = instance;
            }

            //public operations
            public string Name
            {
                get => (string)rName3.GetValue(instance);
            }

            readonly object instance;
        }


        //assembly
        static readonly Assembly assembly =
            Assembly.Load(GuiHelper.GetResourceBytes(
                "Mono.Cecil.dll"));

        //reflection fields
        static readonly Type rAssemblyDefinition =
            assembly.GetType(
                "Mono.Cecil.AssemblyDefinition");
        static readonly MethodInfo rReadAssembly =
            rAssemblyDefinition.GetMethod(
                "ReadAssembly",
                new Type[] { typeof(Stream) });
        static readonly MethodInfo rWrite =
            rAssemblyDefinition.GetMethod(
                "Write",
                new Type[] { typeof(Stream) });
        static readonly PropertyInfo rMainModule =
            rAssemblyDefinition.GetProperty(
                "MainModule");
        static readonly PropertyInfo rName2 =
            rAssemblyDefinition.GetProperty(
                "Name");
        static readonly Type rModuleDefinition =
            assembly.GetType(
                "Mono.Cecil.ModuleDefinition");
        static readonly PropertyInfo rResources =
            rModuleDefinition.GetProperty(
                "Resources");
        static readonly Type rResource =
            assembly.GetType(
                "Mono.Cecil.Resource");
        static readonly PropertyInfo rName =
            rResource.GetProperty(
                "Name");
        static readonly Type rManifestResourceAttributes =
            assembly.GetType(
                "Mono.Cecil.ManifestResourceAttributes");
        static readonly FieldInfo rManifestResourceAttributesPrivate =
            rManifestResourceAttributes.GetField("Private");
        static readonly FieldInfo rManifestResourceAttributesPublic =
            rManifestResourceAttributes.GetField("Public");
        static readonly FieldInfo rManifestResourceAttributesVisibilityMask =
            rManifestResourceAttributes.GetField("VisibilityMask");
        static readonly Type rEmbeddedResource =
            assembly.GetType(
                "Mono.Cecil.EmbeddedResource");
        static readonly ConstructorInfo rEmbeddedResourceConstr =
            rEmbeddedResource.GetConstructor(
                new Type[]
                {
                    typeof(string),
                    rManifestResourceAttributes,
                    typeof(byte[])
                });
        static readonly ConstructorInfo rEmbeddedResourceConstr2 =
            rEmbeddedResource.GetConstructor(
                new Type[]
                {
                    typeof(string),
                    rManifestResourceAttributes,
                    typeof(Stream)
                });
        static readonly PropertyInfo rEmbeddedResourceStream =
            rEmbeddedResource.GetProperty(
                "EmbeddedResourceStream");
        static readonly Type rCollection =
            assembly.GetType(
                "Mono.Collections.Generic.Collection`1")
                    .MakeGenericType(rResource);
        static readonly MethodInfo rAdd =
            rCollection.GetMethod(
                "Add");
        static readonly Type rAssemblyNameDefinition =
            assembly.GetType(
                "Mono.Cecil.AssemblyNameDefinition");
        static readonly PropertyInfo rName3 =
            rAssemblyNameDefinition.GetProperty(
                "Name");
    }
}
