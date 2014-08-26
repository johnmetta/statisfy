using System;
using System.Diagnostics;

namespace Statsify.Core.Model
{
    [DebuggerDisplay("{Timestamp}: {Value}")]
    public struct Datapoint : IEquatable<Datapoint>
    {
        public DateTime Timestamp { get; private set; }

        public double? Value { get; private set; }

        public Datapoint(DateTime timestamp, double? value) : 
            this()
        {
            Timestamp = timestamp;
            Value = value;
        }

        public bool Equals(Datapoint other)
        {
            return Timestamp.Equals(other.Timestamp) && Value.Equals(other.Value);
        }

        public override bool Equals(object obj)
        {
            if(ReferenceEquals(null, obj)) return false;
            return obj is Datapoint && Equals((Datapoint)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Timestamp.GetHashCode() * 397) ^ Value.GetHashCode();
            }
        }

        public static bool operator ==(Datapoint left, Datapoint right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Datapoint left, Datapoint right)
        {
            return !left.Equals(right);
        }
    }
}
