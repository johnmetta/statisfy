using System;
using Statsify.Agent.Configuration;

namespace Statsify.Agent.Impl
{
    public class MetricDefinition : IMetricDefinition
    {
        private readonly Func<double> nextValueProvider;
 
        public string Name { get; private set; }

        public AggregationStrategy AggregationStrategy { get; private set; }

        public MetricDefinition(string name, Func<double> nextValueProvider, AggregationStrategy aggregationStrategy)
        {
            Name = name;
            AggregationStrategy = aggregationStrategy;

            this.nextValueProvider = nextValueProvider;
        }

        public virtual double GetNextValue()
        {
            return nextValueProvider();
        }
    }
}
