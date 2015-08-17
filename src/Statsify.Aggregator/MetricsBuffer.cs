using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Statsify.Aggregator
{
    internal class MetricsBuffer
    {
        private readonly ConcurrentDictionary<string, IList<float>> timers = new ConcurrentDictionary<string, IList<float>>();
        private readonly ConcurrentDictionary<string, float> timerCounters = new ConcurrentDictionary<string, float>();
        private readonly ConcurrentDictionary<string, float> gauges = new ConcurrentDictionary<string, float>();
        private readonly ConcurrentDictionary<string, float> counters = new ConcurrentDictionary<string, float>();

        public IEnumerable<KeyValuePair<string, IList<float>>>  Timers
        {
            get { return timers; }
        }
 
        public IEnumerable<KeyValuePair<string, float>>  Counters
        {
            get { return counters; }
        }

        public IEnumerable<KeyValuePair<string, float>>  Gauges
        {
            get { return gauges; }
        }

        public void Aggregate(Metric metric)
        {
            var key = metric.Name;

            switch(metric.Type)
            {
                case MetricType.Timer:
                    timers.AddOrUpdate(key, new List<float> { metric.Value },
                        (k, v) =>
                        {
                            v.Add(metric.Value);
                            return v;
                        });

                    timerCounters.AddOrUpdate(key, 1 / metric.Sample, (k, v) => v + (1 / metric.Sample));
                    break;
                
                case MetricType.Gauge:
                    var signed = metric.Signed;
                    gauges.AddOrUpdate(key, metric.Value, (k, v) => (signed ? 0 : v) + metric.Value);
                    break;

                case MetricType.Set:
                    break;

                case MetricType.Counter:
                    counters.AddOrUpdate(key, metric.Value * (1 / metric.Sample), (k, v) => v + metric.Value * (1 / metric.Sample));
                    break;
            } // switch
        }
    }
}