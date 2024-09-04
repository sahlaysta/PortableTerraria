using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Sahlaysta.PortableTerrariaCommon;

namespace Sahlaysta.PortableTerrariaLauncher
{

    /// <summary>
    /// Reads and extracts data from the assembly resources of this launcher.
    /// The write logic is in GuiLauncherAssemblyWriter.cs
    /// </summary>
    internal static class GuiLauncherAssemblyReader
    {

        private static string launcherGuid;
        public static string GetLauncherGuid()
        {
            if (launcherGuid == null)
            {
                launcherGuid = ManifestResources.ReadUTF8String("GUID");
            }
            return launcherGuid;
        }

        private class AssemblyZipDelegate
        {

            public readonly int Id;
            public readonly StreamConcatenator.StreamDelegate StreamDelegate;

            public AssemblyZipDelegate(int id, StreamConcatenator.StreamDelegate streamDelegate)
            {
                Id = id;
                StreamDelegate = streamDelegate;
            }

        }

        public static Stream GetZipStream()
        {
            string launcherGuid = GetLauncherGuid();

            List<AssemblyZipDelegate> assemblyZipDelegates = new List<AssemblyZipDelegate>();

            using (SHA256 sha256 = SHA256.Create())
            {
                int i = 0;
                byte[] headerBuf = new byte[112];
                UTF8Encoding utf8Encoding = new UTF8Encoding(false, true);
                string consistentNumberOfSplitsStr = null;
                while (true)
                {
                    string namePrefix = "ZIP_";

                    byte[] hash = sha256.ComputeHash(Encoding.Unicode.GetBytes(launcherGuid + i));
                    string hashAsString = string.Join("", hash.Select(x => x.ToString("X2")));
                    string nameSuffix = hashAsString;

                    string resourceName = namePrefix + nameSuffix;

                    Stream resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
                    if (resourceStream == null)
                    {
                        break;
                    }

                    string errorMsg = "Bad split header, resource name " + resourceName;
                    using (resourceStream)
                    {
                        if (resourceStream.Length != 0)
                        {
                            if (ReadFully(resourceStream, headerBuf) != headerBuf.Length)
                            {
                                throw new Exception(errorMsg);
                            }

                            string headerAsString;
                            using (StreamReader headerReader =
                                new StreamReader(new MemoryStream(headerBuf), utf8Encoding))
                            {
                                headerAsString = headerReader.ReadToEnd();
                            }

                            string headerPrefix = "\"FileSplitHeader\"";
                            if (!headerAsString.StartsWith(headerPrefix))
                            {
                                throw new Exception(errorMsg);
                            }
                            headerAsString = headerAsString.Substring(headerPrefix.Length);

                            string[] headerSplits =
                                headerAsString.Split('\0').Where(x => x.Length != 0).ToArray();

                            string headerSizeStr = null;
                            string splitIdStr = null;
                            string numberOfSplitsStr = null;
                            foreach (string headerSplit in headerSplits)
                            {
                                if (!headerSplit.StartsWith("\"") || !headerSplit.EndsWith("\"")
                                    || headerSplit.Count(x => x == '=') != 1)
                                {
                                    throw new Exception(errorMsg);
                                }

                                string keyStr = headerSplit.Substring(1, headerSplit.IndexOf('=') - 1);

                                string valueStr = headerSplit.Substring(headerSplit.IndexOf('=') + 1);
                                valueStr = valueStr.Substring(0, valueStr.Length - 1);

                                switch (keyStr)
                                {
                                    case "HeaderSize":
                                        if (headerSizeStr != null) { throw new Exception(errorMsg); }
                                        headerSizeStr = valueStr;
                                        break;
                                    case "SplitId":
                                        if (splitIdStr != null) { throw new Exception(errorMsg); }
                                        splitIdStr = valueStr;
                                        break;
                                    case "NumberOfSplits":
                                        if (numberOfSplitsStr != null) { throw new Exception(errorMsg); }
                                        numberOfSplitsStr = valueStr;
                                        break;
                                    default: throw new Exception(errorMsg);
                                }
                            }

                            if (headerSizeStr == null || splitIdStr == null || numberOfSplitsStr == null)
                            {
                                throw new Exception(errorMsg);
                            }

                            if (int.Parse(headerSizeStr) != headerBuf.Length)
                            {
                                throw new Exception(errorMsg);
                            }

                            if (consistentNumberOfSplitsStr == null)
                            {
                                consistentNumberOfSplitsStr = numberOfSplitsStr;
                            }
                            else
                            {
                                if (consistentNumberOfSplitsStr != numberOfSplitsStr)
                                {
                                    throw new Exception(errorMsg);
                                }
                            }

                            int id = int.Parse(splitIdStr);

                            if (assemblyZipDelegates.Any(x => x.Id == id))
                            {
                                throw new Exception(errorMsg);
                            }

                            StreamConcatenator.StreamDelegate streamDelegate =
                                (out Stream stream, out bool closeStream) =>
                                {
                                    stream = new SkippedStream(
                                        headerBuf.Length,
                                        Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName));
                                    closeStream = true;
                                };

                            AssemblyZipDelegate assemblyZipDelegate = new AssemblyZipDelegate(id, streamDelegate);
                            assemblyZipDelegates.Add(assemblyZipDelegate);
                        }
                    }

                    i += 1;
                }
            }

            if (assemblyZipDelegates.Count == 0)
            {
                throw new Exception("Resources not found");
            }

            assemblyZipDelegates.Sort((x, y) => x.Id.CompareTo(y.Id));

            return StreamConcatenator.ConcatenateSeekableStreams(
                assemblyZipDelegates.Select(x => x.StreamDelegate).ToArray());
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

        private class SkippedStream : Stream
        {

            private readonly long nSkip;
            private Stream stream;
            private bool disposed;

            public SkippedStream(long nSkip, Stream stream)
            {
                this.nSkip = nSkip;
                this.stream = stream;
                if (stream.Position != 0)
                {
                    throw new ArgumentException("Base stream position must be 0");
                }
                if (nSkip < 0)
                {
                    throw new ArgumentException("nSkip cannot be negative");
                }
                if (nSkip > stream.Length)
                {
                    throw new ArgumentException("nSkip cannot be greater than stream length");
                }
                stream.Position = nSkip;
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                if (disposed) { throw new ObjectDisposedException(GetType().FullName); }

                return stream.Read(buffer, offset, count);
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                if (disposed) { throw new ObjectDisposedException(GetType().FullName); }

                switch (origin)
                {
                    case SeekOrigin.Begin:
                        Position = offset;
                        return Position;
                    case SeekOrigin.Current:
                        Position = Position + offset;
                        return Position;
                    case SeekOrigin.End:
                        Position = Length + offset;
                        return Position;
                    default: throw new Exception();
                }
            }

            public override long Length
            {
                get
                {
                    if (disposed) { throw new ObjectDisposedException(GetType().FullName); }
                    return stream.Length - nSkip;
                }
            }

            public override long Position
            {
                get
                {
                    if (disposed) { throw new ObjectDisposedException(GetType().FullName); }
                    return stream.Position - nSkip;
                }
                set
                {
                    if (disposed) { throw new ObjectDisposedException(GetType().FullName); }
                    if (value < 0)
                    {
                        throw new ArgumentException("Attempt to set position before beginning of stream");
                    }
                    stream.Position = value + nSkip;
                }
            }

            protected override void Dispose(bool disposing)
            {
                if (disposed) return;
                disposed = true;
                if (stream != null)
                {
                    Stream theStream = stream;
                    stream = null;
                    if (disposing)
                    {
                        theStream.Close();
                    }
                }
            }

            public override void Flush()
            {
                if (disposed) { throw new ObjectDisposedException(GetType().FullName); }
            }

            public override void Write(byte[] buffer, int offset, int count) {
                throw new NotSupportedException(); }
            public override bool CanRead { get { return !disposed; } }
            public override bool CanWrite { get { return false; } }
            public override bool CanSeek { get { return !disposed; } }
            public override void SetLength(long value) { throw new NotSupportedException(); }

        }

    }
}
