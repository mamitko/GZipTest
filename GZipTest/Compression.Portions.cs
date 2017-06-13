using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;

namespace GZipTest
{
    public partial class Compression
    {
        private class StreamPortion
        {
            public long StartPostion { get; private set; }
            public byte[] Data { get; private set; }

            public StreamPortion(long startPostion, byte[] data)
            {
                StartPostion = startPostion;
                Data = data;
            }

            public static IEnumerable<StreamPortion> SplitStream(Stream stream, int portionLength)
            {
                StreamPortion chunk;
                while (TryRead(stream, portionLength, out chunk))
                {
                    yield return chunk;
                }
            }
            
            public void WriteToItsPlace(Stream stream)
            {
                if (StartPostion + Data.Length > stream.Length)
                    stream.SetLength(StartPostion + Data.Length);

                stream.Position = StartPostion;
                stream.Write(Data, 0, Data.Length);
            }

            private static bool TryRead(Stream stream, int length, out StreamPortion chunk)
            {
                chunk = null;
                var streamPosition = stream.Position;
                using (var memStream = new MemoryStream())
                {
                    if (stream.CopyBytesTo(memStream, length) == 0)
                        return false;

                    chunk = new StreamPortion(streamPosition, memStream.ToArray());
                    return true;
                }
            }
        }

        private class CompressedPortion
        {
            private readonly long _originalStartPostion;
            private readonly byte[] _zippedData;

            private CompressedPortion(long originalStartPostion, byte[] zippedData)
            {
                _originalStartPostion = originalStartPostion;
                _zippedData = zippedData;
            }

            private static bool TryReadNext(Stream stream, out CompressedPortion chunk)
            {
                chunk = null;

                long startPosition;
                long chunkLength;
                if (!stream.TryReadLong(out startPosition) || !stream.TryReadLong(out chunkLength))
                    return false;

                var data = new byte[chunkLength];
                stream.Read(data, 0, (int)chunkLength);

                chunk = new CompressedPortion(startPosition, data);

                return true;
            }

            public static CompressedPortion Compress(StreamPortion portion)
            {
                using (var uncompressedStream = new MemoryStream(portion.Data))
                {
                    using (var compressedStream = new MemoryStream())
                    {
                        using (var zipStream = new GZipStream(compressedStream, CompressionMode.Compress))
                        {
                            uncompressedStream.CopyTo(zipStream);
                        }

                        return new CompressedPortion(portion.StartPostion, compressedStream.ToArray());
                    }
                }
            }
            
            public void WriteCompressedTo(Stream stream)
            {
                stream.WriteLong(_originalStartPostion);
                stream.WriteLong(_zippedData.Length);
                stream.Write(_zippedData, 0, _zippedData.Length);
            }
            
            public static IEnumerable<CompressedPortion> ReadAllFrom(Stream stream)
            {
                CompressedPortion chunk;
                while (TryReadNext(stream, out chunk))
                {
                    yield return chunk;
                }
            }

            public StreamPortion Decompress()
            {
                using (var compressedStream = new MemoryStream(_zippedData))
                {
                    using (var zip = new GZipStream(compressedStream, CompressionMode.Decompress))
                    {
                        using (var uncompressedStream = new MemoryStream())
                        {
                            uncompressedStream.Position = 0;
                            zip.CopyTo(uncompressedStream);

                            return new StreamPortion(_originalStartPostion, uncompressedStream.ToArray());
                        }
                    }
                }
            }
        }
    }
}