using Statsify.Agent.Configuration;

namespace Statsify.Agent.Impl
{
    public interface IMetricDefinition
    {
        string Name { get; }

        AggregationStrategy AggregationStrategy { get; }

        double GetNextValue();
    }
}