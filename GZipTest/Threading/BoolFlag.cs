using System.Threading;

namespace GZipTest.Threading
{
    //todo rename?
    public class BoolFlag 
    {
        private const int FalseValue = 0;
        private const int TrueValue = 1;

        private int value;

        private static int BoolToInt(bool value)
        {
            return value ? TrueValue : FalseValue;
        }


        public static implicit operator bool(BoolFlag flag)
        {
            return flag.Value;
        }

        public bool Value => value == TrueValue;
      
        public bool InterlockedCompareAssign(bool newValue, bool comparand)
        {
            return Interlocked.CompareExchange(ref value, 
                BoolToInt(newValue), 
                BoolToInt(comparand)) != FalseValue;
        }

        public BoolFlag(): this(false)
        {
        }

        public BoolFlag(bool value)
        {
            this.value = value ? TrueValue : FalseValue;
        }
    }
}