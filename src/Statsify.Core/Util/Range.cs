using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Statsify.Core.Util
{
    [DebuggerDisplay("From: '{From,nq}', Until: '{Until,nq}'")]
    public struct Range<T> : IEquatable<Range<T>>
    {
        public T From { get; private set; }

        public T Until { get; private set; }

        [DebuggerStepThrough]
        public Range(T @from, T until) : 
            this()
        {
            From = @from;
            Until = until;
        }

        public bool Equals(Range<T> other)
        {
            return EqualityComparer<T>.Default.Equals(From, other.From) && EqualityComparer<T>.Default.Equals(Until, other.Until);
        }

        public override bool Equals(object obj)
        {
            if(ReferenceEquals(null, obj)) return false;
            return obj is Range<T> && Equals((Range<T>) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (EqualityComparer<T>.Default.GetHashCode(From)*397) ^ EqualityComparer<T>.Default.GetHashCode(Until);
            }
        }

        public static bool operator ==(Range<T> left, Range<T> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Range<T> left, Range<T> right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return string.Format("From: {0}, Until: {1}", From, Until);
        }
    }
}