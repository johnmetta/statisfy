using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;

namespace Statsify.Aggregator
{
    internal class MetricsBuffer
    {
        private readonly ConcurrentDictionary<string, IList<float>> timers = new ConcurrentDictionary<string, IList<float>>();
        private readonly ConcurrentDictionary<string, float> timerCounters = new ConcurrentDictionary<string, float>();
        private readonly ConcurrentDictionary<string, float> gauges = new ConcurrentDictionary<string, float>();
        private readonly ConcurrentDictionary<string, float> counters = new ConcurrentDictionary<string, float>();
        private readonly ConcurrentDictionary<string, ConcurrentBag<string>> sets = new ConcurrentDictionary<string, ConcurrentBag<string>>(); 

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

        public float? GetTimerCounter(string name)
        {
            float value;
            return timerCounters.TryGetValue(name, out value) ? 
                (float?)value : 
                null;
        }

        public void Aggregate(Metric metric)
        {
            var key = metric.Name;
            var value = TryParseFloat(metric.Value);

            switch(metric.Type)
            {
                case MetricType.Timer:
                    if(!value.HasValue) return;
                    
                    timers.AddOrUpdate(key, new List<float> { value.Value },
                        (k, v) =>
                        {
                            v.Add(value.Value);
                            return v;
                        });

                    timerCounters.AddOrUpdate(key, 1 / metric.Sample, (k, v) => v + (1 / metric.Sample));
                    break;
                
                case MetricType.Gauge:
                    if(!value.HasValue) return;

                    var signed = metric.Signed;
                    gauges.AddOrUpdate(key, value.Value, (k, v) => (signed ? 0 : v) + value.Value);
                    break;

                case MetricType.Set:
                    break;

                case MetricType.Counter:
                    if(!value.HasValue) return;

                    counters.AddOrUpdate(key, value.Value * (1 / metric.Sample), (k, v) => v + value.Value * (1 / metric.Sample));
                    break;
            } // switch
        }

        private static float? TryParseFloat(string s)
        {
            float value = 0;
            if(float.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out value))
                return value;

            return null;
        }
    }
}