using NUnit.Framework;
using Statsify.Client;

namespace Statsify.Tests.Client
{
    [TestFixture]
    public class MetricNameBuilderTests
    {
        [Test]
        [TestCase("namespace", "metric_name", ExpectedResult = "namespace.metric_name")]
        [TestCase("namespace..", ".metric_name", ExpectedResult = "namespace.metric_name")]
        [TestCase(".name\\space..", ".metr|c_n@:e .", ExpectedResult = "name_space.metr_c_n__e")]
        [TestCase(" namespace  ", "  metric_name ", ExpectedResult = "namespace.metric_name")]
        public string BuildMetricName(string @namespace, string name)
        {
            return MetricNameBuilder.BuildMetricName(@namespace, name);
        }
    }
}