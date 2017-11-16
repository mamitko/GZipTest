using System;
using System.Collections.Generic;

namespace GZipTest.Parallelizing
{
    public static class ParallelSelectExtentions
    {
        public static ParallelSelect<TSource, TResult> SelectParallely<TSource, TResult>(this IEnumerable<TSource> enumerable, Func<TSource, TResult> func, Cancellation cancellation = null)
        {
            return new ParallelSelect<TSource, TResult>(enumerable, func, new ParallelSettings {Cancellation = cancellation});
        }

        public static ParallelSelect<T, T> Buffered<T>(this IEnumerable<T> enumerable, Cancellation cancellation = null, int? bufferCapacity = null)
        {
            return SelectParallely(enumerable, i => i, cancellation)
                .WithBoundedOutputCapacity(bufferCapacity)
                .WithForcedDegreeOfParallelism(1);
        }
    }
}