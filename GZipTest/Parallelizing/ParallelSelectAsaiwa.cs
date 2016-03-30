using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace GZipTest.Parallelizing
{
    public static class ParallelSelectAsaiwa // As Short As I Was Able
    {
        private static BlockingQueue<TDestination> StartSelecting<TSource, TDestination>(
            Func<IEnumerable<TSource>> getSource, Func<TSource, TDestination> selector, Action<Exception> onFirstExceptionCallback, Cancellation canceller, 
            int threadsDesired, int bufferCapcity = -1)
        {
            Exception firstException = null;

            var workersCountDesired = Math.Max(1, threadsDesired > 0 ? threadsDesired : Environment.ProcessorCount);
            var enumerables = Enumerable.Range(0, workersCountDesired).Select(_ => getSource()).Where(srs => srs != null).ToArray();

            var outputBuffer = new BlockingQueue<TDestination>(bufferCapcity);

            var threadesFinished = 0;
            foreach (var source in enumerables)
            {
                var capturedSource = source; // old compliers use the same "instance" of loop varable for all iterations (Language Specification C#4.0 section 8.8.4)
                var worker = new Thread(() =>
                {
                    try
                    {
                        foreach (var item in capturedSource)
                        {
                            canceller.ThrowIfCancelled();

                            Thread.MemoryBarrier();
                            if (firstException != null)
                                break;

                            var processed = selector(item);
                            outputBuffer.AddIfNotCompleted(processed);
                        }
                    }
                    catch (Exception e)
                    {
                        if (Interlocked.CompareExchange(ref firstException, e, null) == null)
                            onFirstExceptionCallback(e);
                    }
                    finally
                    {
                        if (Interlocked.Increment(ref threadesFinished) == enumerables.Length)
                            outputBuffer.CompleteAdding();

                    }
                })
                { IsBackground = true };
                worker.Start();
            }

            return outputBuffer;
        }

        public static IEnumerable<TDestination> SelectParallellyAsaiwa<TSource, TDestination>(
            this IEnumerable<TSource> source, Func<TSource, TDestination> selector, Cancellation canceller = null, 
            int prefetchBufferCapacity = -1, int outputBufferCapcity = -1)
        {
            canceller = canceller ?? Cancellation.Uncancallable;
            Exception firstException = null;
            Action<Exception> onException = e => Interlocked.CompareExchange(ref firstException, e, null);

            var inputBuffer = StartSelecting(() => Interlocked.Exchange(ref source, null), _ => _, onException, canceller, threadsDesired: 1, bufferCapcity: prefetchBufferCapacity);

            var outputBuffer = StartSelecting(inputBuffer.GetConsumingEnumerable, selector, onException, canceller, threadsDesired: Parallelism.DefaultDegree, bufferCapcity: outputBufferCapcity);
            
            foreach (var processed in outputBuffer.GetConsumingEnumerable())
            {
                if (firstException != null)
                    break;

                yield return processed;
            }

            if (firstException != null)
                CrossThreadTransferredException.Rethrow(firstException);
        }
    }

}