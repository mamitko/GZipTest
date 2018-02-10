using System;
using System.Diagnostics;
using System.Threading;
using GZipTest.Threading;

namespace GZipTest.Parallelizing
{
    internal class ParallelWorkerBase
    {
        private readonly ParallelSettings settings;
        private readonly ManualResetEvent finishedEvent = new ManualResetEvent(false);

        private readonly BoolFlag started = new BoolFlag(false);
        private readonly BoolFlag finished = new BoolFlag(false);

        private Exception firstHappenedException;
        
        protected ParallelWorkerBase(ParallelSettings settings = default(ParallelSettings))
        {
            this.settings = settings;
            this.settings.Cancellation.RegisterCallback(Cancellation_Canceled);
        }

        public bool IsCanceled { get; private set; }
        
        public void Start()
        {
            if (started.InterlockedCompareAssign(true, false))
                return;

            var combinedCancellation = Cancellation.CreateLinked(settings.Cancellation);

            var threadCount = settings.ForcedDegreeOfParallelism ?? Parallelism.DefaultDegree;
            var threadsFinished = 0;

            Debug.Assert(threadCount > 0);
            threadCount = Math.Max(1, threadCount);
            for (var i = 0; i < threadCount; i++)
            {
                var threadIndex = i;
                var thread = new Thread(
                        () =>
                        {
                            try
                            {
                                DoWork(combinedCancellation, threadCount, threadIndex);
                            }
                            catch (Exception e)
                            {
                                Interlocked.CompareExchange(ref firstHappenedException, e, null);
                                combinedCancellation.Cancel();
                            }
                            finally
                            {
                                if (Interlocked.Increment(ref threadsFinished) == threadCount)
                                {
                                    OnLastWorkerFinishing();
                                    Finish();
                                }
                            }
                        })
                    {IsBackground = true};

                thread.Start();
            }
        }

        public void Wait()
        {
            finishedEvent.WaitOne();

            if (firstHappenedException != null)
                CrossThreadTransferredException.Rethrow(firstHappenedException);

            settings.Cancellation.ThrowIfCanceled();
        }

        protected bool IsFinished => finished.Value;

        protected virtual void DoWork(Cancellation cancellation, int workersTotal, int thisWorkerIndex) {}

        protected virtual void OnCompleteOrCanceled() { }

        protected virtual void OnLastWorkerFinishing() { }

        private void Cancellation_Canceled()
        {
            Finish();
        }
        
        private void Finish() //TODO rename
        {
            if (finished.InterlockedCompareAssign(true, false))
                return;

            IsCanceled = settings.Cancellation.IsCanceled;
            
            OnCompleteOrCanceled();

            finishedEvent.Set();
        }
    }
}