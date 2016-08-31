using System;
using Statsify.Agent.Configuration;

namespace Statsify.Agent.Impl
{
    public class MetricConfiguration : IMetricConfiguration
    {
        public string Name { get; private set; }

        public string Type { get; private set; }

        public string Path { get; private set; }

        public AggregationStrategy AggregationStrategy { get; private set; }

        public TimeSpan? RefreshEvery { get; private set; }

        public MetricConfiguration(string name, string type, string path, AggregationStrategy aggregationStrategy, TimeSpan? refreshEvery)
        {
            Name = name;
            Type = type;
            Path = path;
            AggregationStrategy = aggregationStrategy;
            RefreshEvery = refreshEvery;
        }
    }
}
