using System.Collections.Generic;
using System.Linq;
using Statsify.Agent.Configuration;

namespace Statsify.Agent.Impl
{
    public class MetricCollector
    {
        private readonly Logger log = LogManager.GetCurrentClassLogger();
        private readonly IList<IMetricDefinition> metricDefinitions = new List<IMetricDefinition>();

        public MetricCollector(IEnumerable<MetricConfigurationElement> metrics)
        {
            var metricDefinitionFactory = new MetricDefinitionFactory();

            foreach(var metric in metrics.SelectMany(metricDefinitionFactory.CreateMetricDefinitions).Where(m => m != null))
            {
                log.Info("adding metric '{0}' with aggregation strategy '{1}'", metric.Name, metric.AggregationStrategy);
                metricDefinitions.Add(metric);
            }
        }

        public IEnumerable<Metric> GetCollectedMetrics()
        {
            return metricDefinitions.Select(metricDefinition => new Metric(metricDefinition.Name, metricDefinition.AggregationStrategy, metricDefinition.GetNextValue()));
        }
    }
}
