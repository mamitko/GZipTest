using System;
using System.Collections.Generic;
using System.Threading;
using GZipTest.Threading;

namespace GZipTest.Parallelizing
{
    internal class ForAll<T> : IDisposable
    {
        private readonly ParallelSettings _settings;

        private readonly Action<T> _action;
        private readonly IEnumerable<T> _source;
        private EnumerableThreadSafeWrapper<T> _wrappedSource;

        private Exception _happenedExceptions;

        private readonly BoolFlag _started = new BoolFlag();
        private readonly BoolFlag _finished = new BoolFlag();
        
        private bool NothingExceptionalHapened()
        {
            return _happenedExceptions == null && !_settings.Cancellation.IsCanceled;
        }

        private void OnThreadsFinished()
        {
            if (_finished.InterlockedCompareAssign(true, false))
                return;
            
            _settings.Cancellation.Canceled -= Cancellation_Canceled;
            DisposeSourceAdapter();
            OnCompleted( new WorkCompleteEventArgs(_happenedExceptions, _settings.Cancellation.IsCanceled) );
        }

        private void Cancellation_Canceled(object sender, EventArgs args)
        {
            OnThreadsFinished();    
        }

        public ForAll(IEnumerable<T> source, Action<T> action, ParallelSettings settings = default(ParallelSettings))
        {
            _source = source;

            _settings = settings;
            _action = action;
            _settings.Cancellation = _settings.Cancellation ?? Cancellation.Uncancallable;

            _settings.Cancellation.Canceled += Cancellation_Canceled;
        }

        public void Start()
        {
            if (_started.InterlockedCompareAssign(true, false))
                return;
            
            _wrappedSource = new EnumerableThreadSafeWrapper<T>(_source);

            var threadCount = _settings.ForcedDegreeOfParallelizm ?? Parallelism.DefaultDegree;
            var threadsFinished = 0;
            
            for (var i = 0; i < threadCount; i++)
            {
                var th = new Thread(
                    () =>
                    {
                        try
                        {
                            T item;
                            while (NothingExceptionalHapened() && _wrappedSource.TryGetNext(out item))
                            {
                                _action(item);
                            }
                        }
                        catch (Exception e)
                        {
                            Interlocked.CompareExchange(ref _happenedExceptions, e, null);
                        }
                        finally
                        {
                            if (Interlocked.Increment(ref threadsFinished) == threadCount)
                                OnThreadsFinished();
                        }
                    }) 
                    {IsBackground = true};
            
                th.Start();
            }
        }
        
        public event EventHandler<WorkCompleteEventArgs> Completed;

        protected virtual void OnCompleted(WorkCompleteEventArgs e)
        {
            var handler = Completed;
            if (handler != null) handler(this, e);
        }

        private void DisposeSourceAdapter()
        {
            if (_wrappedSource != null)
                _wrappedSource.Dispose();
        }

        public void Dispose()
        {
            DisposeSourceAdapter();
        }
    }
}