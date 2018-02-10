using System.Diagnostics;
using System.Threading;

namespace GZipTest.Threading
{
    internal struct LockOwningState
    // This structure is mutable (even worse, it has modifying methods) so it should be used with care, 
    // e.g. do not use it as type of properties or readonly fields, do not copy, unless you exactly know what are you doing.
    {
        public static readonly LockOwningState Ownerless = default(LockOwningState);

        private int threadId;
        private int depth;

        public void Started()
        {
            Debug.Assert(depth == 0);

            threadId = Thread.CurrentThread.ManagedThreadId;
            depth = 1;
        }

        public void DepthIncreasing()
        {
            depth++;
        }

        public int DepthDecreased()
        {
            Debug.Assert(depth > 0);

            depth--;

            if (depth == 0)
                threadId = 0;

            return depth;
        }

        public bool IsOwnedByCurrentThread => threadId == Thread.CurrentThread.ManagedThreadId;

        public void CheckIsOwnedByCurrentThread()
        {
            if (threadId != Thread.CurrentThread.ManagedThreadId)
                throw new SynchronizationLockException();
        }
    }
}
