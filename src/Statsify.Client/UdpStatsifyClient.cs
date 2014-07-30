using System;
using System.Globalization;
using System.Net.Sockets;
using System.Text;

namespace Statsify.Client
{
    public class UdpStatsifyClient : IStatsifyClient
    {
        private static readonly Random Sampler = new Random();

        private readonly string host;
        private readonly int port;
        private readonly string @namespace;
        private readonly UdpClient udpClient;

        public UdpStatsifyClient(string host = "127.0.0.1", int port = 8125, string @namespace = "")
        {
            this.host = host;
            this.port = port;
            this.@namespace = @namespace;
            udpClient = new UdpClient();
        }

        public void Counter(string metric, double value, double sample = 1)
        {
            PublishMetric(metric, "c", value, sample);
        }

        public void Gauge(string metric, double value, double sample = 1)
        {
            PublishMetric(metric, "g", value, sample);
        }

        public void GaugeDiff(string metric, double value, double sample = 1)
        {
            PublishMetric(metric, "g", value, sample, true);
        }

        public void Time(string metric, double value, double sample = 1)
        {
            PublishMetric(metric, "ms", value, sample);
        }

        public void Time(string metric, Action action, double sample = 1)
        {
            
        }

        private void PublishMetric(string metric, string type, double value, double sample, bool explicitlySigned = false)
        {
            if(sample < 1 && sample < Sampler.NextDouble()) return;
            
            var metricName = GetMetricName(metric);

            var metricValueFormat = explicitlySigned ? "{0:+#.####;-#.####;#}" : "{0}";
            var metricValue =
                value == 0 ?
                    (explicitlySigned ?
                        "+0" :
                        "0") :
                    string.Format(CultureInfo.InvariantCulture, metricValueFormat, (float)value);

            var datagram = string.Format(CultureInfo.InvariantCulture, "{0}:{1}|{2}", metricName, metricValue, type);
                
            if(sample < 1)
                datagram += string.Format(CultureInfo.InvariantCulture, "|@{0:N3}", (float)sample);

            PublishDatagram(datagram);
        }

        private string GetMetricName(string name)
        {
            return MetricNameBuilder.BuildMetricName(@namespace, name);
        }

        private void PublishDatagram(string datagram)
        {
            var buffer = Encoding.UTF8.GetBytes(datagram);
            udpClient.Send(buffer, buffer.Length, host, port);
        }

        public void Dispose()
        {
            if(udpClient != null)
                udpClient.Close();
        }
    }
}