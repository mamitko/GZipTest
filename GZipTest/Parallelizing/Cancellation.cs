using System;
using System.Threading;
using GZipTest.Threading;

namespace GZipTest.Parallelizing
{
    public class Cancellation
    {
        public static readonly Cancellation Uncancallable = new Cancellation();

        private Action _onCancelledCallbacks;
        private readonly BoolFlag _isCanceled = new BoolFlag(false);
        
        public void Cancel()
        {
            if (this == Uncancallable)
                return;

            if (_isCanceled.InterlockedCompareAssign(true, false))
                return;
            
            if (_onCancelledCallbacks != null)
                _onCancelledCallbacks();

            _onCancelledCallbacks = null;
        }

        public bool IsCanceled { get { return _isCanceled; } }
        
        public void ThrowExceptionIfCancelled()
        {
            if (_isCanceled)
            {
                throw new OperationCanceledException();
            }
        }

        public void RegisterCallback(Action onCacelledCallback)
        {
            var sw = new SpinWaitStolen();
            while (true)
            {
                var wasInstance = _onCancelledCallbacks;
                var newInstance = wasInstance + onCacelledCallback; 
                if (Interlocked.CompareExchange(ref _onCancelledCallbacks, newInstance, wasInstance) == wasInstance)
                    break;
                sw.SpinOnce();
            }
        }
    }
}