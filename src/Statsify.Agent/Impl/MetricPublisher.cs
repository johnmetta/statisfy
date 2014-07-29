using System;
using System.Threading;
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

        private Timer publisherTimer;

        private volatile bool publishing;

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

            publisherTimer = new Timer(PublisherTimerCallback, null, collectionInterval, collectionInterval);

            publishing = false;
        }

        public void Stop()
        {
            if(publisherTimer == null) return;

            publisherTimer.Dispose(stoppedEvent);

            stopEvent.WaitOne();

            publisherTimer = null;

            stopEvent.Dispose();

            stoppedEvent.Dispose();
        }

        private void PublisherTimerCallback(object state)
        {
            if(publishing) return;

            publishing = true;

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

            publishing = false;
        }
    }
}