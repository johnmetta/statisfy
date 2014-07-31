using System.Globalization;
using System.Net;
using System.Text;
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

        private readonly AnnotationAggregator annotationAggregator;

        private Timer publisherTimer;

        private readonly ManualResetEvent stopEvent;

        private readonly Logger log = LogManager.GetCurrentClassLogger();        
        
        private UdpDatagramReader udpDatagramReader;

        private const string AnnotationDatagramPrefix = "annotation:";

        public StatsifyAggregatorService(HostSettings hostSettings, ConfigurationManager configurationManager)
        {
            stopEvent = new ManualResetEvent(false);

            configuration = configurationManager.Configuration;

            metricAggregator = new MetricAggregator(configuration, stopEvent);

            annotationAggregator = new AnnotationAggregator(configuration);

            metricParser = new MetricParser();
        }

        public bool Start(HostControl hostControl)
        {
            stopEvent.Reset();

            var ipAddress = IPAddress.Parse(configuration.Endpoint.Address);

            udpDatagramReader = new UdpDatagramReader(ipAddress, configuration.Endpoint.Port);

            udpDatagramReader.DatagramHandler += UdpDatagramReaderHandler;                        

            publisherTimer = new Timer(PublisherTimerCallback, null, configuration.Storage.FlushInterval, configuration.Storage.FlushInterval);                       

            return true;
        }

        private void UdpDatagramReaderHandler(object sender, UdpDatagramEventArgs args)
        {
            var datagram = Encoding.UTF8.GetString(args.Buffer);

            if (datagram.StartsWith(AnnotationDatagramPrefix, true, CultureInfo.InvariantCulture))
            {
                var annotation = datagram.Substring(AnnotationDatagramPrefix.Length);

                log.Trace("received annotation '{0}'", annotation);

                annotationAggregator.Aggregate(annotation);
            }
            else
            {
                foreach (var metric in metricParser.ParseMetrics(datagram))
                {
                    log.Trace("received metric '{0}' ({1}) with value {2}", metric.Name, metric.Type, metric.Value);

                    metricAggregator.Aggregate(metric);
                }   
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