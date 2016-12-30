using System;
using Statsify.Core.Components;

namespace Statsify.Core.Expressions
{
    public class EvalContext
    {
        public DateTime From { get; private set; }

        public DateTime Until { get; private set; }

        public IMetricRegistry MetricRegistry { get; private set; }

        public EvalContext(DateTime @from, DateTime until, IMetricRegistry metricRegistry)
        {
            From = @from;
            Until = until;
            MetricRegistry = metricRegistry;
        }
    }
}