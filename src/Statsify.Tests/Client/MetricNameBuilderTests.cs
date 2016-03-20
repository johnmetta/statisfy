using NUnit.Framework;
using Statsify.Client;

namespace Statsify.Tests.Client
{
    [TestFixture]
    public class MetricNameBuilderTests
    {
        [Test]
        public void BuildMetricName()
        {
            Assert.AreEqual("namespace.metric_name", MetricNameBuilder.BuildMetricName("namespace", "metric_name"));
            Assert.AreEqual("namespace.metric_name", MetricNameBuilder.BuildMetricName("namespace..", ".metric_name"));
            Assert.AreEqual("name_space.metr_c_n__e_", MetricNameBuilder.BuildMetricName(".name\\space..", ".metr|c_n@:e ."));
        }
    }
}