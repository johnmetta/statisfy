namespace Statsify.Client
{
    public class Metric
    {
        public string Name { get; private set; }

        public MetricType Type { get; private set; }

        public MetricValue Value { get; private set; }

        public double Sample { get; private set; }

        public Metric(string name, MetricType type, MetricValue value, double sample)
        {
            Name = name;
            Value = value;
            Sample = sample;
            Type = type;
        }

        public static Metric Counter(string name, double value, double sample = 1)
        {
            return new Metric(name, MetricType.Counter, new MetricValue.DoubleMetricValue(value), sample);
        }

        public static Metric Gauge(string name, double value, double sample = 1)
        {
            return new Metric(name, MetricType.Gauge, new MetricValue.DoubleMetricValue(value), sample);
        }

        public static Metric GaugeDiff(string name, double value, double sample = 1)
        {
            return new Metric(name, MetricType.Gauge, new MetricValue.DoubleMetricValue(value, true), sample);
        }

        public static Metric Time(string name, double value, double sample = 1)
        {
            return new Metric(name, MetricType.Time, new MetricValue.DoubleMetricValue(value), sample);
        }

        public static Metric Set(string name, string value, double sample = 1)
        {
            return new Metric(name, MetricType.Time, new MetricValue.StringMetricValue(value), sample);
        }
    }
}