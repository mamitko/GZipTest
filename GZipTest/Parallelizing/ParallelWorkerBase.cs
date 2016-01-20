using System;
using System.Diagnostics;
using System.Threading;
using GZipTest.Threading;

namespace GZipTest.Parallelizing
{
    internal class ParallelWorkerBase
    {
        private readonly ParallelSettings _settings;
        
        private readonly ManualResetEvent _finishedEvent = new ManualResetEvent(false);

        private readonly BoolFlag _started = new BoolFlag(false);
        private readonly BoolFlag _finished = new BoolFlag(false);

        private Exception _firstHappenedException;
        
        protected ParallelWorkerBase(ParallelSettings settings = default(ParallelSettings))
        {
            _settings = settings;
            _settings.Cancellation = _settings.Cancellation ?? Cancellation.Uncancallable;
            _settings.Cancellation.RegisterCallback(Cancellation_Canceled);
        }

        public bool IsCancelled { get; private set; }
        
        public void Start()
        {
            if (_started.InterlockedCompareAssign(true, false))
                return;

            var combinedCancellation = Cancellation.CreateLinked(_settings.Cancellation);

            var threadCount = _settings.ForcedDegreeOfParallelizm ?? Parallelism.DefaultDegree;
            var threadsFinished = 0;

            Debug.Assert(threadCount > 0);
            threadCount = Math.Max(1, threadCount);
            for (var i = 0; i < threadCount; i++)
            {
                var threadIndex = i;
                var th = new Thread(
                    () =>
                    {
                        try
                        {
                            DoWork(combinedCancellation, threadCount, threadIndex);
                        }
                        catch (Exception e)
                        {
                            Interlocked.CompareExchange(ref _firstHappenedException, e, null);
                            combinedCancellation.Cancel();
                        }
                        finally
                        {
                            if (Interlocked.Increment(ref threadsFinished) == threadCount)
                                Finish();
                        }
                    })
                { IsBackground = true };

                th.Start();
            }
        }

        public void Wait()
        {
            _finishedEvent.WaitOne();

            if (_firstHappenedException != null)
                CrossThreadTransferredException.Rethrow(_firstHappenedException);

            //https://msdn.microsoft.com/en-us/library/system.componentmodel.asynccompletedeventargs.error(v=vs.110).aspx
            //The value of the Error property is null if the operation was canceled.

            _settings.Cancellation.ThrowExceptionIfCancelled();
        }

        protected bool IsFinished { get { return _finished; } }

        protected virtual void DoWork(Cancellation cancellation, int workersTotal, int thisWorkerIndex) {}

        protected virtual void OnFinished() { }

        private void Cancellation_Canceled()
        {
            Finish();
        }
        
        private void Finish() // rename
        {
            if (_finished.InterlockedCompareAssign(true, false))
                return;

            IsCancelled = _settings.Cancellation.IsCanceled;
            
            OnFinished();

            _finishedEvent.Set();
        }
    }
}