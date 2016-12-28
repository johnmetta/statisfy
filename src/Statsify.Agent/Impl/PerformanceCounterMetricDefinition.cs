using System;
using Statsify.Agent.Configuration;

namespace Statsify.Agent.Impl
{
    public class PerformanceCounterMetricDefinition : MetricDefinition
    {
        public PerformanceCounterMetricDefinition(string name, Func<double> nextValueProvider, AggregationStrategy aggregationStrategy) : 
            base(name, nextValueProvider, aggregationStrategy)
        {
        }

        public override double GetNextValue()
        {
            //
            // Sometimes, Performance Counters disappear midflight (drive gets ejected, disabled or 
            // something equally disastrous happens). When this happens, GetNextValue() will fail
            // with IOE, which we catch and convert to MIE.
            try
            {
                return base.GetNextValue();
            } // try
            catch(InvalidOperationException e)
            {
                throw new MetricInvalidatedException("Performance Counter Metric has been invalidated", Name);
            } // catch
        }
    }
}