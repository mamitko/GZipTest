using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace GZipTest.Parallelizing
{
    public static class ParallelSelectEtude
    {
        // It's just for self-trainings. I do realize these all is to complicated and weird for production-quality code.

        private static BlockingQueue<TDestination> StartSelecting<TSource, TDestination>(
            Func<IEnumerable<TSource>> getSource, 
            Func<TSource, TDestination> selector, 
            Action<Exception> onFirstExceptionCallback, 
            Cancellation cancellation, 
            int threadsDesired, 
            int bufferCapacity = -1)
        {
            Exception firstException = null;
            var workersCountDesired = Math.Max(1, threadsDesired > 0 ? threadsDesired : Environment.ProcessorCount);
            var enumerables = Enumerable.Range(0, workersCountDesired).Select(_ => getSource()).Where(srs => srs != null).ToArray();
            var outputBuffer = new BlockingQueue<TDestination>(bufferCapacity);

            var threadsFinished = 0;
            foreach (var source in enumerables)
            {
                var capturedSource = source; // old compilers use the same "instance" of loop variable for all iterations (Language Specification C#4.0 section 8.8.4)
                var worker = new Thread(() =>
                {
                    try
                    {
                        foreach (var item in capturedSource)
                        {
                            cancellation.ThrowIfCancelled();

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
                        if (Interlocked.Increment(ref threadsFinished) == enumerables.Length)
                            outputBuffer.CompleteAdding();

                    }
                })
                { IsBackground = true };
                worker.Start();
            }

            return outputBuffer;
        }

        public static IEnumerable<TDestination> SelectParallelyEtude<TSource, TDestination>(
            this IEnumerable<TSource> source, 
            Func<TSource, TDestination> selector, 
            Cancellation cancellation = null, 
            int prefetchBufferCapacity = -1, 
            int outputBufferCapacity = -1)
        {
            cancellation = cancellation ?? Cancellation.Uncancallable;
            Exception firstException = null;
            Action<Exception> onException = e => Interlocked.CompareExchange(ref firstException, e, null);

            var inputBuffer = StartSelecting(
                getSource: () => Interlocked.Exchange(ref source, null), 
                selector: _ => _, 
                onFirstExceptionCallback: onException, 
                cancellation: cancellation, 
                threadsDesired: 1, 
                bufferCapacity: prefetchBufferCapacity);

            var outputBuffer = StartSelecting(
                getSource: inputBuffer.GetConsumingEnumerable, 
                selector: selector, 
                onFirstExceptionCallback: onException, 
                cancellation: cancellation, 
                threadsDesired: Parallelism.DefaultDegree, 
                bufferCapacity: outputBufferCapacity);
            
            foreach (var processed in outputBuffer.GetConsumingEnumerable())
            {
                Thread.MemoryBarrier();
                if (firstException != null)
                    break;

                yield return processed;
            }

            if (firstException != null)
                CrossThreadTransferredException.Rethrow(firstException);
        }
    }

}