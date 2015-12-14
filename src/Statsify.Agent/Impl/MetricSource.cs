using System.Collections.Generic;
using System.Linq;

namespace Statsify.Agent.Impl
{
    public class MetricSource : IMetricSource
    {
        private readonly IList<IMetricDefinition> metricDefinitions;

        public MetricSource(IEnumerable<IMetricDefinition> metricDefinitions)
        {
            this.metricDefinitions = new List<IMetricDefinition>(metricDefinitions ?? Enumerable.Empty<IMetricDefinition>());
        }

        public IEnumerable<IMetricDefinition> GetMetricDefinitions()
        {
            return metricDefinitions;
        }

        public void InvalidateMetricDefinition(IMetricDefinition metricDefinition)
        {
            metricDefinitions.Remove(metricDefinition);
        }
    }
}