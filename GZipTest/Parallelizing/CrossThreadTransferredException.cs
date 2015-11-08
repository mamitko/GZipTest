using System;

namespace GZipTest.Parallelizing
{
    public class CrossThreadTransferredException: Exception
    {
        private const string StdMessage = "Exception happened in another thread.";

        public static void Rethrow(Exception exception)
        {
            if (exception == null) 
                return;

            var cancalledException = exception as OperationCanceledException; 
            if (cancalledException != null)
                throw new OperationCanceledException();
            // автор знает, что в оригинале это исключение придет внутри "агрегации"
            
            throw new CrossThreadTransferredException(exception);
        }

        public override string Message
        {
            get { return DigOutTrueException(InnerException).Message; }
        }

        private CrossThreadTransferredException(Exception innerException)
            : base (StdMessage, DigOutTrueException(innerException))
        {
        }
        
        private static Exception DigOutTrueException(Exception exception)
        {
            var e = exception;
            while (e is CrossThreadTransferredException && e.InnerException != null)
            {
                e = e.InnerException;
            }
            return e;
        }
    }
}