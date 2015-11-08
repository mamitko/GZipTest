using System;
using System.IO;

namespace TestGZipTest
{
    public class AlmostEndlessStreamMock: Stream
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

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override long Length
        {
            get { return long.MaxValue; }
        }

        public override long Position { get; set; }
    }
}