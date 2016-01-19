using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using GZipTest.Threading;

namespace GZipTest.Parallelizing
{
    /// <summary>
    /// Executes an action on each item of IEnumerable. Trys to do it parallely.
    /// </summary>
    internal class ForAll<T>
    {
        private readonly IEnumerable<T> _source;
        private readonly Action<T> _action;
        private readonly ParallelSettings _settings;
        private Action<WorkCompletionInfo> _compeltionCallbacks;

        private EnumerableThreadSafeWrapper<T> _wrappedSource;
        private readonly ManualResetEvent _isCompletedEvent = new ManualResetEvent(false);
        private readonly BoolFlag _started = new BoolFlag();
        
        private WorkCompletionInfo _competionResult;
        private Exception _firstHappenedException;

        public ForAll(IEnumerable<T> source, Action<T> action, ParallelSettings settings = default(ParallelSettings))
        {
            _source = source;

            _settings = settings;
            _action = action;
            _settings.Cancellation = _settings.Cancellation ?? Cancellation.Uncancallable;

            _settings.Cancellation.RegisterCallback(Cancellation_Canceled);
        }

        public WorkCompletionInfo CompletionResult
        {
            get
            {
                _isCompletedEvent.WaitOne();
                return _competionResult;
            }
        }

        public void OnCompleted(Action<WorkCompletionInfo> completedCallback)
        {
            _compeltionCallbacks += completedCallback;
        }

        public void Start()
        {
            if (_started.InterlockedCompareAssign(true, false))
                return;

            _wrappedSource = new EnumerableThreadSafeWrapper<T>(_source);

            var threadCount = _settings.ForcedDegreeOfParallelizm ?? Parallelism.DefaultDegree;
            var threadsFinished = 0;

            Debug.Assert(threadCount > 0);
            for (var i = 0; i < Math.Max(1, threadCount); i++)
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
                            Interlocked.CompareExchange(ref _firstHappenedException, e, null);
                        }
                        finally
                        {
                            if (Interlocked.Increment(ref threadsFinished) == threadCount)
                                OnCompleted();
                        }
                    })
                { IsBackground = true };

                th.Start();
            }
        }

        private void Cancellation_Canceled()
        {
            OnCompleted();
        }

        private bool NothingExceptionalHapened()
        {
            return _firstHappenedException == null && !_settings.Cancellation.IsCanceled;
        }

        private void OnCompleted()
        {
            var result = new WorkCompletionInfo(_firstHappenedException, _settings.Cancellation.IsCanceled);
            if (Interlocked.CompareExchange(ref _competionResult, result, null) != null)
                return;

            if (_wrappedSource != null)
                _wrappedSource.Dispose();

            if (_compeltionCallbacks != null)
                _compeltionCallbacks(result);

            _isCompletedEvent.Set();
        }
    }
}