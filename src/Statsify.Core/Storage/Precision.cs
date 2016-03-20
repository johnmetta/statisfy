using System;

namespace Statsify.Core.Storage
{
    public struct Precision : IComparable<Precision>
    {
        private readonly TimeSpan value;

        public Precision(TimeSpan value) : 
            this()
        {
            this.value = value;
        }

        public static implicit operator TimeSpan(Precision precision)
        {
            return precision.value;
        }

        public static implicit operator int(Precision precision)
        {
            return (int)precision.value.TotalSeconds;
        }

        public int CompareTo(Precision other)
        {
            return value.CompareTo(other.value);
        }

        public override string ToString()
        {
            return value.ToString();
        }
    }
}