using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using Statsify.Agent.Configuration;
using Statsify.Agent.Util;
using Statsify.Client;

namespace Statsify.Agent.Impl
{
    public class MemcachedMetricSource : IMetricSource
    {
        private readonly AggregationStrategy aggregationStrategy;
        private readonly string name;
        private readonly Uri uri;

        public MemcachedMetricSource(IMetricConfiguration metric)
        {
            name = metric.Name;
            aggregationStrategy = metric.AggregationStrategy;
            uri = new Uri(Environment.ExpandEnvironmentVariables(metric.Path));
        }

        public IEnumerable<IMetricDefinition> GetMetricDefinitions()
        {
            using(var tcpClient = new TcpClient())
            {
                tcpClient.Connect(uri.Host, uri.Port);

                using(var networkStream = tcpClient.GetStream())
                {
                    WriteString(networkStream, "stats\n");

                    var stats = 
                        ReadStrings(networkStream).
                            TakeWhile(s => !s.Equals("END", StringComparison.InvariantCultureIgnoreCase)).
                            Where(s => s.StartsWith("STAT ")).
                            Select(s => s.SubstringAfter("STAT ")).
                            Select(s => s.Split(' '));

                    foreach(var stat in stats)
                    {
                        long value;
                        if(!long.TryParse(stat[1], out value)) continue;

                        var metric = stat[0];

                        yield return new MetricDefinition(
                            MetricNameBuilder.BuildMetricName(name, metric),
                            () => value,
                            aggregationStrategy);
                    } // foreach

                    WriteString(networkStream, "quit\n");
                } // using
            } // using
        }

        public void InvalidateMetricDefinition(IMetricDefinition metricDefinition)
        {
        }

        private static void WriteString(NetworkStream networkStream, string value)
        {
            var buffer = Encoding.ASCII.GetBytes(value);
            networkStream.Write(buffer, 0, buffer.Length);
        }

        private static IEnumerable<string> ReadStrings(NetworkStream networkStream)
        {
            using(var streamReader = new StreamReader(new NonCloseableStreamWrapper(networkStream), Encoding.ASCII))
            {
                var line = "";
                while((line = streamReader.ReadLine()) != null)
                    yield return line;
            } // using
        }
    }
}
