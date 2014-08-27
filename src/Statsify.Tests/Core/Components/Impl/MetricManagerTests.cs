using System.Linq;
using NUnit.Framework;
using Statsify.Core.Components.Impl;

namespace Statsify.Tests.Core.Components.Impl
{
    [TestFixture]
    public class MetricManagerTests
    {
        [Test]
        public void ResolveMetricNames()
        {
            var metricManager = new MetricManager(@"c:\statsify");
            var metricNames = metricManager.ResolveMetricNames("servers.*.system.processor.total*").ToList();
        }
    }
}
