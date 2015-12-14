using System.Collections.Generic;

namespace Statsify.Agent.Impl
{
    public interface IMetricSource
    {
        IEnumerable<IMetricDefinition> MetricDefinitions { get; }
    }
}