using System;
using System.Linq;
using NUnit.Framework;
using Statsify.Agent.Configuration;
using Statsify.Agent.Impl;

namespace Statsify.Tests.Agent.Impl
{
    [TestFixture]
    public class MemcachedMetricSourceTests
    {
        [Test]
        public void GetMetricDefinitions()
        {
            var configuration = new MetricConfiguration("memcached", "memcached", "tcp://mow1aps2:11211", AggregationStrategy.Gauge, null);
            var memcachedMetricSource = new MemcachedMetricSource(configuration);

            try
            {
                var metricDefinitions = memcachedMetricSource.GetMetricDefinitions().ToList();

                Assert.IsNotEmpty(metricDefinitions);

                foreach (var metricDefinition in metricDefinitions)
                    Console.WriteLine("{0} - {1}", metricDefinition.Name, metricDefinition.GetNextValue());
            }
            catch (System.Net.Sockets.SocketException e)
            {
                System.Diagnostics.Debug.WriteLine("Memcached not installed. No test run");
            }
        }
    }
}
