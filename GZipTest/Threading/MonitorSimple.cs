using System;
using System.Threading;

namespace GZipTest.Threading
{
    //todo? insert Thread.MemoryBarier invocations

    public class MonitorSimple: IDisposable
    {
        private readonly ControlFlowQueue _readyQueue = new ControlFlowQueue();

        private int _waitersCount;
        private readonly AutoResetEvent _waitGate = new AutoResetEvent(false); 

        private LockOwningState _owning = LockOwningState.Ownerless;

        public void Enter()
        {
            if (!TryEnter(true))
                throw new InvalidOperationException();

            // Todo something: TryEnter(true) never returns false. 
            // Consider replacing with another exception or even assertion (it's private method so think it's accaptable) 
            // and
            // Split ControlFlowQueue.TryEnter(bool) into Enter() end TryEnter() with no params. 
            // It was supposed to work as a sort of incapsulation, but actually just makes a mess and confuses.
        }
        
        public bool TryEnter()
        {
            return TryEnter(false);
        }

        private bool TryEnter(bool threadBlockingAllowed)
        {
            if (_owning.IsOwnedByCurrentThread)
            {
                _owning.DepthIncreasing();
                return true;
            }
            
            if (!_readyQueue.TryEnter(threadBlockingAllowed))
                return false;

            _owning.Started();
            return true;
        }

        public void Exit()
        {
            _owning.CheckIsOwnedByCurrentThread();

            if (_owning.DepthDecreased() == 0)
                _readyQueue.Exit();
        }

        public void Wait()
        {
            _owning.CheckIsOwnedByCurrentThread();

            Interlocked.Increment(ref _waitersCount);
            // it's vitally important to incremet _waitersCount before releasing _readyQueue. 
            // Otherwise theoretically Pulse() can be invoked before _watersCount is incremented, find no waiters and do not "open" _waitGate.

            var savedOwningState = _owning;
            _owning = LockOwningState.Ownerless;
            _readyQueue.Exit();

            _waitGate.WaitOne();
            Interlocked.Decrement(ref _waitersCount);

            _readyQueue.Enter();
            _owning = savedOwningState;
        }

        public void PulseAll()
        {
            while (Pulse())
            {
            }
        }

        public bool Pulse()
        {
            _owning.CheckIsOwnedByCurrentThread();

            if (Thread.VolatileRead(ref _waitersCount) == 0)
                return false;

            _waitGate.Set();
            return true;
        }

        public LockHandle GetLocked()
        
        {
            Enter();
            return new LockHandle(this);
        }

        public void Dispose()
        {
            _readyQueue.Dispose();
            ((IDisposable)_waitGate).Dispose();
            // Not sure is it good idea or not to invoke Dispose this way. Perhaps there were some reasons to make it protected in 3.5
        }

        
        public struct LockHandle : IDisposable
        {
            private readonly MonitorSimple _source;

            public LockHandle(MonitorSimple source)
            {
                _source = source;
            }

            public void Dispose()
            {
                _source.Exit();
            }
        }
    }
}
