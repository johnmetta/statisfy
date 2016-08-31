using System;

namespace Statsify.Agent.Configuration
{
    public interface IMetricConfiguration
    {
        string Name { get; }

        string Type { get; }

        string Path { get; }

        AggregationStrategy AggregationStrategy { get; }

        TimeSpan? RefreshEvery { get; }
    }
}