using System;
using Statsify.Core.Model;

namespace Statsify.Core.Components
{
    public interface IMetricReader
    {
        Metric ReadMetric(string metricName, DateTime from, DateTime until, TimeSpan? precision = null);
    }
}