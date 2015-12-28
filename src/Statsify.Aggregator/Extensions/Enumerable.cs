using System.Collections.Generic;

namespace Statsify.Aggregator.Extensions
{
    public static class Enumerable
    {
        public static IEnumerable<float> Accumulate(this IEnumerable<float> src)
        {
            using (var e = src.GetEnumerator())
            {
                e.MoveNext();

                var v = e.Current;
                yield return v;

                while (e.MoveNext())
                {
                    v += e.Current;
                    yield return v;
                }

            }
        }
    }
}
