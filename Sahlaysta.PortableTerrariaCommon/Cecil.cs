using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Sahlaysta.PortableTerrariaCommon
{

    /// <summary>
    /// Uses the Mono.Cecil library to rewrite the embedded resources of an assembly.
    /// </summary>
    internal static class Cecil
    {

        public delegate void DelegateRead(out Stream stream, out bool closeStream);

        public delegate void DelegateWrite(out Stream stream, out bool closeStream);

        public delegate void DelegateReadWrite(out Stream stream, out bool closeStream);

        public delegate void DelegateReadResource(string resourceName, out Stream stream, out bool closeStream);

        public static void AssemblyRewriteEmbeddedResources(
            Assembly cecilAssembly,
            DelegateRead assemblyIn,
            DelegateWrite assemblyOut,
            string[] addResourceNames,
            string[] removeResourceNames,
            DelegateReadResource resourceReader,
            DelegateReadWrite overrideCecilMemoryStream)
        {
            if (cecilAssembly == null || assemblyIn == null || assemblyOut == null)
            {
                throw new ArgumentNullException();
            }

            addResourceNames = (string[])(addResourceNames ?? new string[] { }).Clone();
            removeResourceNames = (string[])(removeResourceNames ?? new string[] { }).Clone();

            if (addResourceNames.Contains(null) || removeResourceNames.Contains(null))
                throw new ArgumentNullException();

            if (addResourceNames.Length == 0 && removeResourceNames.Length == 0)
            {
                throw new ArgumentException("Empty");
            }

            foreach (string addResourceName in addResourceNames)
                if (addResourceNames.Count(x => x == addResourceName) > 1)
                    throw new ArgumentException("Add duplicate resource: " + addResourceName);

            foreach (string addResourceName in addResourceNames)
                if (removeResourceNames.Contains(addResourceName))
                    throw new ArgumentException("Cannot both add and remove resource: " + addResourceName);

            if (addResourceNames.Length > 0 && resourceReader == null)
            {
                throw new ArgumentNullException();
            }

            if (overrideCecilMemoryStream == null)
            {
                overrideCecilMemoryStream = (out Stream stream, out bool closeStream) =>
                {
                    stream = new MemoryStream();
                    closeStream = true;
                };
            }

            Type assemblyDefinitionType = ReflectionHelper.GetType(cecilAssembly, "Mono.Cecil.AssemblyDefinition");
            Type embeddedResourceType = ReflectionHelper.GetType(cecilAssembly, "Mono.Cecil.EmbeddedResource");
            Type manifestResourceAttributesType =
                ReflectionHelper.GetType(cecilAssembly, "Mono.Cecil.ManifestResourceAttributes");
            Type moduleDefinitionType = ReflectionHelper.GetType(cecilAssembly, "Mono.Cecil.ModuleDefinition");
            Type resourceTypeType = ReflectionHelper.GetType(cecilAssembly, "Mono.Cecil.ResourceType");
            MethodInfo readAssemblyMethod = ReflectionHelper.GetMethod(assemblyDefinitionType,
                "ReadAssembly", new Type[] { typeof(Stream) });
            PropertyInfo mainModuleProperty = ReflectionHelper.GetProperty(assemblyDefinitionType, "MainModule");
            PropertyInfo resourcesProperty = ReflectionHelper.GetProperty(moduleDefinitionType, "Resources");
            PropertyInfo resourceTypeProperty = ReflectionHelper.GetProperty(embeddedResourceType, "ResourceType");
            object resourceTypeEmbedded = ReflectionHelper.GetEnumValue(resourceTypeType, "Embedded");
            PropertyInfo resourceNameProperty = ReflectionHelper.GetProperty(embeddedResourceType, "Name");
            ConstructorInfo embeddedResourceConstructor = ReflectionHelper.GetConstructor(embeddedResourceType,
                new Type[] { typeof(string), manifestResourceAttributesType, typeof(Stream) });
            object manifestResourceAttributesPublic =
                ReflectionHelper.GetEnumValue(manifestResourceAttributesType, "Public");
            MethodInfo writeAssemblyMethod =
                ReflectionHelper.GetMethod(assemblyDefinitionType, "Write", new Type[] { typeof(Stream) });
            PropertyInfo embeddedResourceStreamProperty =
                embeddedResourceType.GetProperty("EmbeddedResourceStream");

            using (StreamDelegator assemblyInStream = new StreamDelegator(assemblyIn))
            {

                IDisposable assemblyDefinition = (IDisposable)readAssemblyMethod
                    .Invoke(null, new object[] { assemblyInStream });

                using (assemblyDefinition)
                {
                    object mainModule = mainModuleProperty.GetValue(assemblyDefinition);
                    IList resources = (IList)resourcesProperty.GetValue(mainModule);

                    for (int i = 0; i < resources.Count; i++)
                    {
                        object resource = resources[i];
                        if (resource != null)
                        {
                            object resourceType = resourceTypeProperty.GetValue(resource);
                            string resourceName = (string)resourceNameProperty.GetValue(resource);
                            if (resourceTypeEmbedded.Equals(resourceType))
                            {
                                if (removeResourceNames.Contains(resourceName))
                                {
                                    resources.RemoveAt(i);
                                    i--;
                                }
                                if (addResourceNames.Contains(resourceName))
                                {
                                    throw new Exception("Resource already exists: " + resourceName);
                                }
                            }
                        }
                    }

                    using (StreamManager streamManager = new StreamManager(resourceReader))
                    {
                        foreach (string resourceName in addResourceNames)
                        {
                            Stream stream = streamManager.NewStream(resourceName);
                            object embeddedResource = embeddedResourceConstructor
                                .Invoke(new object[] { resourceName, manifestResourceAttributesPublic, stream });
                            resources.Add(embeddedResource);
                        }

                        using (StreamDelegator cecilMemStream = new StreamDelegator(overrideCecilMemoryStream))
                        {

                            if (embeddedResourceStreamProperty != null)
                            {
                                embeddedResourceStreamProperty.SetValue(null, cecilMemStream);
                            }
                            try
                            {
                                using (StreamDelegator assemblyOutStream = new StreamDelegator(assemblyOut))
                                {
                                    writeAssemblyMethod.Invoke(
                                        assemblyDefinition, new object[] { assemblyOutStream });
                                }
                            }
                            finally
                            {
                                if (embeddedResourceStreamProperty != null)
                                {
                                    embeddedResourceStreamProperty.SetValue(null, null);
                                }
                            }
                        }
                    }
                }
            }
        }

        private class StreamDelegator : Stream
        {

            private readonly Stream stream;
            private readonly bool closeStream;
            private bool disposed;

            public StreamDelegator(DelegateRead d) { d(out stream, out closeStream); NullCheck(); }
            public StreamDelegator(DelegateWrite d) { d(out stream, out closeStream); NullCheck(); }
            public StreamDelegator(DelegateReadWrite d) { d(out stream, out closeStream); NullCheck(); }

            private void NullCheck()
            {
                if (stream == null) { throw new ArgumentNullException(); }
            }

            protected override void Dispose(bool disposing)
            {
                if (disposed) return;
                disposed = true;
                if (disposing && closeStream)
                {
                    stream.Close();
                }
            }

            private Stream GetStream()
            {
                if (disposed) { throw new ObjectDisposedException(GetType().FullName); }
                return stream;
            }

            public override int Read(byte[] buffer, int offset, int count) {
                return GetStream().Read(buffer, offset, count); }
            public override void Write(byte[] buffer, int offset, int count) {
                GetStream().Write(buffer, offset, count); }
            public override void Flush() { GetStream().Flush(); }
            public override long Seek(long offset, SeekOrigin origin) {
                return GetStream().Seek(offset, origin); }
            public override void SetLength(long value) { GetStream().SetLength(value); }
            public override long Length { get { return GetStream().Length; } }
            public override bool CanRead { get { return !disposed && GetStream().CanRead; } }
            public override bool CanWrite { get { return !disposed && GetStream().CanWrite; } }
            public override bool CanSeek { get { return !disposed && GetStream().CanSeek; } }
            public override long Position {
                get { return GetStream().Position; } set { GetStream().Position = value; } }

        }

        private class StreamManager : IDisposable
        {

            private DelegateReadResource drr;
            private StreamManagerStream current;
            private bool disposed;

            public StreamManager(DelegateReadResource drr)
            {
                this.drr = drr;
            }

            public Stream NewStream(string resourceName)
            {
                if (disposed) { throw new ObjectDisposedException(GetType().FullName); }
                return new StreamManagerStream(this, resourceName);
            }

            void IDisposable.Dispose()
            {
                if (disposed) return;
                disposed = true;
                drr = null;
                using (current)
                {
                    current = null;
                }
            }

            private class StreamManagerStream : Stream
            {

                private readonly StreamManager streamManager;
                private readonly string resourceName;
                private Stream stream;
                private bool closeStream;
                private bool disposed;

                public StreamManagerStream(StreamManager streamManager, string resourceName)
                {
                    this.streamManager = streamManager;
                    this.resourceName = resourceName;
                }

                private Stream GetStream()
                {
                    if (disposed) { throw new ObjectDisposedException(GetType().FullName); }

                    StreamManagerStream current = streamManager.current;
                    if (object.ReferenceEquals(current, this))
                    {
                        return stream;
                    }
                    else
                    {
                        current?.Close();
                        streamManager.drr(resourceName, out stream, out closeStream);
                        if (stream == null) { throw new ArgumentNullException(); }
                        streamManager.current = this;
                        return stream;
                    }
                }

                protected override void Dispose(bool disposing)
                {
                    if (disposed) return;
                    disposed = true;
                    if (disposing)
                        streamManager.current = null;
                    if (stream != null)
                    {
                        Stream theStream = stream;
                        stream = null;
                        if (disposing && closeStream)
                        {
                            theStream.Close();
                        }
                    }
                }

                public override int Read(byte[] buffer, int offset, int count) {
                    return GetStream().Read(buffer, offset, count); }
                public override void Write(byte[] buffer, int offset, int count) {
                    GetStream().Write(buffer, offset, count); }
                public override void Flush() { GetStream().Flush(); }
                public override long Seek(long offset, SeekOrigin origin) {
                    return GetStream().Seek(offset, origin); }
                public override void SetLength(long value) { GetStream().SetLength(value); }
                public override long Length { get { return GetStream().Length; } }
                public override bool CanRead { get { return !disposed && GetStream().CanRead; } }
                public override bool CanWrite { get { return !disposed && GetStream().CanWrite; } }
                public override bool CanSeek { get { return !disposed && GetStream().CanSeek; } }
                public override long Position {
                    get { return GetStream().Position; } set { GetStream().Position = value; } }

            }

        }

    }
}