using System;
using System.IO;
using System.Linq;

namespace Sahlaysta.PortableTerrariaCommon
{
    internal static class StreamConcatenator
    {

        public delegate void StreamDelegate(out Stream stream, out bool closeStream);

        public static Stream ConcatenateStreams(params StreamDelegate[] streamDelegates)
        {
            if (streamDelegates == null)
            {
                throw new ArgumentException("Null");
            }
            if (streamDelegates.Length == 0)
            {
                throw new ArgumentException("Empty");
            }
            streamDelegates = (StreamDelegate[])streamDelegates.Clone();
            if (streamDelegates.Contains(null))
            {
                throw new ArgumentException("Null");
            }
            return new ConcatenatedStream(streamDelegates);
        }

        public static Stream ConcatenateStreams(params Stream[] streams)
        {
            if (streams == null)
            {
                throw new ArgumentException("Null");
            }
            if (streams.Length == 0)
            {
                throw new ArgumentException("Empty");
            }
            streams = (Stream[])streams.Clone();
            if (streams.Contains(null))
            {
                throw new ArgumentException("Null");
            }
            StreamDelegate[] streamDelegates = streams
                .Select(x => (StreamDelegate)((out Stream stream, out bool closeStream) =>
                {
                    stream = x;
                    closeStream = false;
                })).ToArray();
            return ConcatenateStreams(streamDelegates);
        }

        private class ConcatenatedStream : Stream
        {

            private StreamDelegate[] streamDelegates;
            private int streamDelegateIndex;
            private Stream stream;
            private bool closeStream;
            private bool disposed;

            public ConcatenatedStream(StreamDelegate[] streamDelegates)
            {
                this.streamDelegates = streamDelegates;
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                if (disposed) throw new ObjectDisposedException(GetType().FullName);
                if ((offset | count | (offset + count) | (buffer.Length - (offset + count))) < 0)
                {
                    throw new IndexOutOfRangeException();
                }
                if (count == 0) return 0;

                while (true)
                {
                    if (stream == null)
                    {
                        if (streamDelegateIndex >= streamDelegates.Length)
                        {
                            return 0;
                        }
                        else
                        {
                            streamDelegates[streamDelegateIndex](out stream, out closeStream);
                            if (stream == null) throw new ArgumentException("Null");
                            streamDelegateIndex += 1;
                        }
                    }

                    int r = stream.Read(buffer, offset, count);
                    if (r == 0)
                    {
                        Stream theStream = stream;
                        stream = null;
                        if (closeStream)
                        {
                            theStream.Close();
                        }
                    }
                    else
                    {
                        return r;
                    }
                }

            }

            public override void Flush()
            {
                if (disposed) throw new ObjectDisposedException(GetType().FullName);
            }

            protected override void Dispose(bool disposing)
            {
                if (disposed) return;
                disposed = true;
                streamDelegates = null;
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

            public override void Write(byte[] buffer, int offset, int count) {
                throw new NotSupportedException(); }
            public override bool CanRead { get { return true; } }
            public override bool CanWrite { get { return false; } }
            public override bool CanSeek { get { return false; } }
            public override long Seek(long offset, SeekOrigin origin) { throw new NotSupportedException(); }
            public override void SetLength(long value) { throw new NotSupportedException(); }
            public override long Length { get { throw new NotSupportedException(); } }
            public override long Position {
                get { throw new NotSupportedException(); } set { throw new NotSupportedException(); } }

        }

    }

}