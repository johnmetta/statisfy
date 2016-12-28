using System;
using System.Collections.Generic;

namespace Statsify.Core.Util
{
    internal static class QueueExtensions
    {
        public static IEnumerable<T> DequeueWhile<T>(this Queue<T> queue, Predicate<T> predicate)
        {
            while(queue.Count > 0)
            {
                if(!predicate(queue.Peek())) yield break;

                yield return queue.Dequeue();
            } // while
        }
    }
}
