using System;
using System.Collections.Generic;
using System.Threading;
using GZipTest.Threading;

namespace GZipTest.Parallelizing
{
    internal class EnumerableThreadSafeWrapper<T> : IDisposable
    // идея так себе:
    //  - из обычного enumerable эффективнее читать одним потоком в буффер;
    //  - между многопоточными источниками-приемниками это будет "узким местом"
    {
        private readonly IEnumerable<T> source;
        private readonly MonitorSimple sourceLock = new MonitorSimple();

        private IEnumerator<T> sourceEnumerator;
        private ReferenceBool disposeRequested;
        private int timesTriedToGetNext;

        public EnumerableThreadSafeWrapper(IEnumerable<T> source)
        {
            this.source = source;
        }
        
        public bool TryGetNext(out T item)
        {
            if (disposeRequested)
                throw new ObjectDisposedException(ToString());
            
            item = default(T);
            if (disposeRequested)
                return false;

            Interlocked.Increment(ref timesTriedToGetNext);
            try
            {
                using (sourceLock.GetLocked())
                {                    
                    if (disposeRequested)
                        return false;
                                        
                    if (sourceEnumerator == null)
                        sourceEnumerator = source.GetEnumerator();

                    if (!sourceEnumerator.MoveNext())                                           
                        return false;

                    item = sourceEnumerator.Current;
                    return true;
                }
            }
            finally
            {
                if (Interlocked.Decrement(ref timesTriedToGetNext) == 0 & disposeRequested)
                    DisposeInternals();
            }
       } 

        private void DisposeInternals()
        {
            sourceEnumerator?.Dispose();
            sourceLock.Dispose();
        }

        public void Dispose()
        {
            if (Interlocked.CompareExchange(ref disposeRequested, true, false))
                return;

            // if source is busy, just leave scheduled disposeRequested and return.
            // Real disposal will be done on the end of last TryGetNext()
            if (!sourceLock.TryEnter())
                return;
            try
            {
                DisposeInternals();
            }
            finally
            {
                sourceLock.Exit();                
            }
        }
    }
}