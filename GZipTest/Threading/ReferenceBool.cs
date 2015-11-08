namespace GZipTest.Threading
{
    /// <summary>
    /// В настоящем коде не использовать!
    /// Класс таким создан исключительно в экспериментальных целях 
    /// и из иррацональных эстетических побуждений (для мимикрии под bool, а она вредна).
    /// </summary>
    internal sealed class ReferenceBool
    {
        private static readonly ReferenceBool TrueInstance = new ReferenceBool();

        private ReferenceBool() { }

        public static implicit operator bool(ReferenceBool value)
        {
            return value != null;
        }

        public static implicit operator ReferenceBool(bool value)
        {
            return value ? TrueInstance : null; 
        }
    }
}