using System;

namespace GZipTest.Parallelizing
{
    public class WorkCompleteEventArgs : EventArgs
    {
        public WorkCompleteEventArgs(Exception error, bool cancelled)
        {
            Cancelled = cancelled;
            Error = error;
        }

        public bool Cancelled { get; private set; }

        public Exception Error { get; private set;}
    }
}