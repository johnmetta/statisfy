using System;
using System.Collections.Generic;
using System.Linq;
using Statsify.Core.Model;
using Statsify.Core.Util;

namespace Statsify.Core.Expressions
{
    public static class MetricReader
    {
        public static List<Metric> ReadMetrics(Environment environment, string[] metricNames, DateTime from, DateTime until)
        {
            //
            // Do a single pass through queued metric datapoints
            var queuedDatapoints = 
                environment.QueuedMetricDatapoints.
                    Where(md => metricNames.Contains(md.Name, StringComparer.InvariantCultureIgnoreCase) && from <= md.Datapoint.Timestamp && md.Datapoint.Timestamp <= until).
                    GroupBy(md => md.Name.ToLowerInvariant()).
                    ToDictionary(g => g.Key, g => g.Select(md => md.Datapoint).ToList());

            var metrics = new List<Metric>(metricNames.Length);
            foreach(var metricName in metricNames)
            {
                var metric = environment.MetricRegistry.ReadMetric(metricName, from, until);

                var key = metricName.ToLowerInvariant();
                if(queuedDatapoints.ContainsKey(key))
                {
                    var queued = queuedDatapoints[key];
                    metric = new Metric(metric.Name, new Series(metric.Series, metric.Series.Datapoints.Append(queued).OrderBy(d => d.Timestamp)));
                }

                metrics.Add(metric);
            } // foreach

            return metrics;
        }
    }
}