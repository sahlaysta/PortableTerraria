using System;
using System.IO;
using System.Threading;

namespace Sahlaysta.PortableTerrariaCommon
{

    /// <summary>
    /// Write bytes in a new thread, and read what was written to that thread via a Stream.
    /// This is useful to essentially convert a write operation into a readable Stream.
    /// </summary>
    internal static class ThreadStreamPiper
    {

        public delegate void DelegateWrite(Stream stream);

        public static Stream ReadPipedWriteInNewThread(DelegateWrite writer)
        {
            Piper piper = new Piper();
            Stream writeEnd = piper.WriteEnd;
            Stream readEnd = piper.ReadEnd;
            Thread thread = new Thread(() =>
            {
                Exception exception = null;
                try
                {
                    using (BufferedStream bufferedStream = new BufferedStream(writeEnd))
                    {
                        writer(bufferedStream);
                    }
                }
                catch (Exception e)
                {
                    exception = e;
                }
                finally
                {
                    piper.OnEnd(exception);
                }
            });
            piper.Thread = thread;
            thread.Start();
            return readEnd;
        }

        private class Piper
        {

            public readonly Stream WriteEnd;
            public readonly Stream ReadEnd;
            public Thread Thread;

            private readonly object monitorObj = new object();

            private bool hasBytesToRead;
            private byte[] onWriteBuffer;
            private int onWriteOffset;
            private int onWriteCount;
            private int onWritePosition;

            private bool end;
            private Exception endException;

            private bool writeDisposed;
            private bool readDisposed;

            public Piper()
            {
                this.WriteEnd = new WriteEndStream(this);
                this.ReadEnd = new ReadEndStream(this);
            }

            public void OnWrite(byte[] buffer, int offset, int count)
            {
                if ((offset | count | (offset + count) | (buffer.Length - (offset + count))) < 0)
                {
                    throw new IndexOutOfRangeException();
                }
                if (count == 0) return;

                lock (monitorObj)
                {
                    if (writeDisposed) { throw new ObjectDisposedException(GetType().FullName); }
                    if (end) return;
                    hasBytesToRead = true;
                    onWriteBuffer = buffer;
                    onWriteOffset = offset;
                    onWriteCount = count;
                    onWritePosition = offset;
                    Monitor.PulseAll(monitorObj);
                    while (hasBytesToRead)
                    {
                        Monitor.Wait(monitorObj);
                    }
                }
            }

            public void OnEnd(Exception exception)
            {
                lock (monitorObj)
                {
                    if (end) return;
                    end = true;
                    hasBytesToRead = false;
                    onWriteBuffer = null;
                    endException = exception;
                    onWriteBuffer = null;
                    Monitor.PulseAll(monitorObj);
                }
            }

            public int OnRead(byte[] buffer, int offset, int count)
            {
                if ((offset | count | (offset + count) | (buffer.Length - (offset + count))) < 0)
                {
                    throw new IndexOutOfRangeException();
                }
                if (count == 0) return 0;

                lock (monitorObj)
                {
                    if (readDisposed) { throw new ObjectDisposedException(GetType().FullName); }
                    if (end)
                    {
                        if (endException != null)
                        {
                            Exception exception = endException;
                            endException = null;
                            throw new Exception("Write pipe threw exception", exception);
                        }
                        else
                        {
                            return 0;
                        }
                    }
                    while (!hasBytesToRead && !end)
                    {
                        Monitor.Wait(monitorObj);
                    }
                    if (end)
                    {
                        if (endException != null)
                        {
                            Exception exception = endException;
                            endException = null;
                            throw new Exception("Write pipe threw exception", exception);
                        }
                        else
                        {
                            return 0;
                        }
                    }
                    int bytesToRead = Math.Min(count, onWriteCount - onWritePosition);
                    Array.Copy(onWriteBuffer, onWritePosition, buffer, offset, bytesToRead);
                    onWritePosition += bytesToRead;
                    if (onWritePosition == onWriteCount - onWriteOffset)
                    {
                        hasBytesToRead = false;
                        onWriteBuffer = null;
                        Monitor.PulseAll(monitorObj);
                    }
                    return bytesToRead;
                }
            }

            public void OnWriteDispose(bool disposing)
            {
                lock (monitorObj)
                {
                    writeDisposed = true;
                }
            }

            public void OnReadDispose(bool disposing)
            {
                lock (monitorObj)
                {
                    if (readDisposed) return;
                    readDisposed = true;
                    if (!disposing) return;
                    Thread?.Abort();
                    Thread = null;
                    Exception theEndException = endException;
                    OnEnd(null);
                    if (theEndException != null)
                    {
                        throw new Exception("Write pipe threw exception", theEndException);
                    }
                }
            }

            public void OnWriteFlush()
            {
                lock (monitorObj)
                {
                    if (writeDisposed) { throw new ObjectDisposedException(GetType().FullName); }
                }
            }

            public void OnReadFlush()
            {
                lock (monitorObj)
                {
                    if (readDisposed) { throw new ObjectDisposedException(GetType().FullName); }
                    if (endException != null)
                    {
                        Exception exception = endException;
                        endException = null;
                        throw new Exception("Write pipe threw exception", exception);
                    }
                }
            }

            public bool CanWrite()
            {
                lock (monitorObj)
                {
                    return !writeDisposed;
                }
            }

            public bool CanRead()
            {
                lock (monitorObj)
                {
                    return !readDisposed;
                }
            }

            private class WriteEndStream : Stream
            {

                private readonly Piper piper;

                public WriteEndStream(Piper piper)
                {
                    this.piper = piper;
                }

                public override void Write(byte[] buffer, int offset, int count)
                {
                    piper.OnWrite(buffer, offset, count);
                }

                protected override void Dispose(bool disposing)
                {
                    piper.OnWriteDispose(disposing);
                }

                public override void Flush()
                {
                    piper.OnWriteFlush();
                }

                public override int Read(byte[] buffer, int offset, int count) {
                    throw new NotSupportedException(); }
                public override bool CanRead { get { return false; } }
                public override bool CanWrite { get { return piper.CanWrite(); } }
                public override bool CanSeek { get { return false; } }
                public override long Seek(long offset, SeekOrigin origin) { throw new NotSupportedException(); }
                public override void SetLength(long value) { throw new NotSupportedException(); }
                public override long Length { get { throw new NotSupportedException(); } }
                public override long Position {
                    get { throw new NotSupportedException(); } set { throw new NotSupportedException(); } }

            }

            private class ReadEndStream : Stream
            {

                private readonly Piper piper;

                public ReadEndStream(Piper piper)
                {
                    this.piper = piper;
                }

                public override int Read(byte[] buffer, int offset, int count)
                {
                    return piper.OnRead(buffer, offset, count);
                }

                protected override void Dispose(bool disposing)
                {
                    piper.OnReadDispose(disposing);
                }

                public override void Flush()
                {
                    piper.OnReadFlush();
                }

                public override void Write(byte[] buffer, int offset, int count) {
                    throw new NotSupportedException(); }
                public override bool CanRead { get { return piper.CanRead(); } }
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
}