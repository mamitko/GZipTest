using System;

namespace GZipTest.Parallelizing
{
    public class WorkCompletionInfo : EventArgs
    {
        public WorkCompletionInfo(Exception error, bool cancelled)
        {
            Cancelled = cancelled;
            Error = error;
        }
        
        public bool Cancelled { get; private set; }

        public Exception Error { get; private set;}
    }
}