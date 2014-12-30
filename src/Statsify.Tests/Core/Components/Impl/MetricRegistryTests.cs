using System.Linq;
using NUnit.Framework;
using Statsify.Core.Components.Impl;

namespace Statsify.Tests.Core.Components.Impl
{
    [TestFixture]
    public class MetricRegistryTests
    {
        [Test]
        public void ResolveMetricNames()
        {
            var metricRegistry = new MetricRegistry(@"c:\statsify");
            var metricNames = metricRegistry.ResolveMetricNames("servers.*.system.{processor,memory}.*").ToList();
        }
    }
}
