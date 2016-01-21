using System;
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
                .Buffered(_cancellation, PrereadBufferSizePcs) // This line is optional but for certain reasons it increses CPU utilisation.
                .SelectParallely(portion => CompressedPortion.Compress(portion), _cancellation)
                .WithBoundedOutputCapacity(OutputBufferSizePcs); // This line prevents OutOfMemmoryException on large files or with slow output disk storages.

            foreach (var chunk in compressed)
            {
                chunk.WriteCompressedTo(dst);
                OnProgressChanged();
            }
        }

        public void Decompress(Stream src, Stream dst)
        {
            _cancellation = new Cancellation();

            var decompressed = CompressedPortion.ReadAllFrom(src)
                .Buffered(_cancellation, PrereadBufferSizePcs) // This line is optional but for certain reasons it increses CPU utilisation.
                .SelectParallely(chunk => chunk.Decompress(), _cancellation)
                .WithBoundedOutputCapacity(OutputBufferSizePcs); // This line prevents OutOfMemmoryException on large files or with slow output disk storages.

            foreach (var portion in decompressed)
            {
                portion.WriteToItsPlace(dst);
                OnProgressChanged();
            }
        }

        // As Short As I Was Able
        public void CompressAsaiwa(Stream src, Stream dst)
        {
            _cancellation = new Cancellation();

            var compressed = StreamPortion.SplitStream(src, PortionLengthBytes)
                .SelectParallellyAsaiwa(portion => CompressedPortion.Compress(portion), _cancellation);

            foreach (var chunk in compressed)
            {
                chunk.WriteCompressedTo(dst);
            }
        }

        public void DecompressAsaiwa(Stream src, Stream dst)
        {
            _cancellation = new Cancellation();

            var decompressed = CompressedPortion.ReadAllFrom(src)
                .SelectParallellyAsaiwa(chunk => chunk.Decompress(), _cancellation);

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

        public event EventHandler ProgressChanged;

        protected virtual void OnProgressChanged()
        {
            ProgressChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}