using System.Threading;

namespace GZipTest.Threading
{
    public class BoolFlag //tdoo rename
    {
        private const int FalseValue = 0;
        private const int TrueValue = 1;

        private int _value;

        private static int BoolToInt(bool value)
        {
            return value ? TrueValue : FalseValue;
        }


        public static implicit operator bool(BoolFlag flag)
        {
            return flag._value == TrueValue;
        }
      
        public bool InterlockedCompareAssign(bool newValue, bool comparand)
        {
            return Interlocked.CompareExchange(ref _value, 
                BoolToInt(newValue), 
                BoolToInt(comparand)) != FalseValue;
        }

        public BoolFlag(): this(false)
        {
        }

        public BoolFlag(bool value)
        {
            _value = value ? TrueValue : FalseValue;
        }
    }
}