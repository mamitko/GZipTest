using System;
using System.Collections;
using System.Collections.Generic;

namespace GZipTest.Parallelizing
{
    public class ParallelSelect<TSource, TResult>: IEnumerable<TResult>
    {
        private readonly IEnumerable<TSource> source;
        private readonly Func<TSource, TResult> selectorFunc;
        private readonly ParallelSettings settings;
        private readonly int? outBufferCapacity;
        
        public ParallelSelect<TSource, TResult> WithBoundedOutputCapacity(int? boundedCapacity)
        {
            return new ParallelSelect<TSource, TResult>(source, selectorFunc, settings, boundedCapacity);
        }

        public ParallelSelect<TSource, TResult> WithForcedDegreeOfParallelism(int degreeOfParallelism)
        {
            var newSettings = settings.Modified(forcedDegreeOfParallelism: degreeOfParallelism);
            return new ParallelSelect<TSource, TResult>(source, selectorFunc, newSettings, outBufferCapacity);
        }

        public ParallelSelect(IEnumerable<TSource> source, Func<TSource, TResult> selector, ParallelSettings settings, int? outBufferCapacity = null)
        {
            this.source = source;
            selectorFunc = selector;
            this.settings = settings;
            this.outBufferCapacity = outBufferCapacity;
        }

        public IEnumerator<TResult> GetEnumerator()
        { 
            var buffer = new BlockingQueue<TResult>(outBufferCapacity ?? -1);
            
            var forAll = new ForAll<TSource>(source, i => buffer.AddIfNotCompleted(selectorFunc(i)), settings);
            forAll.RegisterOnFinished(_ => buffer.CompleteAdding());

            forAll.Start();

            foreach (var item in buffer.GetConsumingEnumerable())
            {
                if (settings.Cancellation.IsCanceled)
                    break;

                yield return item;
            }
            
            forAll.Wait();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}