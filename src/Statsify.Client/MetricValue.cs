namespace Statsify.Client
{
    public abstract class MetricValue
    {
        internal class StringMetricValue : MetricValue
        {
            public string Value { get; private set; }

            public StringMetricValue(string value)
            {
                Value = value;
            }
        }

        internal class DoubleMetricValue : MetricValue
        {
            public double Value { get; private set; }

            public bool ExplicitlySigned { get; private set; }

            public DoubleMetricValue(double value, bool explicitlySigned = false)
            {
                Value = value;
                ExplicitlySigned = explicitlySigned;
            }
        }
    }
}