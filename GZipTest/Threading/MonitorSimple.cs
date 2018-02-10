using System;
using System.Threading;

namespace GZipTest.Threading
{
 public class MonitorSimple: IDisposable
    {
        private readonly ControlFlowQueue readyQueue = new ControlFlowQueue();

        private int waitersCount;
        private readonly AutoResetEvent waitGate = new AutoResetEvent(false); 

        private LockOwningState owning = LockOwningState.Ownerless;

        public void Enter()
        {
            if (!TryEnter(true))
                throw new InvalidOperationException();

            // Todo something: TryEnter(true) never returns false. 
            // Consider replacing with another exception or even assertion (it's private method so think it's acceptable) 
            // and
            // Split ControlFlowQueue.TryEnter(bool) into Enter() end TryEnter() with no params. 
            // It was supposed to work as a sort of encapsulation, but actually just makes a mess and confuses.
        }
        
        public bool TryEnter()
        {
            return TryEnter(false);
        }

        private bool TryEnter(bool threadBlockingAllowed)
        {
            if (owning.IsOwnedByCurrentThread)
            {
                owning.DepthIncreasing();
                return true;
            }
            
            if (!readyQueue.TryEnter(threadBlockingAllowed))
                return false;

            owning.Started();
            return true;
        }

        public void Exit()
        {
            owning.CheckIsOwnedByCurrentThread();

            if (owning.DepthDecreased() == 0)
                readyQueue.Exit();
        }

        public void Wait()
        {
            owning.CheckIsOwnedByCurrentThread();

            Interlocked.Increment(ref waitersCount);
            // Increment _waitersCount before releasing _readyQueue. 
            // Otherwise theoretically Pulse() can be invoked before _watersCount is incremented, find no waiters and do not "open" _waitGate.

            var savedOwningState = owning;
            owning = LockOwningState.Ownerless;
            readyQueue.Exit();

            waitGate.WaitOne();
            Interlocked.Decrement(ref waitersCount);

            readyQueue.Enter();
            owning = savedOwningState;
        }

        public void PulseAll()
        {
            while (Pulse())
            {
            }
        }

        public bool Pulse()
        {
            owning.CheckIsOwnedByCurrentThread();

            if (Thread.VolatileRead(ref waitersCount) == 0)
                return false;

            waitGate.Set();
            return true;
        }

        public LockHandle GetLocked()
        
        {
            Enter();
            return new LockHandle(this);
        }

        public void Dispose()
        {
            readyQueue.Dispose();
            ((IDisposable)waitGate).Dispose();
            // Not sure is it good idea or not to invoke Dispose this way. Perhaps there were some reasons to make it protected in 3.5
        }

        
        public struct LockHandle : IDisposable
        {
            private readonly MonitorSimple source;

            public LockHandle(MonitorSimple source)
            {
                this.source = source;
            }

            public void Dispose()
            {
                source.Exit();
            }
        }
    }
}
