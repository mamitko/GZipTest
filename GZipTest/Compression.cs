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
        
        // Тут две реализации (две пары методов Compress-Decompress): подлиннее и покороче
        
        public void Compress(Stream src, Stream dst)
        {
            _cancellation = new Cancellation();

            var compressed = StreamPortion.SplitStream(src, PortionLengthBytes)
                .Buffered(_cancellation, PrereadBufferSizePcs) // This line is optional but for certain reasons it increases CPU utilization.
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
                .Buffered(_cancellation, PrereadBufferSizePcs) // This line is optional but for certain reasons it increases CPU utilization.
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
                .SelectParallellyAsaiwa(portion => CompressedPortion.Compress(portion), _cancellation, PrereadBufferSizePcs, OutputBufferSizePcs);

            foreach (var chunk in compressed)
            {
                chunk.WriteCompressedTo(dst);
                OnProgressChanged();
            }
        }

        public void DecompressAsaiwa(Stream src, Stream dst)
        {
            _cancellation = new Cancellation();

            var decompressed = CompressedPortion.ReadAllFrom(src)
                .SelectParallellyAsaiwa(chunk => chunk.Decompress(), _cancellation, PrereadBufferSizePcs, OutputBufferSizePcs);

            foreach (var portion in decompressed)
            {
                portion.WriteToItsPlace(dst);
                OnProgressChanged();
            }
        }

        public void Cancel()
        {
            if (_cancellation == null) 
                return;

            _cancellation.Cancel();
        }

        public event EventHandler ProgressChanged;

        private void OnProgressChanged()
        {
            ProgressChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}