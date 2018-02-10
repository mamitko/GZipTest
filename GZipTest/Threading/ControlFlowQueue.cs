using System;
using System.Threading;

namespace GZipTest.Threading
{
    internal class ControlFlowQueue : IDisposable
    {
        private readonly AutoResetEvent kernelLock = new AutoResetEvent(false);
        private int waitersCount;

        public void Enter()
        {
            TryEnter(threadBlockingAllowed: true);
        }

        internal bool TryEnter(bool threadBlockingAllowed = false)
        {
            const int SpinCount = 1000;

            if (!threadBlockingAllowed)
                return Interlocked.CompareExchange(ref waitersCount, 1, 0) == 0;

            var spinWait = new SpinWait();
            for (var i = 0; i < SpinCount; i++)
            {
                if (Interlocked.CompareExchange(ref waitersCount, 1, 0) == 0)
                    return true;

                spinWait.SpinOnce();
            }

            if (Interlocked.Increment(ref waitersCount) == 1)
                return true;
                        
            kernelLock.WaitOne();
            return true;
        }

        public void Exit()
        {
            if (Interlocked.Decrement(ref waitersCount) > 0)
                kernelLock.Set();
        }

        public void Dispose()
        {
            ((IDisposable)kernelLock).Dispose(); 
            // Not sure is it good idea or not to invoke Dispose this way. Perhaps there were some reasons to make it protected in 3.5
        }
    }
}
