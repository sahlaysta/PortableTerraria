using System;
using System.IO;
using System.Linq;

namespace Sahlaysta.PortableTerrariaCommon
{

    /// <summary>
    /// Join multiple Streams into one Stream.
    /// </summary>
    internal static class StreamConcatenator
    {

        public delegate void StreamDelegate(out Stream stream, out bool closeStream);

        public static Stream ConcatenateStreams(params StreamDelegate[] streamDelegates)
        {
            return ConcatenateStreams(streamDelegates, false);
        }

        public static Stream ConcatenateStreams(params Stream[] streams)
        {
            return ConcatenateStreams(streams, false);
        }

        public static Stream ConcatenateSeekableStreams(params StreamDelegate[] streamDelegates)
        {
            return ConcatenateStreams(streamDelegates, true);
        }

        public static Stream ConcatenateSeekableStreams(params Stream[] streams)
        {
            return ConcatenateStreams(streams, true);
        }

        private static Stream ConcatenateStreams(StreamDelegate[] streamDelegates, bool seekable)
        {
            if (streamDelegates == null)
            {
                throw new ArgumentNullException();
            }
            if (streamDelegates.Length == 0)
            {
                throw new ArgumentException("Empty");
            }
            streamDelegates = (StreamDelegate[])streamDelegates.Clone();
            if (streamDelegates.Contains(null))
            {
                throw new ArgumentNullException();
            }
            return seekable
                ? (Stream)new SeekableConcatenatedStream(streamDelegates)
                : (Stream)new ConcatenatedStream(streamDelegates);
        }

        private static Stream ConcatenateStreams(Stream[] streams, bool seekable)
        {
            if (streams == null)
            {
                throw new ArgumentNullException();
            }
            if (streams.Length == 0)
            {
                throw new ArgumentException("Empty");
            }
            streams = (Stream[])streams.Clone();
            if (streams.Contains(null))
            {
                throw new ArgumentNullException();
            }
            StreamDelegate[] streamDelegates = streams
                .Select(x => (StreamDelegate)((out Stream stream, out bool closeStream) =>
                {
                    stream = x;
                    closeStream = false;
                })).ToArray();
            return seekable
                ? (Stream)new SeekableConcatenatedStream(streamDelegates)
                : (Stream)new ConcatenatedStream(streamDelegates);
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
                this.streamDelegateIndex = -1;
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                if (disposed) { throw new ObjectDisposedException(GetType().FullName); }
                if ((offset | count | (offset + count) | (buffer.Length - (offset + count))) < 0)
                {
                    throw new IndexOutOfRangeException();
                }
                if (count == 0) return 0;

                while (true)
                {
                    if (stream == null)
                    {
                        if (streamDelegateIndex >= streamDelegates.Length - 1)
                        {
                            return 0;
                        }
                        else
                        {
                            streamDelegates[streamDelegateIndex + 1](out stream, out closeStream);
                            if (stream == null) { throw new ArgumentNullException(); }
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
                if (disposed) { throw new ObjectDisposedException(GetType().FullName); }
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
            public override bool CanRead { get { return !disposed; } }
            public override bool CanWrite { get { return false; } }
            public override bool CanSeek { get { return false; } }
            public override long Seek(long offset, SeekOrigin origin) { throw new NotSupportedException(); }
            public override void SetLength(long value) { throw new NotSupportedException(); }
            public override long Length { get { throw new NotSupportedException(); } }
            public override long Position {
                get { throw new NotSupportedException(); } set { throw new NotSupportedException(); } }

        }

        private class SeekableConcatenatedStream : ConcatenatedStream
        {

            private StreamDelegate[] streamDelegates;
            private long[] streamDelegateLengths;
            private readonly long totalLength;
            private int streamDelegateIndex;
            private long pos;
            private Stream stream;
            private bool closeStream;
            private bool disposed;

            public SeekableConcatenatedStream(StreamDelegate[] streamDelegates) : base(streamDelegates)
            {
                this.streamDelegates = streamDelegates;
                this.streamDelegateLengths = new long[streamDelegates.Length];
                this.totalLength = 0;
                this.streamDelegateIndex = -1;
                for (int i = 0; i < streamDelegates.Length; i++)
                {
                    stream = null;
                    closeStream = false;
                    try
                    {
                        streamDelegates[i](out stream, out closeStream);
                        if (stream == null) { throw new ArgumentNullException(); }
                        if (!stream.CanSeek) { throw new ArgumentException("Stream cannot seek"); }
                        long streamLength = stream.Length;
                        streamDelegateLengths[i] = streamLength;
                        this.totalLength += streamLength;
                    }
                    finally
                    {
                        if (stream != null)
                        {
                            Stream theStream = stream;
                            stream = null;
                            if (closeStream)
                            {
                                theStream.Close();
                            }
                        }
                    }
                }
                stream = null;
                closeStream = false;
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                if (disposed) { throw new ObjectDisposedException(GetType().FullName); }
                if ((offset | count | (offset + count) | (buffer.Length - (offset + count))) < 0)
                {
                    throw new IndexOutOfRangeException();
                }
                if (count == 0) return 0;

                while (true)
                {
                    if (stream == null)
                    {
                        if (streamDelegateIndex >= streamDelegates.Length - 1)
                        {
                            return 0;
                        }
                        else
                        {
                            streamDelegates[streamDelegateIndex + 1](out stream, out closeStream);
                            if (stream == null) { throw new ArgumentNullException(); }
                            if (stream.Length != streamDelegateLengths[streamDelegateIndex + 1])
                            {
                                throw new ArgumentException("Length of stream has changed");
                            }
                            stream.Position = 0;
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
                        pos += r;
                        return r;
                    }
                }

            }

            public override void Flush()
            {
                if (disposed) { throw new ObjectDisposedException(GetType().FullName); }
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                if (disposed) { throw new ObjectDisposedException(GetType().FullName); }

                switch (origin)
                {
                    case SeekOrigin.Begin:
                        Position = offset;
                        return pos;
                    case SeekOrigin.Current:
                        Position = pos + offset;
                        return pos;
                    case SeekOrigin.End:
                        Position = totalLength + offset;
                        return pos;
                    default: throw new Exception();
                }
            }

            public override long Length
            {
                get
                {
                    if (disposed) { throw new ObjectDisposedException(GetType().FullName); }
                    return totalLength;
                }
            }

            public override long Position
            {
                get
                {
                    if (disposed) { throw new ObjectDisposedException(GetType().FullName); }
                    return pos;
                }
                set
                {
                    if (disposed) { throw new ObjectDisposedException(GetType().FullName); }
                    if (value < 0)
                    {
                        throw new ArgumentException("Attempt to set position before beginning of stream");
                    }

                    int i = 0;
                    long iPos = 0;
                    bool found = false;
                    for (; i < streamDelegates.Length; i++)
                    {
                        long streamLength = streamDelegateLengths[i];
                        if (iPos + streamLength > value)
                        {
                            found = true;
                            break;
                        }
                        iPos += streamLength;
                    }

                    if (!found)
                    {
                        if (stream != null)
                        {
                            Stream theStream = stream;
                            stream = null;
                            if (closeStream)
                            {
                                theStream.Close();
                            }
                        }
                        streamDelegateIndex = streamDelegates.Length;
                        pos = value;
                    }
                    else if (stream != null && streamDelegateIndex == i)
                    {
                        stream.Position = value - iPos;
                        pos = value;
                    }
                    else
                    {
                        if (stream != null)
                        {
                            Stream theStream = stream;
                            stream = null;
                            if (closeStream)
                            {
                                theStream.Close();
                            }
                        }
                        streamDelegateIndex = -1;
                        pos = 0;
                        streamDelegates[i](out stream, out closeStream);
                        if (stream == null) { throw new ArgumentNullException(); }
                        if (stream.Length != streamDelegateLengths[i])
                        {
                            throw new ArgumentException("Length of stream has changed");
                        }
                        stream.Position = value - iPos;
                        pos = value;
                        streamDelegateIndex = i;
                    }
                }
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
            public override bool CanRead { get { return !disposed; } }
            public override bool CanWrite { get { return false; } }
            public override bool CanSeek { get { return !disposed; } }
            public override void SetLength(long value) { throw new NotSupportedException(); }

        }

    }
}