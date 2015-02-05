using System;
using System.Collections.Generic;
using Statsify.Core.Model;

namespace Statsify.Core.Components
{
    public interface IMetricRegistry
    {
        IEnumerable<string> ResolveMetricNames(string metricNameSelector);

        Metric ReadMetric(string metricName, DateTime from, DateTime until, TimeSpan? precision = null);
    }
}