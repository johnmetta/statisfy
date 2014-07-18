using System.Collections.Generic;
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
                if(metricDefinition != null)
                    metricDefinitions.Add(metricDefinition);
            } // foreach
        }

        public IEnumerable<Metric> GetCollectedMetrics()
        {
            foreach(var metricDefinition in metricDefinitions)
            {
                var metric = new Metric(metricDefinition.Name, metricDefinition.AggregationStrategy, metricDefinition.GetNextValue());
                yield return metric;
            } // foreach
        } 
    }
}
