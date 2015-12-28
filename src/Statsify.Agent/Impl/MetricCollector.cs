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
        private readonly IList<IMetricSource> metricSources = new List<IMetricSource>();

        public MetricCollector(IEnumerable<MetricConfigurationElement> metrics)
        {
            var metricDefinitionFactory = new MetricDefinitionFactory();
            var metricSourceFactory = new MetricSourceFactory(metricDefinitionFactory);

            foreach(var metric in metrics)
            {
                var metricSource = metricSourceFactory.CreateMetricSource(metric);
                if(metricSource == null)
                {
                    log.Warn("could not create metric source for '{0}:{1}'", metric.Type, metric.Path);
                    continue;
                } // if

                log.Info("created metric source for '{0}:{1}' with aggregation strategy '{2}'", metric.Type, metric.Name, metric.AggregationStrategy);
                metricSources.Add(metricSource);

                foreach(var metricDefinition in metricSource.GetMetricDefinitions())
                    log.Info("created metric definition '{0}' with aggregation strategy '{1}'", metricDefinition.Name, metricDefinition.AggregationStrategy);
            } // foreach
        }

        public IEnumerable<Metric> GetCollectedMetrics()
        {
            foreach(var metricSource in metricSources)
            {
                foreach(var metricDefinition in metricSource.GetMetricDefinitions())
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
                        metricSource.InvalidateMetricDefinition(metricDefinition);
                    } // catch
                    catch(Exception e)
                    {
                        log.ErrorException(string.Format("invalidating metric '{0}'", metricDefinition.Name), e);
                        metricSource.InvalidateMetricDefinition(metricDefinition);
                    } // catch

                    if(metric != null)
                        yield return metric;
                } // foreach
            } // foreach
        }
    }
}
