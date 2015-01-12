using System;
using System.Collections.Generic;

namespace Statsify.Core.Util
{
    public static class EnumerableUtil
    {
        public static IEnumerable<T> Generate<T>(T seed, Func<T, T> nextValue, Predicate<T> @while)
        {
            var value = seed;
            while(@while(value))
            {
                yield return value;
                value = nextValue(value);
            } // while
        }
    }
}
