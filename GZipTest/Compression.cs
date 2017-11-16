using System;
using System.IO;
using GZipTest.Parallelizing;

namespace GZipTest
{
    //TODO extract something like abstract "Compressor" (with two descendants) class to get rid of two set of public methods

    public partial class Compression
    {
        internal const int PortionLengthBytes = 5 * (1 << 20);

        private const int OutputBufferSizePcs = 50*(1 << 20)/PortionLengthBytes;
        private static int PreReadBufferSizePcs => Parallelism.DefaultDegree;

        private Cancellation cancellation;
        
        public void Compress(Stream src, Stream dst)
        {
            cancellation = new Cancellation();

            var compressed = StreamPortion.SplitStream(src, PortionLengthBytes)
                .Buffered(cancellation, PreReadBufferSizePcs)
                .SelectParallely(portion => CompressedPortion.Compress(portion), cancellation)
                .WithBoundedOutputCapacity(OutputBufferSizePcs);

            foreach (var chunk in compressed)
            {
                chunk.WriteCompressedTo(dst);
                OnProgressChanged();
            }
        }

        public void Decompress(Stream src, Stream dst)
        {
            cancellation = new Cancellation();

            var decompressed = CompressedPortion.ReadAllFrom(src)
                .Buffered(cancellation, PreReadBufferSizePcs)
                .SelectParallely(chunk => chunk.Decompress(), cancellation) 
                .WithBoundedOutputCapacity(OutputBufferSizePcs);

            foreach (var portion in decompressed)
            {
                portion.WriteAtItsPlace(dst);
                OnProgressChanged();
            }
        }
        
        public void CompressEtude(Stream src, Stream dst)
        {
            cancellation = new Cancellation();

            var compressed = StreamPortion.SplitStream(src, PortionLengthBytes)
                .SelectParallelyEtude(portion => CompressedPortion.Compress(portion), cancellation, PreReadBufferSizePcs, OutputBufferSizePcs);

            foreach (var chunk in compressed)
            {
                chunk.WriteCompressedTo(dst);
                OnProgressChanged();
            }
        }

        public void DecompressEtude(Stream src, Stream dst)
        {
            cancellation = new Cancellation();

            var decompressed = CompressedPortion.ReadAllFrom(src)
                .SelectParallelyEtude(chunk => chunk.Decompress(), cancellation, PreReadBufferSizePcs, OutputBufferSizePcs);

            foreach (var portion in decompressed)
            {
                portion.WriteAtItsPlace(dst);
                OnProgressChanged();
            }
        }

        public void Cancel()
        {
            cancellation?.Cancel();
        }

        public event EventHandler ProgressChanged;

        private void OnProgressChanged()
        {
            ProgressChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}