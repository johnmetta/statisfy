using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using NLog;
using Statsify.Aggregator.ComponentModel;
using Statsify.Aggregator.Configuration;
using Statsify.Aggregator.Extensions;
using Statsify.Core.Model;

namespace Statsify.Aggregator
{
    public class MetricAggregator : IMetricAggregator
    {
        private readonly Logger log = LogManager.GetCurrentClassLogger();

        private readonly StatsifyAggregatorConfigurationSection configuration;
        private readonly IDatapointDatabaseResolver datapointDatabaseResolver;
        private readonly ManualResetEvent stopEvent;
        private readonly float flushInterval;
        
        private long metrics;
        private volatile MetricDatapointQueue metricDatapointQueue = new MetricDatapointQueue();
        private volatile MetricsBuffer metricsBuffer;

        public MetricAggregator(StatsifyAggregatorConfigurationSection configuration, IDatapointDatabaseResolver datapointDatabaseResolver, ManualResetEvent stopEvent)
        {
            this.configuration = configuration;
            this.datapointDatabaseResolver = datapointDatabaseResolver;
            this.stopEvent = stopEvent;
            flushInterval = (float)configuration.Storage.FlushInterval.TotalMilliseconds;

            metricsBuffer = new MetricsBuffer();

            var flushThread = new Thread(FlushCallback);
            flushThread.Start();
        }

        private void FlushCallback()
        {
            if(!Directory.Exists(configuration.Storage.Path))
            {
                log.Info("creating '{0}'", configuration.Storage.Path);
                Directory.CreateDirectory(configuration.Storage.Path);
            } // if

            var stopwatch = new Stopwatch();
            while(!stopEvent.WaitOne(TimeSpan.FromMinutes(5)))
                Flush(stopwatch);

            Flush(stopwatch);
        }

        private void Flush(Stopwatch stopwatch)
        {
            var n = 0;
            stopwatch.Restart();

            var fq = Interlocked.CompareExchange(ref metricDatapointQueue, new MetricDatapointQueue(), metricDatapointQueue);

            foreach(var g in fq.GroupBy(m => m.Name))
            {
                var datapoints = g.Select(md => md.Datapoint).ToList();
                n += datapoints.Count;

                var metric = g.Key;

                try
                {
                    var db = datapointDatabaseResolver.ResolveDatapointDatabase(metric);
                    if(db != null)
                        db.WriteDatapoints(datapoints);
                } // try
                catch(Exception e)
                {
                    var message = string.Format("could not write datapoints to '{0}'", metric);

                    log.ErrorException(message, e);
                } // catch
            } // foreach

            stopwatch.Stop();

            if(n > 0)
                log.Info("completed flushing {0:N0} entries in {1} ({2:N2} per second)", n, stopwatch.Elapsed, n / stopwatch.Elapsed.TotalSeconds);
        }

        public void Aggregate(Metric metric)
        {
#pragma warning disable 420
            //
            // See:
            // * http://msdn.microsoft.com/en-us/library/4bw5ewxy(VS.80).aspx
            // * http://stackoverflow.com/a/425150/60188
            var buffer = Volatile.Read(ref metricsBuffer);
#pragma warning restore 420

            Interlocked.Increment(ref metrics);
            buffer.Aggregate(metric);
        }

        public void Flush()
        {
#pragma warning disable 420
            //
            // See above.
            var buffer = Interlocked.Exchange(ref metricsBuffer, new MetricsBuffer());
#pragma warning restore 420
            
            var ts = DateTime.UtcNow;

            IDictionary<string, IDictionary<string, float>> timerData = new Dictionary<string, IDictionary<string, float>>();

            foreach(var pair in buffer.Timers.Where(k => k.Value.Count > 0))
            {
                var key = pair.Key;

                timerData[key] = new Dictionary<string, float>();

                var values = pair.Value.OrderBy(v => v).ToList();
                var count = values.Count;
                var min = values[0];
                var max = values[count - 1];
                var cumulativeValues = values.Accumulate().ToList();
                var sum = min;
                var mean = min;
                var thresholdBoundary = max;
                var pctThreshold = new[] { /*85.0, 90, */95.0, 99 };

                foreach(var pct in pctThreshold)
                {                        
                    if(count > 1)
                    {
                        var numInThreshold = (int)Math.Round(Math.Abs(pct) / 100 * count);

                        if(numInThreshold == 0) continue;

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

                var timerCounter = buffer.GetTimerCounter(key);
                if(timerCounter != null)
                {
                    timerData[key]["count"] = timerCounter.Value;
                    timerData[key]["count_ps"] = timerCounter.Value / (flushInterval / 1000);
                } // if

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

            var fq = Volatile.Read(ref metricDatapointQueue);
            foreach(var pair in buffer.Counters)
            {
                fq.Enqueue(new MetricDatapoint(pair.Key, ts, pair.Value));
                //  metricsBuffer.Aggregate(new Metric(pair.Key, 0, MetricType.Counter, 1, false));
            }

            foreach(var pair in buffer.Gauges)
            {
                fq.Enqueue(new MetricDatapoint(pair.Key, ts, pair.Value));
            }

            foreach(var pair in buffer.Sets)
            {
                fq.Enqueue(new MetricDatapoint(pair.Key, ts, pair.Value));
            }

            foreach(var t in timerData.Keys.ToList())
            {
                foreach(var tt in timerData[t].Keys)
                    fq.Enqueue(new MetricDatapoint(t + "." + tt, ts, timerData[t][tt]));

                /*timers[t] = new List<float>();
                timerCounters[t] = 0;*/
            }

            fq.Enqueue(new MetricDatapoint("statsify.queue_backlog", ts, fq.Count));
            fq.Enqueue(new MetricDatapoint("statsify.metrics.count", ts, metrics));
        }

        public int QueueBacklog
        {
            get { return metricDatapointQueue.Count; }
        }

        public IEnumerable<MetricDatapoint> Queue
        {
            get { return metricDatapointQueue; }
        }
    }
}