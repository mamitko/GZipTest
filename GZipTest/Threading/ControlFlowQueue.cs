using System;
using System.Threading;

namespace GZipTest.Threading
{
    internal class ControlFlowQueue : IDisposable
    {
        private readonly AutoResetEvent _kernelLock = new AutoResetEvent(false);
        private int _waitersCount;

        public void Enter()
        {
            TryEnter(threadBlockingAllowed: true);
        }

        internal bool TryEnter(bool threadBlockingAllowed = false)
        {
            const int spinCount = 1000;

            var spinWait = new SpinWaitStolen();
            for (var i = 0; i < spinCount; i++)
            {
                if (Interlocked.CompareExchange(ref _waitersCount, 1, 0) == 0)
                    return true;

                spinWait.SpinOnce();
            }

            if (Interlocked.Increment(ref _waitersCount) == 1)
                return true;

            // trying to follow MSDN:
            // ... If the thread cannot enter without blocking, the method returns false, and the thread does not enter the critical section.
            // i.e. spining is not blocking
            if (!threadBlockingAllowed)
                return false;

            _kernelLock.WaitOne();
            return true;
        }

        public void Exit()
        {
            if (Interlocked.Decrement(ref _waitersCount) > 0)
                _kernelLock.Set();
        }

        public void Dispose()
        {
            ((IDisposable)_kernelLock).Dispose(); 
            // Not sure is it good idea or not to invoke Dispose this way. Perhaps there were some reasons to make it protected in 3.5
        }
    }
}
