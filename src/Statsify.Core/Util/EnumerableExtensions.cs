using System;
using System.Collections.Generic;
using System.Linq;

namespace Statsify.Core.Util
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<Range<T>> ToRanges<T>(this IEnumerable<T> enumerable)
        {
            using(var enumerator = enumerable.GetEnumerator())
            {
                if(!enumerator.MoveNext()) yield break;
                var from = enumerator.Current;

                while(enumerator.MoveNext())
                {
                    var until = enumerator.Current;
                    yield return new Range<T>(@from, until);

                    @from = until;
                } // while
            } // using
        }

        public static TResult Median<TSource, TResult>(this IEnumerable<TSource> enumerable, Func<TSource, TResult> keySelector, Func<TResult, TResult, TResult> add,
            Func<TResult, int, TResult> divide)
        {
            var values = enumerable.Select(keySelector).ToArray();
                        
            var mid = (int)Math.Floor(values.Length / 2.0);
            var median = (values.Length % 2 != 0) ? values[mid] : divide(add(values[mid - 1], values[mid]), 2);

            return median;
        }
    }
}