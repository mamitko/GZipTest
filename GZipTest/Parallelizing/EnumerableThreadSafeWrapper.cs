using System;
using System.Collections.Generic;
using System.Threading;
using GZipTest.Threading;

namespace GZipTest.Parallelizing
{
    internal class EnumerableThreadSafeWrapper<T> : IDisposable
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
            // if (_disposeReqeusted)
            //    throw new ObjectDisposedException(ToString());
            // Think it's not good idea to throw ObjectDisposedException here since hypothetically TryGetNext can be invoked, and then "paused" "before" even started.
            // At the same time Dispose can be invoked at another thread (e.g. in case of cancelling some operation). This will cause exception at seems to be a legal TryGetNext invocation.
            
            // Asume normaly Dispose is not supposed be invoked before "last" TryGetNext() returns.
            // If it is, let's treat it as cancelation, as in general, cancellation seems to be the only way it could happened;
            
            // In other words noone will knownly dispose an object before getting everything he/she wants from it.

            // It's not 100%-surely Framework Design Guidelines complaint, but they are not 100% clear about when to throw ObjectDisposedException (and when not),
            // and says completly nothing about implementing and using IDisposable in multithreading environment.

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