namespace GZipTest.Parallelizing
{
    public struct ParallelSettings
    {
        // структура мутабельной сделана намеренно

        private Cancellation _cancellation;

        public Cancellation Cancellation
        {
            get { return _cancellation ?? Cancellation.Uncancallable; } // не уверен, что хорошая идея, но пользоваться вроде бы удобно
            set { _cancellation = value; }
        }
        public int? ForcedDegreeOfParallelizm { get; set; }
    }
}