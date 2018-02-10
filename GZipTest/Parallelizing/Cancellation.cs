using System;
using System.Threading;
using GZipTest.Threading;

namespace GZipTest.Parallelizing
{
    public class Cancellation
    {
        public static readonly Cancellation NonCancalable = new Cancellation();

        public static Cancellation CreateLinked(Cancellation source)
        {
            var cancellation = new Cancellation();
            source.RegisterCallback(() =>
            {
                cancellation.Cancel();
            });

            return cancellation;
        }

        private readonly BoolFlag isCanceled = new BoolFlag(false);
        private Action onCanceledCallbacks;

        public bool IsCanceled => isCanceled.Value;

        public void Cancel()
        {
            if (this == NonCancalable)
                return;

            if (isCanceled.InterlockedCompareAssign(true, false))
                return;

            onCanceledCallbacks?.Invoke();
        }

        public void ThrowIfCanceled()
        {
            if (isCanceled)
            {
                throw new OperationCanceledException();
            }
        }

        public void RegisterCallback(Action canceledCallback)
        {
            if (IsCanceled)
            {
                canceledCallback();
                return;
            }

            var sw = new SpinWait();
            while (true)
            {
                var wasInstance = onCanceledCallbacks;
                var newInstance = wasInstance + canceledCallback;

                if (Interlocked.CompareExchange(ref onCanceledCallbacks, newInstance, wasInstance) == wasInstance)
                    break;

                sw.SpinOnce();
            }
        }
    }
}