using System.Net;
using System.Threading;
using NLog;
using Statsify.Aggregator.Configuration;
using Statsify.Aggregator.Network;
using Topshelf;
using Topshelf.Runtime;

namespace Statsify.Aggregator
{
    public class StatsifyAggregatorService : ServiceControl
    {
        private readonly StatsifyAggregatorConfigurationSection configuration;

        private readonly MetricParser metricParser;

        private readonly MetricAggregator metricAggregator;

        private Timer publisherTimer;

        private readonly ManualResetEvent stopEvent;

        private readonly Logger log = LogManager.GetCurrentClassLogger();        
        
        private UdpDatagramReader udpDatagramReader;

        public StatsifyAggregatorService(HostSettings hostSettings, ConfigurationManager configurationManager)
        {
            stopEvent = new ManualResetEvent(false);

            configuration = configurationManager.Configuration;

            metricAggregator = new MetricAggregator(configuration, stopEvent);

            metricParser = new MetricParser();
        }

        public bool Start(HostControl hostControl)
        {
            stopEvent.Reset();

            udpDatagramReader = new UdpDatagramReader(IPAddress.Parse(configuration.Endpoint.Address), configuration.Endpoint.Port);

            udpDatagramReader.DatagramHandler += UdpDatagramReaderDatagramHandler;

            publisherTimer = new Timer(PublisherTimerCallback, null, configuration.Storage.FlushInterval, configuration.Storage.FlushInterval);                       

            return true;
        }

        private void UdpDatagramReaderDatagramHandler(object sender, UdpDatagramEventArgs args)
        {
            foreach(var metric in metricParser.ParseMetrics(args.Buffer))
            {
                log.Trace("received metric '{0}' ({1}) with value {2}", metric.Name, metric.Type, metric.Value);

                metricAggregator.Aggregate(metric);
            }
        }

        public bool Stop(HostControl hostControl)
        {
            stopEvent.Set();

            if(publisherTimer != null)
                publisherTimer.Dispose();

            if(udpDatagramReader != null)
                udpDatagramReader.Dispose();
            
            /*if(httpServer != null)
                httpServer.Dispose();*/

            hostControl.Stop();

            return true;
        }

        public void Shutdown(HostControl hostControl)
        {
            stopEvent.Set();

            if(publisherTimer != null)
                publisherTimer.Dispose();

            if(udpDatagramReader != null)
                udpDatagramReader.Dispose();

            hostControl.Stop();
        }

        private void PublisherTimerCallback(object state)
        {
            metricAggregator.Flush();
        }
    }
}