using System;

namespace Statsify.Agent.Impl
{
    public class MetricInvalidatedException : Exception
    {
        public string MetricName { get; private set; }

        public MetricInvalidatedException()
        {
        }

        public MetricInvalidatedException(string message, string metricName) : 
            base(message)
        {
            MetricName = metricName;
        }
    }
}