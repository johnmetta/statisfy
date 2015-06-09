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

        public MetricPublisher(MetricCollector metricCollector, IStatsifyClient statsifyClient, TimeSpan collectionInterval)
        {
            this.metricCollector = metricCollector;
            this.statsifyClient = statsifyClient;
            this.collectionInterval = collectionInterval;
        }

        public void Start()
        {
            log.Trace("starting MetricPublisher");

            stopEvent = new ManualResetEvent(false);
            stoppedEvent = new ManualResetEvent(false);
            
            ThreadPool.RegisterWaitForSingleObject(stopEvent, PublisherTimerCallback, null, collectionInterval, true);

            log.Trace("started MetricPublisher");
        }

        public void Stop()
        {
            log.Trace("stopping MetricPublisher");

            stopEvent.Set();
            stopEvent.Dispose();

            log.Trace("waiting for callback to stop");

            stoppedEvent.WaitOne();
            stoppedEvent.Dispose();

            log.Trace("stopping MetricPublisher");
        }

        private void PublisherTimerCallback(object state, bool timedOut)
        {
            if(!timedOut)
            {
                log.Trace("stopping callback");
                stoppedEvent.Set();
                log.Trace("stopped callback");

                return;
            } // if

            log.Trace("starting publishing metrics");
            var metrics = 0;

            foreach(var metric in metricCollector.GetCollectedMetrics())
            {
                metrics++;
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
                } // switch
            } // foreach

            log.Trace("completed publishing {0} metrics", metrics);
        
            ThreadPool.RegisterWaitForSingleObject(stopEvent, PublisherTimerCallback, null, collectionInterval, true);
        }
    }
}