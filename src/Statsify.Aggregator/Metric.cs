using System;

namespace Statsify.Aggregator
{
    public class Metric : IEquatable<Metric>
    {
        public string Name { get; set; }

        public float Value { get; set; }

        public MetricType Type { get; set; }

        public float Sample { get; set; }

        public bool ExplicitlySigned { get; set; }

        public Metric()
        {
        }

        public Metric(string name, float value, MetricType type, float sample, bool explicitlySigned)
        {
            Name = name;
            Value = value;
            Type = type;
            Sample = sample;
            ExplicitlySigned = explicitlySigned;
        }

        public override string ToString()
        {
            return string.Format("Name: {0}, Value: {1}, Type: {2}, Sample: {3}, ExplicitlySigned: {4}", Name, Value, Type, Sample, ExplicitlySigned);
        }

        public bool Equals(Metric other)
        {
            if(ReferenceEquals(null, other)) return false;
            if(ReferenceEquals(this, other)) return true;
            return string.Equals(Name, other.Name) && Value.Equals(other.Value) && string.Equals(Type, other.Type) && Sample.Equals(other.Sample) && ExplicitlySigned.Equals(other.ExplicitlySigned);
        }

        public override bool Equals(object obj)
        {
            if(ReferenceEquals(null, obj)) return false;
            if(ReferenceEquals(this, obj)) return true;
            if(obj.GetType() != this.GetType()) return false;
            return Equals((Metric)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ Value.GetHashCode();
                hashCode = (hashCode * 397) ^ (Type != null ? Type.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ Sample.GetHashCode();
                hashCode = (hashCode * 397) ^ ExplicitlySigned.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(Metric left, Metric right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Metric left, Metric right)
        {
            return !Equals(left, right);
        }
    }
}