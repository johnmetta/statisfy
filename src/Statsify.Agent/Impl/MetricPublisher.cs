using System;
using System.Threading;
using System.Timers;
using NLog;
using Statsify.Agent.Configuration;
using Statsify.Client;

namespace Statsify.Agent.Impl
{
    public class MetricPublisher
    {
        private readonly Logger log = LogManager.GetCurrentClassLogger();

        private readonly MetricCollector metricCollector;

        private readonly IStatsifyClient statsifyClient;

        private readonly TimeSpan collectionInterval;

        private ManualResetEvent stopEvent;

        private ManualResetEvent stoppedEvent;
        private System.Timers.Timer publisherTimer;

        public MetricPublisher(MetricCollector metricCollector, IStatsifyClient statsifyClient, TimeSpan collectionInterval)
        {
            this.metricCollector = metricCollector;

            this.statsifyClient = statsifyClient;

            this.collectionInterval = collectionInterval;
        }

        public void Start()
        {
            stopEvent = new ManualResetEvent(false);

            stoppedEvent = new ManualResetEvent(false);

            publisherTimer = new System.Timers.Timer(collectionInterval.TotalMilliseconds) { AutoReset = false };
            publisherTimer.Elapsed += PublisherTimerCallback;

            publisherTimer.Start();
        }

        public void Stop()
        {
            if(publisherTimer == null) return;

            stopEvent.Set();
            publisherTimer.Stop();

            publisherTimer.Dispose();

            stopEvent.Dispose();

            stoppedEvent.Dispose();

            publisherTimer = null;
        }

        private void PublisherTimerCallback(object state, ElapsedEventArgs args)
        {
            foreach(var metric in metricCollector.GetCollectedMetrics())
            {
                log.Trace("publishing metric '{0}' with value '{1}'", metric.Name, metric.Value);
                
                switch(metric.AggregationStrategy)
                {
                    case AggregationStrategy.Gauge:
                        statsifyClient.Gauge(metric.Name, metric.Value);
                        break;

                    case AggregationStrategy.Counter:
                        statsifyClient.Counter(metric.Name, metric.Value);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            if(!stopEvent.WaitOne(0))
                publisherTimer.Start();
        }
    }
}