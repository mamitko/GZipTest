using System;
using System.Collections.Generic;
using System.Threading;

namespace GZipTest.Parallelizing
{
    /// <summary>
    /// Executes an action for each item of IEnumerable attempting to do it parallely.
    /// </summary>
    internal class ForAll<T>: ParallelWorkerBase
    {
        private readonly IEnumerable<T> _source;
        private readonly Action<T> _action;
        private Action<ForAll<T>> _onFinishedCallback;
        
        private EnumerableThreadSafeWrapper<T> _wrappedSource;

        protected override void DoWork(Cancellation cancellation, int workersTotal, int thisWorkerIndex)
        {
            var ws = new EnumerableThreadSafeWrapper<T>(_source); 
            if (Interlocked.CompareExchange(ref _wrappedSource, ws, null) != null)
                ws.Dispose();

            T item;
            while (!cancellation.IsCanceled && _wrappedSource.TryGetNext(out item))
            {
                _action(item);
            }
        }

        protected override void OnFinished()
        {
            if (_wrappedSource != null)
                _wrappedSource.Dispose();

            if (_onFinishedCallback != null)
                _onFinishedCallback(this);
        }

        public void RegisterOnfinished(Action<ForAll<T>> onFinishedCallback)
        {
            if (IsFinished)
                onFinishedCallback(this);
            else
                _onFinishedCallback += onFinishedCallback;
        }
        
        public ForAll(IEnumerable<T> source, Action<T> action, ParallelSettings settings = default(ParallelSettings)): base(settings)
        {
            _source = source;
            _action = action;
        }
    }
}