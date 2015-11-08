using System;
using System.Threading;

namespace GZipTest.Parallelizing
{
    public class Cancellation
    {
        // автор знает, что в FCL это два типа (даже три), и наверное, догадывается зачем это, но тут это перебор

        public static readonly Cancellation Uncancallable = new Cancellation();
        
        private volatile bool _canceled;
        
        public void Cancel()
        {
            if (this == Uncancallable)
                return;

            _canceled = true;
            OnCanceled();
        }

        public bool IsCanceled
        {
            get { return _canceled; }
        }

        public void ThrowExceptionIfCancelled()
        {
            if (_canceled)
            {
                throw new OperationCanceledException();
            }
        }
        
        public event EventHandler Canceled;
        protected virtual void OnCanceled()
        {
            var handler = Canceled;
            if (handler != null) handler(this, EventArgs.Empty);
        }
    }
}