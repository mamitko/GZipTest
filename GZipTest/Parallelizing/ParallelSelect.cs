using System;
using System.Collections;
using System.Collections.Generic;

namespace GZipTest.Parallelizing
{
    public class ParallelSelect<TSource, TResult>: IEnumerable<TResult>
    {
        private readonly IEnumerable<TSource> _source;
        private readonly Func<TSource, TResult> _selectorFunc;
        private readonly ParallelSettings _settings;
        private readonly int? _outBufferCapacity;

        public ParallelSelect<TSource, TResult> WithBoundedOutputCapacity(int? boundedCapacity)
        {
            return new ParallelSelect<TSource, TResult>(_source, _selectorFunc, _settings, boundedCapacity);
        }

        public ParallelSelect<TSource, TResult> WithForcedDegreeOfParallelizm(int degreeOfParallelizm)
        {
            var newSettings = _settings;
            newSettings.ForcedDegreeOfParallelizm = degreeOfParallelizm;

            return new ParallelSelect<TSource, TResult>(_source, _selectorFunc, newSettings, _outBufferCapacity);
        }

        public ParallelSelect(IEnumerable<TSource> source, Func<TSource, TResult> selector, ParallelSettings settings, int? outBufferCapacity = null)
        {
            _source = source;
            _selectorFunc = selector;
            _settings = settings;
            _outBufferCapacity = outBufferCapacity;
        }

        public IEnumerator<TResult> GetEnumerator()
        { 
            // В "промышленном" коде это, наверное, был бы отдельный _рукописный_ класс (а не конечный автомат + замыкания).

            var buffer = new BlockingQueue<TResult>(_outBufferCapacity ?? -1);
            
            var forAll = new ForAll<TSource>(_source, i => buffer.AddIfNotCompleted(_selectorFunc(i)), _settings);
            forAll.RegisterOnfinished(_ => buffer.CompleteAdding());

            forAll.Start();

            foreach (var item in buffer.GetConsumingEnumerable())
            {
                if (_settings.Cancellation.IsCanceled)
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