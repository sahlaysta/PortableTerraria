using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sahlaysta.PortableTerrariaCommon
{
    //combines multiple read streams into one
    class SplitInStream : Stream
    {
        //constructor
        public SplitInStream(Func<Stream> getNextStream)
        {
            this.getNextStream = getNextStream;
            stream = getNextStream();
        }

        //public operations
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => totalBytesRead;
            set => throw new NotSupportedException();
        }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count)
        {
            return read(buffer, offset, count);
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
            throw new NotSupportedException();
        }



        int read(byte[] buffer, int offset, int count)
        {
            if (stream == null)
                return 0;
            int read = stream.Read(buffer, offset, count);
            while (read <= 0) //advance stream concat
            {
                stream.Dispose();
                stream = getNextStream();
                if (stream == null)
                    return 0;
                read = stream.Read(buffer, offset, count);
            }
            totalBytesRead += read;
            return read;
        }

        long totalBytesRead = 0;
        readonly Func<Stream> getNextStream;
        Stream stream;
    }
}
