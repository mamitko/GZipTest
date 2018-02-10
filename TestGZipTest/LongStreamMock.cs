using System;
using System.IO;

namespace TestGZipTest
{
    public class EndlessStreamMock: Stream
    {
        public override void Flush()
        {
            throw new InvalidOperationException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new InvalidOperationException();
        }

        public override void SetLength(long value)
        {
            throw new InvalidOperationException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var bytesRead = 0;

            for (var i = 0; i < count && Position < Length; i++)
            {
                buffer[offset + i] = 1;
                Position++;
                bytesRead++;
            }

            return bytesRead;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new InvalidOperationException();
        }

        public override long Position { get; set; }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => long.MaxValue;
    }
}