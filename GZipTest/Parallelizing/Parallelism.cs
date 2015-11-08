using System;

namespace GZipTest.Parallelizing
{
    public static class Parallelism
    {
        public static int DefaultDegree { get { return Environment.ProcessorCount; } }
    }
}