using System;
using System.IO;

namespace GZipTest
{
    public static class StreamExtension
    {
        private const int CopyBufferSize = 81920;

        public static bool TryReadLong(this Stream stream, out long value)
        {
            value = 0;
            var buffer = BitConverter.GetBytes(new long());
            var bytesRead = stream.Read(buffer, 0, buffer.Length);
            if (bytesRead < buffer.Length)
                return false;

            value = BitConverter.ToInt64(buffer, 0);
            return true;
        }

        public static void WriteLong(this Stream stream, long value)
        {
            foreach (var b in BitConverter.GetBytes(value))
            {
                stream.WriteByte(b);
            }
        }

        public static void CopyTo(this Stream src, Stream dst)
        {
            int bytesRead;
            var buffer = new byte[CopyBufferSize];
            do
            {
                bytesRead = src.Read(buffer, 0, buffer.Length);
                if (bytesRead > 0)
                    dst.Write(buffer, 0, bytesRead);

            } while (bytesRead > 0);
        }

        public static long CopyAmountTo(this Stream src, Stream dst, long bytesToCopy)
        // тактое странное название, а не просто CopyTo, потому что в Fw 4.0+ второй параметр Stream.CopyTo -- размер буфера
        {
            var buffer = new byte[CopyBufferSize];
            long bytesDone = 0;
            while (true)
            {
                var bitesLeft = bytesToCopy - bytesDone;
                if (bitesLeft <= 0)
                    break;

                var bytesToReadNow = (int)Math.Min(buffer.Length, bitesLeft);

                var bytesRead = src.Read(buffer, 0, bytesToReadNow);
                if (bytesRead <= 0)
                    break;

                dst.Write(buffer, 0, bytesRead);
                bytesDone = bytesDone + bytesRead;
            }

            return bytesDone;
        }
    }
}