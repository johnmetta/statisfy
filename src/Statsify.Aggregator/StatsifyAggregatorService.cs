using System;
using System.Net;
using System.Net.Sockets;
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
        private static readonly DateTime Epoch = DateTime.SpecifyKind(new DateTime(1970, 1, 1), DateTimeKind.Utc);

        private readonly StatsifyAggregatorConfigurationSection configuration;
        private readonly MetricParser metricParser;
        private readonly MetricAggregator metricAggregator;
        private Timer publisherTimer;
        private ManualResetEvent stopEvent;

        private readonly Logger log = LogManager.GetCurrentClassLogger();
        //private HttpServer httpServer;
        
        private UdpDatagramReader udpDatagramReader;

        private UdpClient udpClient;
        private IPEndPoint ipEndpoint;

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
            
            //httpServer = new HttpServer();

            return true;
        }

        private void UdpDatagramReaderDatagramHandler(object sender, UdpDatagramEventArgs args)
        {
            foreach(var metric in metricParser.ParseMetrics(args.Buffer))
            {
                log.Trace("received metric '{0}' ({1}) with value {2}", metric.Name, metric.Type, metric.Value);
                metricAggregator.Aggregate(metric);
            } // foreach
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