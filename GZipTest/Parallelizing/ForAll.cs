using System;
using System.Collections.Generic;
using System.Threading;

namespace GZipTest.Parallelizing
{
    /// <summary>
    /// Executes an action for each item of IEnumerable attempting to do it in parallel.
    /// </summary>
    internal class ForAll<T>: ParallelWorkerBase
    {
        private readonly IEnumerable<T> source;
        private readonly Action<T> action;

        private Action<ForAll<T>> onFinishedCallback;
        private EnumerableThreadSafeWrapper<T> wrappedSource;

        protected override void DoWork(Cancellation cancellation, int workersTotal, int thisWorkerIndex)
        {
            if (wrappedSource == null)
            {
                var ws = new EnumerableThreadSafeWrapper<T>(source);
                if (Interlocked.CompareExchange(ref wrappedSource, ws, null) != null)
                    ws.Dispose();
            }

            T item;
            while (!cancellation.IsCanceled && wrappedSource.TryGetNext(out item))
            {
                action(item);
            }
        }

        protected override void OnCompleteOrCanceled()
        {
            onFinishedCallback?.Invoke(this);
        }

        protected override void OnLastWorkerFinishing()
        {
            wrappedSource?.Dispose();
        }

        public void RegisterOnFinished(Action<ForAll<T>> callback)
        {
            // TODO !!! what if got finished "after" checking IsFinished but "before" subscribing?
            // TODO consider moving these all into base class (since any kind "worker" can finish work)

            if (IsFinished)
                callback(this);
            else
                onFinishedCallback += callback;
        }

        public ForAll(IEnumerable<T> source, Action<T> action, ParallelSettings settings = default(ParallelSettings))
            : base(settings)
        {
            this.source = source;
            this.action = action;
        }
    }
}