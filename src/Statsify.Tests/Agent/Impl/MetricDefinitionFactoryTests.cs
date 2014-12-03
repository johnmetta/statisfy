using NUnit.Framework;
using Statsify.Agent.Configuration;
using Statsify.Agent.Impl;

namespace Statsify.Tests.Agent.Impl
{
    [TestFixture]
    public class MetricDefinitionFactoryTests
    {
        [Test]
        public void CreateInstance()
        {
            var metricDefinition = new MetricDefinitionFactory().CreateInstance(new MetricConfigurationElement {
                Name = "sql_server.lock_timeouts_sec",
                Type = "performance-counter",
                Path = @"\SQLServer:Locks(_Total)\Lock Timeouts/sec",
                AggregationStrategy = AggregationStrategy.Gauge
            });
        }
    }
}
