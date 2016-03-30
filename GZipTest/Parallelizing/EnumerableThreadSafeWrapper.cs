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
        private readonly IEnumerable<T> _source;
        private readonly MonitorSimple _sourceLock = new MonitorSimple();

        private IEnumerator<T> _sourceEnumerator;
        private ReferenceBool _disposeReqeusted;
        private int _timesTriedToGetNext;

        public EnumerableThreadSafeWrapper(IEnumerable<T> source)
        {
            _source = source;
        }
        
        public bool TryGetNext(out T item)
        {
            if (_disposeReqeusted)
                throw new ObjectDisposedException(ToString());
            
            item = default(T);
            if (_disposeReqeusted)
                return false;

            Interlocked.Increment(ref _timesTriedToGetNext);
            try
            {
                using (_sourceLock.GetLocked())
                {                    
                    if (_disposeReqeusted)
                        return false;
                                        
                    if (_sourceEnumerator == null)
                        _sourceEnumerator = _source.GetEnumerator();

                    if (!_sourceEnumerator.MoveNext())                                           
                        return false;

                    item = _sourceEnumerator.Current;
                    return true;
                }
            }
            finally
            {
                if (Interlocked.Decrement(ref _timesTriedToGetNext) == 0 & _disposeReqeusted)
                    DisposeInternals();
            }
       } 

        private void DisposeInternals()
        {            
            if (_sourceEnumerator != null)
                _sourceEnumerator.Dispose();

            _sourceLock.Dispose();
        }

        public void Dispose()
        {
            if (Interlocked.CompareExchange(ref _disposeReqeusted, true, false))
                return;

            // if source is busy, just leave scheduled _disposeReqeusted and return.
            // Real disposal will be done on the end of last TryGetNext()
            if (!_sourceLock.TryEnter())
                return;
            try
            {
                DisposeInternals();
            }
            finally
            {
                _sourceLock.Exit();                
            }
        }
    }
}