namespace GZipTest.Parallelizing
{
    public struct ParallelSettings
    {
        private readonly Cancellation cancellation;

        public Cancellation Cancellation => cancellation ?? Cancellation.NonCancalable;

        public int? ForcedDegreeOfParallelism { get; }

        public ParallelSettings(Cancellation cancellation, int? forcedDegreeOfParallelism = null)
        {
            this.cancellation = cancellation ?? Cancellation.NonCancalable;
            ForcedDegreeOfParallelism = forcedDegreeOfParallelism;
        }

        public ParallelSettings Modified(int? forcedDegreeOfParallelism = null)
        {
            return new ParallelSettings(Cancellation, forcedDegreeOfParallelism ?? this.ForcedDegreeOfParallelism);
        }
    }
}