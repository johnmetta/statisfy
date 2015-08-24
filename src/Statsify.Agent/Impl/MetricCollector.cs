using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
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
            } // foreach
        }

        public IEnumerable<Metric> GetCollectedMetrics()
        {
            var invalidatedMetrics = new HashSet<IMetricDefinition>();
            
            foreach(var metricDefinition in metricDefinitions)
            {
                Metric metric = null;

                try
                {
                    var value = metricDefinition.GetNextValue();
                    metric = new Metric(metricDefinition.Name, metricDefinition.AggregationStrategy, value);
                } // try
                catch(MetricInvalidatedException)
                {
                    //
                    // When IMetricDefinition throws an MIE, we don't want to hear
                    // from it any more.
                    log.Warn("invalidating metric '{0}'", metricDefinition.Name);
                    invalidatedMetrics.Add(metricDefinition);
                } // catch
                catch(Exception e)
                {
                    log.ErrorException(string.Format("invalidating metric '{0}'", metricDefinition.Name), e);
                    invalidatedMetrics.Add(metricDefinition);
                } // catch
                 
                if(metric != null)
                    yield return metric;
            } // foreach

            foreach(var invalidatedMetric in invalidatedMetrics)
                metricDefinitions.Remove(invalidatedMetric);
        }
    }
}
