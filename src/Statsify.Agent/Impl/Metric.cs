using Statsify.Agent.Configuration;

namespace Statsify.Agent.Impl
{
    public class Metric
    {
        public string Name { get; private set; }

        public AggregationStrategy AggregationStrategy { get; private set; }

        public double Value { get; private set; }

        public Metric(string name, AggregationStrategy aggregationStrategy, double value)
        {
            Name = name;
            AggregationStrategy = aggregationStrategy;
            Value = value;
        }
    }
}