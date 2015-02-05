using System.Net;
using System.Threading;
using NLog;
using Statsify.Aggregator.Configuration;
using Statsify.Aggregator.Datagrams;
using Statsify.Aggregator.Network;
using Topshelf;
using Topshelf.Runtime;

namespace Statsify.Aggregator
{
    public class StatsifyAggregatorService : ServiceControl
    {
        private readonly Logger log = LogManager.GetCurrentClassLogger();
        private readonly StatsifyAggregatorConfigurationSection configuration;
        private readonly MetricAggregator metricAggregator;
        private readonly AnnotationAggregator annotationAggregator;
        private readonly ManualResetEvent stopEvent;
        private readonly DatagramParser datagramParser;
        private UdpDatagramReader udpDatagramReader;
        private Timer publisherTimer;

        public StatsifyAggregatorService(HostSettings hostSettings, ConfigurationManager configurationManager)
        {
            stopEvent = new ManualResetEvent(false);
            configuration = configurationManager.Configuration;
            metricAggregator = new MetricAggregator(configuration, stopEvent);
            annotationAggregator = new AnnotationAggregator(configuration);
            
            datagramParser = new DatagramParser(new MetricParser());
        }

        public bool Start(HostControl hostControl)
        {
            stopEvent.Reset();

            var ipAddress = IPAddress.Parse(configuration.UdpUdpEndpoint.Address);

            udpDatagramReader = new UdpDatagramReader(ipAddress, configuration.UdpUdpEndpoint.Port);
            udpDatagramReader.DatagramHandler += UdpDatagramReaderHandler;                        

            publisherTimer = new Timer(PublisherTimerCallback, null, configuration.Storage.FlushInterval, configuration.Storage.FlushInterval);                       

            return true;
        }

        private void UdpDatagramReaderHandler(object sender, UdpDatagramEventArgs args)
        {
            var datagram = datagramParser.ParseDatagram(args.Buffer);
            AggregateDatagram(datagram);
        }

        private void AggregateDatagram(Datagram datagram)
        {
            if(datagram is AnnotationDatagram)
            {
                var annotationDatagram = datagram as AnnotationDatagram;
                annotationAggregator.Aggregate(annotationDatagram.Title, annotationDatagram.Message);
            } // if
            else if(datagram is MetricDatagram)
            {
                var metricDatagram = datagram as MetricDatagram;
                
                foreach(var metric in metricDatagram.Metrics)
                    metricAggregator.Aggregate(metric);
            } // else if
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