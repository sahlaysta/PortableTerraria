using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sahlaysta.PortableTerrariaCommon
{
    //converts a write stream to a read stream
    //splits a stream into multiple, each a max size of bytes
    class SplitOutStream
    {
        //input stream
        class InputStream : Stream
        {
            readonly SplitOutStream sos;
            internal InputStream(SplitOutStream sos)
            {
                this.sos = sos;
            }
            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => throw new NotSupportedException();
            public override long Position
            {
                get => throw new NotSupportedException();
                set => throw new NotSupportedException();
            }
            public override void Flush() { }
            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotSupportedException();
            }
            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }
            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return sos.read(buffer, offset, count);
            }
        }

        //output stream
        class OutputStream : Stream
        {
            readonly SplitOutStream sos;
            internal OutputStream(SplitOutStream sos)
            {
                this.sos = sos;
            }
            public override bool CanRead => false;
            public override bool CanSeek => false;
            public override bool CanWrite => true;
            public override long Length => throw new NotSupportedException();
            public override long Position
            {
                get => sos.totalBytesRead;
                set => throw new NotSupportedException();
            }
            public override void Flush() { }
            public override int Read(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException();
            }
            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotSupportedException();
            }
            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }
            public override void Write(byte[] buffer, int offset, int count)
            {
                sos.write(buffer, offset, count);
            }
        }

        readonly InputStream inputStream;
        readonly OutputStream outputStream;
        readonly Action<Stream> writeDataToStream;
        readonly int splitSize;
        volatile byte[] buffer;
        volatile int offset;
        volatile int count;
        volatile int bytesLeft;
        readonly object objLock = new object();
        volatile bool waitingWrite;
        volatile bool ended;
        long totalBytesRead = 0;

        //constructor
        public SplitOutStream(
            Action<Stream> writeDataToStream, int splitSize)
        {
            this.writeDataToStream = writeDataToStream;
            this.splitSize = splitSize;
            inputStream = new InputStream(this);
            outputStream = new OutputStream(this);
            init();
        }

        //public operations
        public Stream Stream { get => inputStream; }

        //initializations
        void init()
        {
            bytesLeft = splitSize;
            waitingWrite = true;
            Task.Run(() =>
            {
                //multithreaded write stream data
                writeDataToStream(outputStream);
                ended = true;
                lock (objLock)
                {
                    Monitor.PulseAll(objLock);
                }
            });

            //wait for first buffer
            lock (objLock)
            {
                while (buffer == null)
                {
                    Monitor.Wait(objLock);
                    if (ended)
                        return;
                }
            }
        }
        int read(byte[] buffer, int offset, int count)
        {
            //write end
            if (this.buffer == null)
                return 0;

            //split end
            if (bytesLeft <= 0)
            {
                bytesLeft = splitSize;
                return 0;
            }

            //read into buffer and set position
            int bytesToRead = Math.Min(count, Math.Min(this.count, bytesLeft));
            Array.Copy(this.buffer, this.offset, buffer, offset, bytesToRead);
            bytesLeft -= bytesToRead;
            totalBytesRead += bytesToRead;
            this.offset += bytesToRead;
            this.count -= bytesToRead;

            //get bytes
            if (this.count <= 0)
                advanceBuffer();
            
            return bytesToRead;
        }

        //paused writeDataToStream
        void write(byte[] buffer, int offset, int count)
        {
            //task end
            if (ended)
                return;

            //set buffer
            lock (objLock)
            {
                this.buffer = buffer;
                this.offset = offset;
                this.count = count;
                Monitor.PulseAll(objLock);
            }

            //start waiting
            lock (objLock)
            {
                while (waitingWrite)
                {
                    Monitor.Wait(objLock);
                    if (ended)
                        return;
                }
                waitingWrite = true;
            }
        }

        //temporarily unpauses writeDataToStream
        void advanceBuffer()
        {
            //task end
            if (ended)
                return;

            //unset buffer
            buffer = null;

            //set waitingWrite to false, pulse waiter
            lock (objLock)
            {
                waitingWrite = false;
                Monitor.PulseAll(objLock);
            }

            //wait for waitingBuffer to be set false
            lock (objLock)
            {
                while (buffer == null)
                {
                    Monitor.Wait(objLock);
                    if (ended)
                        return;
                }
            }
        }
    }
}
