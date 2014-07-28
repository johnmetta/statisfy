using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using NLog;
using Statsify.Aggregator.Configuration;
using Statsify.Aggregator.Extensions;
using Statsify.Core.Storage;

namespace Statsify.Aggregator
{
    public class MetricAggregator
    {
        private readonly Logger log = LogManager.GetCurrentClassLogger();

        private readonly StatsifyAggregatorConfigurationSection configuration;

        private readonly ManualResetEvent stopEvent;

        private readonly float flushInterval;

        private readonly IDictionary<string, IList<float>> timers = new Dictionary<string, IList<float>>();

        private readonly IDictionary<string, float> timerCounters = new Dictionary<string, float>();

        private readonly IDictionary<string, float> gauges = new Dictionary<string, float>();

        private readonly IDictionary<string, float> counters = new Dictionary<string, float>();

        private readonly ConcurrentQueue<Tuple<string, DateTime, float>> flushQueue = new ConcurrentQueue<Tuple<string, DateTime, float>>();

        private readonly object sync = new object();

        public MetricAggregator(StatsifyAggregatorConfigurationSection configuration, ManualResetEvent stopEvent)
        {
            this.configuration = configuration;

            this.stopEvent = stopEvent;

            var flushThread = new Thread(FlushCallback);

            flushThread.Start();

            flushInterval = (float)configuration.Storage.FlushInterval.TotalMilliseconds;
        }

        private void FlushCallback()
        {
            if(!Directory.Exists(configuration.Storage.Path))
            {
                log.Info("creating '{0}'", configuration.Storage.Path);

                Directory.CreateDirectory(configuration.Storage.Path);
            }

            while(!stopEvent.WaitOne(TimeSpan.FromMilliseconds(100)))
            {
                var n = 0;

                var count = flushQueue.Count;

                log.Trace("started flushing {0} entries", count);

                var sw = Stopwatch.StartNew();

                Tuple<string, DateTime, float> datapoint;

                while (flushQueue.TryDequeue(out datapoint))
                {
                    n++;

                    var db = GetDb(configuration.Storage.Path, datapoint.Item1);

                    if(db != null)
                        db.WriteDatapoint(datapoint.Item2, datapoint.Item3);
                }

                log.Trace("completed flushing {0} entries in {1} ({2:N2} per second); {3} items in backlog queue", n, sw.Elapsed, count / sw.Elapsed.TotalSeconds, flushQueue.Count);               

                Thread.Sleep(TimeSpan.FromSeconds(1));
            }
        }

        public void Aggregate(Metric metric)
        {
            lock(sync)
            {
                var key = metric.Name;

                switch(metric.Type)
                {
                    case MetricType.Timer:
                        if(!timers.ContainsKey(key))
                        {
                            timers[key] = new List<float>();

                            timerCounters[key] = 0;
                        }

                        timers[key].Add(metric.Value);

                        timerCounters[key] += (1 / metric.Sample);
                        break;

                    case MetricType.Gauge:
                        if(metric.ExplicitlySigned)
                        {
                            if(!gauges.ContainsKey(key))
                                gauges[key] = 0;

                            gauges[key] += metric.Value;
                        }
                        else
                        {
                            gauges[key] = metric.Value;
                        }
                        break;

                    case MetricType.Set:
                        break;

                    case MetricType.Counter:
                        if(!counters.ContainsKey(key))
                            counters[key] = 0;

                        counters[key] += metric.Value * (1 / metric.Sample);
                        break;
                }
            }
        }

        public void Flush()
        {
            lock(sync)
            {
                var ts = DateTime.UtcNow;

                IDictionary<string, IDictionary<string, float>> timerData = new Dictionary<string, IDictionary<string, float>>();

                foreach(var key in timers.Keys.Where(k => timers[k].Count > 0))
                {
                    timerData[key] = new Dictionary<string, float>();

                    var values = timers[key].OrderBy(v => v).ToList();

                    var count = values.Count;

                    var min = values[0];

                    var max = values[count - 1];

                    var cumulativeValues = values.Accumulate().ToList();

                    var sum = min;

                    var mean = min;

                    var thresholdBoundary = max;

                    var pctThreshold = new[] { 85.0, 90, 95, 99 };

                    foreach(var pct in pctThreshold)
                    {                        
                        if(count > 1)
                        {
                            var numInThreshold = (int)Math.Round(Math.Abs(pct) / 100 * count);

                            if(numInThreshold == 0)
                            {
                                continue;
                            }

                            if(pct > 0)
                            {
                                thresholdBoundary = values[numInThreshold - 1];

                                sum = cumulativeValues[numInThreshold - 1];
                            }
                            else
                            {
                                thresholdBoundary = values[count - numInThreshold];

                                sum = cumulativeValues[count - 1] - cumulativeValues[count - numInThreshold - 1];
                            }
                            mean = sum / numInThreshold;
                        }

                        var cleanPct = pct.ToString(CultureInfo.InvariantCulture);

                        cleanPct = cleanPct.Replace('.', '_').Replace(',', '_').Replace("-", "top");

                        timerData[key]["mean_" + cleanPct] = mean;

                        timerData[key][(pct > 0 ? "upper_" : "lower_") + cleanPct] = thresholdBoundary;

                        timerData[key]["sum_" + cleanPct] = sum;
                    }

                    sum = cumulativeValues[count - 1];

                    mean = sum / count;

                    float sumOfDiffs = 0;

                    for (var i = 0; i < count; i++)                    
                        sumOfDiffs += (values[i] - mean) * (values[i] - mean);                    

                    var mid = (int)Math.Floor(count / 2.0);

                    var median = (count % 2 != 0) ? values[mid] : (values[mid - 1] + values[mid]) / 2;

                    var stddev = (float)Math.Sqrt(sumOfDiffs / count);

                    timerData[key]["std"] = stddev;

                    timerData[key]["upper"] = max;

                    timerData[key]["lower"] = min;

                    timerData[key]["count"] = timerCounters[key];

                    timerData[key]["count_ps"] = timerCounters[key] / (flushInterval / 1000);

                    timerData[key]["sum"] = sum;

                    timerData[key]["mean"] = mean;

                    timerData[key]["median"] = median;

                    // note: values bigger than the upper limit of the last bin are ignored, by design
                    /*conf = histogram || [];
                    bins = [];
                    for (var i = 0; i < conf.length; i++) {
                        if(key.indexOf(conf[i].metric) > -1) {
                            bins = conf[i].bins;
                            break;
                        }
                    }
                    if(bins.length) {
                        current_timer_data['histogram'] = {};
                    }
                    // the outer loop iterates bins, the inner loop iterates timer values;
                    // within each run of the inner loop we should only consider the timer value range that's within the scope of the current bin
                    // so we leverage the fact that the values are already sorted to end up with only full 1 iteration of the entire values range
                    var i = 0;
                    for (var bin_i = 0; bin_i < bins.length; bin_i++) {
                      var freq = 0;
                      for (; i < count && (bins[bin_i] == 'inf' || values[i] < bins[bin_i]); i++) {
                        freq += 1;
                      }
                      bin_name = 'bin_' + bins[bin_i];
                      current_timer_data['histogram'][bin_name] = freq;
                    }*/
                }

                foreach(var counter in counters.Keys.ToList())
                {
                    flushQueue.Enqueue(Tuple.Create(counter, ts, counters[counter]));

                    counters[counter] = 0;
                }

                foreach(var gauge in gauges.Keys.ToList())
                {
                    flushQueue.Enqueue(Tuple.Create(gauge, ts, gauges[gauge]));
                }

                foreach(var t in timerData.Keys.ToList())
                {
                    foreach(var tt in timerData[t].Keys)
                    {
                        flushQueue.Enqueue(Tuple.Create(t + "." + tt, ts, timerData[t][tt]));
                    }

                    timers[t] = new List<float>();

                    timerCounters[t] = 0;
                }

                flushQueue.Enqueue(Tuple.Create("statsify.queue_backlog", ts, (float)flushQueue.Count));
            }
        }

        private Database GetDb(string root, string metric)
        {
            var fullPath = Path.Combine(root, metric.Replace(".", @"\") + ".db");

            var directory = Path.GetDirectoryName(fullPath);

            if(directory != null && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            var downsampling = configuration.Downsampling.FirstOrDefault(d => Regex.IsMatch(metric, d.Pattern));

            if(downsampling == null) return null;

            var storage = configuration.Storage.FirstOrDefault(a => Regex.IsMatch(metric, a.Pattern));

            if(storage == null) return null;

            var retentonPolicy = RetentionPolicy.Parse(storage.Retention);

            var db = Database.OpenOrCreate(fullPath, downsampling.Factor, downsampling.Method, retentonPolicy);

            return db;
        }
    }
}