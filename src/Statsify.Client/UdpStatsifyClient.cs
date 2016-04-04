using System;
using System.Globalization;
using System.IO;
using System.Net.Sockets;
using System.Text;
using Statsify.Client.Configuration;

namespace Statsify.Client
{
    public class UdpStatsifyClient : IStatsifyClient, IStatsifyClientConfiguration, IDisposable
    {
        public const int DefaultPort = 8125;

        private static readonly Random Sampler = new Random();

        private readonly string host;
        private readonly int port;        
        private readonly string @namespace;
        private readonly UdpClient udpClient;

        public UdpStatsifyClient(IStatsifyClientConfiguration configuration) :
            this(configuration.Host, configuration.Port, configuration.Namespace)
        {
        }

        public UdpStatsifyClient(string host = "127.0.0.1", int port = DefaultPort, string @namespace = "")
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

        public void Annotation(string title, string message)
        {
            PublishAnnotation(title, message);
        }

        string IStatsifyClientConfiguration.Host
        {
            get { return host; }
        }

        int IStatsifyClientConfiguration.Port
        {
            get { return port; }
        }

        string IStatsifyClientConfiguration.Namespace
        {
            get { return @namespace; }
        }

        private void PublishMetric(string metric, string type, double value, double sample, bool explicitlySigned = false)
        {
            var cultureInfo = CultureInfo.InvariantCulture;

            var metricValueFormat = explicitlySigned ? "{0:+#.####;-#.####;#}" : "{0}";
            var metricValue =
                Math.Abs(value) < 0.00000001 ?
                    (explicitlySigned ? "+0" : "0") :
                    string.Format(cultureInfo, metricValueFormat, (float)value);

            PublishMetric(metric, type, metricValue, sample);
        }

        private void PublishMetric(string metric, string type, string value, double sample)
        {
            if(sample < 1 && sample < Sampler.NextDouble()) return;

            var cultureInfo = CultureInfo.InvariantCulture;
            var datagram = string.Format(cultureInfo, "{0}:{1}|{2}", GetMetricName(metric), value, type);

            if(sample < 1)
                datagram += string.Format(cultureInfo, "|@{0:N3}", (float)sample);

            PublishDatagram(datagram);
        }

        private void PublishAnnotation(string title, string message)
        {
            using(var memoryStream = new MemoryStream())
            {
                byte[] buffer;

                using(var binaryWriter = new BinaryWriter(memoryStream, Encoding.ASCII))
                {
                    binaryWriter.Write(Encoding.ASCII.GetBytes("datagram:"));
                    binaryWriter.Write(Encoding.ASCII.GetBytes("annotation-v1:"));

                    buffer = Encoding.UTF8.GetBytes(title);
                    binaryWriter.Write(buffer.Length);
                    binaryWriter.Write(buffer);

                    buffer = Encoding.UTF8.GetBytes(message);
                    binaryWriter.Write(buffer.Length);
                    binaryWriter.Write(buffer);
                } // using

                buffer = memoryStream.ToArray();

                Console.WriteLine("sending {0} bytes", buffer.Length);

                udpClient.Send(buffer, buffer.Length, host, port);           
            } // using
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