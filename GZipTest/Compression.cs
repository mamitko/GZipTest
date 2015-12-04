using System.IO;
using GZipTest.Parallelizing;

namespace GZipTest
{
    public partial class Compression
    {
        internal const int PortionLengthBytes = 5 * (1 << 20);
        private const int OutputBufferSizePcs = 50*(1 << 20)/PortionLengthBytes;
        private static int PrereadBufferSizePcs { get { return Parallelism.DefaultDegree; } }
        
        private Cancellation _cancellation;
        
        public void Compress(Stream src, Stream dst)
        {
            _cancellation = new Cancellation();

            var compressed = StreamPortion.SplitStream(src, PortionLengthBytes)
                .Materialized(_cancellation, PrereadBufferSizePcs) // This line is optional but for certain reasons it emproves CPU utilization.
                .SelectParallely(portion => CompressedPortion.Compress(portion), _cancellation)
                .WithBoundedOutputCapacity(OutputBufferSizePcs); // This line is a sort of "optional" one too. It prevents OutOfMemmoryException on large files or with slow output disk storages.

            foreach (var chunk in compressed)
            {
                chunk.WriteCompressedTo(dst);
            }
        }

        public void Decompress(Stream src, Stream dst)
        {
            _cancellation = new Cancellation();

            var decompressed = CompressedPortion.ReadAllFrom(src)
                .Materialized(_cancellation, PrereadBufferSizePcs) // This line is optional but for certain reasons it emproves CPU utilization.
                .SelectParallely(chunk => chunk.Decompress(), _cancellation)
                .WithBoundedOutputCapacity(OutputBufferSizePcs); // This line is a sort of "optional" one too. It prevents OutOfMemmoryException on large files or with slow output disk storages.

            foreach (var portion in decompressed)
            {
                portion.WriteToItsPlace(dst);
            }
        }

        public void Cancel()
        {
            if (_cancellation == null) 
                return;

            _cancellation.Cancel();
        }
    }
}