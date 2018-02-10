using System;
using System.IO;
using System.Linq;
using System.Threading;
using GZipTest;
using GZipTest.Parallelizing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestGZipTest
{
    [TestClass]
    public class TestCompression
    {
        //TODO test the other one set of compress-decompress methods (after extraction Compressor, see TODO-notes on Compression class)

        private static byte[] GetCompressed(byte[] array)
        {
            using (var src = new MemoryStream(array))
            {
                using (var dst = new MemoryStream())
                {
                    new Compression().Compress(src, dst);
                    return dst.ToArray();
                }
            }
        }

        private static byte[] GetDecompressed(byte[] compressed)
        {
            using (var src = new MemoryStream(compressed))
            {
                using (var dst = new MemoryStream())
                {
                    new Compression().Decompress(src, dst);
                    return dst.ToArray();
                }
            }
        }

        private static byte[] CompressDecompress(byte[] array)
        {
            var compressed = GetCompressed(array);
            return GetDecompressed(compressed);
        }

        [TestMethod]
        public void TestCompressDecompressBigRandom()
        {
            var original = new byte[Compression.PortionLengthBytes*Parallelism.DefaultDegree*2];
            new Random().NextBytes(original);

            var restored = CompressDecompress(original);
            Assert.IsTrue(restored.SequenceEqual(original));
        }

        [TestMethod]
        public void TestCompressDecompressEmpty()
        {
            var original = new byte[0];
            var restored = CompressDecompress(original);
            Assert.IsTrue(restored.SequenceEqual(original));
        }

        [TestMethod]
        [ExpectedException(typeof(OperationCanceledException))]
        public void TestCancel()
        {
            var compression = new Compression();
            
            var almostEndlessStream = new EndlessStreamMock();
            using (var dst = new MemoryStream())
            {
                var thread = new Thread(() =>
                {
                    Thread.Sleep(TimeSpan.FromSeconds(0.1));
                    compression.Cancel();
                });
                thread.Start();

                compression.Compress(almostEndlessStream, dst);
            }
        }
    }
}
