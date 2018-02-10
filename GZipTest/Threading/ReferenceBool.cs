namespace GZipTest.Threading
{
    /// <summary>
    /// This class is a kind of experiment and not intended to be used in production code
    /// </summary>
    internal sealed class ReferenceBool
    {
        private static readonly ReferenceBool trueInstance = new ReferenceBool();

        private ReferenceBool() { }

        public static implicit operator bool(ReferenceBool value)
        {
            return value != null;
        }

        public static implicit operator ReferenceBool(bool value)
        {
            return value ? trueInstance : null; 
        }
    }
}