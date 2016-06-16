using System;
using System.Globalization;

namespace Statsify.Aggregator
{
    public class Metric : IEquatable<Metric>
    {
        public string Name { get; set; }

        public string Value { get; set; }

        public MetricType Type { get; set; }

        public float Sample { get; set; }

        public Metric()
        {
        }

        public Metric(string name, string value, MetricType type, float sample)
        {
            Name = name;
            Value = value;
            Type = type;
            Sample = sample;
        }

        public override string ToString()
        {
            return string.Format("Name: {0}, Value: {1}, Type: {2}, Sample: {3}", Name, Value, Type, Sample);
        }

        public bool Equals(Metric other)
        {
            if(ReferenceEquals(null, other)) return false;
            if(ReferenceEquals(this, other)) return true;

            return string.Equals(Name, other.Name) && Value.Equals(other.Value) && string.Equals(Type, other.Type) && 
                Sample.Equals(other.Sample);
        }

        public override bool Equals(object obj)
        {
            if(ReferenceEquals(null, obj)) return false;
            if(ReferenceEquals(this, obj)) return true;

            return obj.GetType() == GetType() && Equals((Metric)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ Value.GetHashCode();
                hashCode = (hashCode * 397) ^ (Type.GetHashCode());
                hashCode = (hashCode * 397) ^ Sample.GetHashCode();

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

        public static Metric Timer(string name, float value, float sample = 1)
        {
            return new Metric(name, value.ToString(CultureInfo.InvariantCulture), MetricType.Timer, sample);
        }

        public static Metric Gauge(string name, string value, float sample = 1)
        {
            return new Metric(name, value, MetricType.Gauge, sample);
        }

        public static Metric Counter(string name, float value, float sample = 1)
        {
            return new Metric(name, value.ToString(CultureInfo.InvariantCulture), MetricType.Counter, sample);
        }

        public static Metric Set(string name, string value, float sample = 1)
        {
            return new Metric(name, value, MetricType.Set, sample);
        }
    }
}