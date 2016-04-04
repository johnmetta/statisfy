using NUnit.Framework;
using Statsify.Aggregator;

namespace Statsify.Tests.Aggregator
{
    [TestFixture]
    public class MetricParserTests
    {
        [Test]
        public void Parse()
        {
            var parser = new MetricParser();

            Assert.AreEqual(
                new Metric("gaugor", "+0", MetricType.Gauge, 1, true),
                parser.ParseMetric("gaugor:+0|g"));

            Assert.AreEqual(
                new Metric("gaugor", "-10", MetricType.Gauge, 1, true),
                parser.ParseMetric("gaugor:-10|g"));

            Assert.AreEqual(
                new Metric("gaugor", "+10", MetricType.Gauge, 1, true),
                parser.ParseMetric("gaugor:+10|g"));

            Assert.AreEqual(
                new Metric("glork", "320", MetricType.Timer, 5, false),
                parser.ParseMetric("glork:320|ms|@5"));

            Assert.AreEqual(
                new Metric("glork", "frob", MetricType.Set, 5, false),
                parser.ParseMetric("glork:frob|s|@5"));
        }
    }
}
