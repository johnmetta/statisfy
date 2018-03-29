using System.IO;
using System.Linq;
using System.Reflection;
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
            string path = @"c:\statisfy";
            var metricRegistry = new MetricRegistry(Path.GetFullPath(path).Replace(@"file:&quot;", string.Empty));
            var metricNames = metricRegistry.ResolveMetricNames("servers.*.system.{processor,memory}.*").ToList();
        }
    }
}
