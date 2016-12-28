using System;
using System.Collections.Generic;
using Statsify.Core.Model;

namespace Statsify.Core.Components
{
    public interface IMetricRegistry
    {
        ISet<string> ResolveMetricNames(string metricNameSelector);

        Metric ReadMetric(string metricName, DateTime from, DateTime until, TimeSpan? precision = null);

        void PurgeMetrics(DateTime lastUpdatedAt);
    }
}