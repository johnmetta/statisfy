using System.Collections.Generic;
using System.Linq;
using Statsify.Agent.Configuration;

namespace Statsify.Agent.Impl
{
    public class MetricCollector
    {
        private readonly IList<MetricDefinition> metricDefinitions = new List<MetricDefinition>();

        public MetricCollector(IEnumerable<MetricConfigurationElement> metrics)
        {
            var metricDefinitionFactory = new MetricDefinitionFactory();

            foreach(var metricDefinition in metrics.Select(metricDefinitionFactory.CreateInstance).Where(metricDefinition => metricDefinition != null))
            {
                metricDefinitions.Add(metricDefinition);
            }
        }

        public IEnumerable<Metric> GetCollectedMetrics()
        {
            return metricDefinitions.Select(metricDefinition => new Metric(metricDefinition.Name, metricDefinition.AggregationStrategy, metricDefinition.GetNextValue()));
        }
    }
}
