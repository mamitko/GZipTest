using System;
using System.Collections;
using System.Collections.Generic;

namespace TestGZipTest
{
    public class DisposableEnumerator<T>: IEnumerable<T>, IEnumerator<T>
    {
        private readonly IEnumerator<T> _origin;

        public DisposableEnumerator(IEnumerable<T> source)
        {
            _origin = source.GetEnumerator();
        }

        public bool IsDisposed { get; private set; }

        public IEnumerator<T> GetEnumerator()
        {
            return this;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Dispose()
        {
            IsDisposed = true;
        }

        public bool MoveNext()
        {
            if (!_origin.MoveNext())
                return false;

            Current = _origin.Current;
            return true;
        }

        public void Reset()
        {
            throw new InvalidOperationException();
        }

        public T Current { get; private set; }

        object IEnumerator.Current
        {
            get { return Current; }
        }
    }
}