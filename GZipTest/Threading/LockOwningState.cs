using System.Diagnostics;
using System.Threading;

namespace GZipTest.Threading
{
    internal struct LockOwningState
    // This struct is mutable (even worse, it has modifying methods) so it should be used with care, 
    // e.g. do not use it as type of properties or readonly fields, do not copy, unless you exactly know what are you doing.
    {
        public static readonly LockOwningState Ownerless = default(LockOwningState);

        private int _threadId;
        private int _depth;

        public void Started()
        {
            Debug.Assert(_depth == 0);

            _threadId = Thread.CurrentThread.ManagedThreadId;
            _depth = 1;
        }

        public void DepthIncreasing()
        {
            _depth++;
        }

        public int DepthDecreased()
        {
            Debug.Assert(_depth > 0);

            _depth--;

            if (_depth == 0)
                _threadId = 0;

            return _depth;
        }

        public bool IsOwnedByCurrentThread
        {
            get { return _threadId == Thread.CurrentThread.ManagedThreadId; }
        }

        public void CheckIsOwnedByCurrentThread()
        {
            if (_threadId != Thread.CurrentThread.ManagedThreadId)
                throw new SynchronizationLockException();
        }
    }
}
