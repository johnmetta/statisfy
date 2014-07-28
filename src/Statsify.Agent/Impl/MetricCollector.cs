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

            foreach(var metric in metrics)
            {
                var metricDefinition = metricDefinitionFactory.CreateInstance(metric);
                if (metricDefinition != null)
                    metricDefinitions.Add(metricDefinition);
            }
        }

        public IEnumerable<Metric> GetCollectedMetrics()
        {
            return metricDefinitions.Select(metricDefinition => new Metric(metricDefinition.Name, metricDefinition.AggregationStrategy, metricDefinition.GetNextValue()));
        }
    }
}
