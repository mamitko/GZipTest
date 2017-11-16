using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace GZipTest
{
    public partial class Compression
    {
        private class StreamPortion
        {
            public long StartPosition { get; }
            public byte[] Data { get; }

            public StreamPortion(long startPosition, byte[] data)
            {
                StartPosition = startPosition;
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
            
            public void WriteAtItsPlace(Stream stream)
            {
                if (StartPosition + Data.Length > stream.Length)
                    stream.SetLength(StartPosition + Data.Length);

                stream.Position = StartPosition;
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
            private readonly long originalStartPosition;
            private readonly byte[] compressedData;

            private CompressedPortion(long originalStartPosition, byte[] compressedData)
            {
                this.originalStartPosition = originalStartPosition;
                this.compressedData = compressedData;
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

                        return new CompressedPortion(portion.StartPosition, compressedStream.ToArray());
                    }
                }
            }
            
            public void WriteCompressedTo(Stream stream)
            {
                stream.WriteLong(originalStartPosition);
                stream.WriteLong(compressedData.Length);
                stream.Write(compressedData, 0, compressedData.Length);
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
                using (var compressedStream = new MemoryStream(compressedData))
                {
                    using (var zip = new GZipStream(compressedStream, CompressionMode.Decompress))
                    {
                        using (var uncompressedStream = new MemoryStream())
                        {
                            uncompressedStream.Position = 0;
                            zip.CopyTo(uncompressedStream);

                            return new StreamPortion(originalStartPosition, uncompressedStream.ToArray());
                        }
                    }
                }
            }
        }
    }
}