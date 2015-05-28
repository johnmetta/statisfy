using System.Collections.Generic;
using Statsify.Core.Model;

namespace Statsify.Aggregator.ComponentModel
{
    public interface IMetricAggregator
    {
        int QueueBacklog { get; }

        IEnumerable<MetricDatapoint> Queue { get; }
    }
}
