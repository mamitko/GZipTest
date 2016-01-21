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

        public static Cancellation CreateLinked(Cancellation source)
        {
            var cancellation = new Cancellation();
            source.RegisterCallback(() =>
            {
                cancellation.Cancel();
            });
            return cancellation;
        }

        public void Cancel()
        {
            if (this == Uncancallable)
                return;

            if (_isCanceled.InterlockedCompareAssign(true, false))
                return;
            
            if (_onCancelledCallbacks != null)
                _onCancelledCallbacks();
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
            if (IsCanceled)
            {
                onCacelledCallback();
                return;
            }

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