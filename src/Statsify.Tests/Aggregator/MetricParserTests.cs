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
                new Metric("gaugor", "+0", MetricType.Gauge, 1),
                parser.ParseMetric("gaugor:+0|g"));

            Assert.AreEqual(
                new Metric("gaugor", "-10", MetricType.Gauge, 1),
                parser.ParseMetric("gaugor:-10|g"));

            Assert.AreEqual(
                new Metric("gaugor", "+10", MetricType.Gauge, 1),
                parser.ParseMetric("gaugor:+10|g"));

            Assert.AreEqual(
                new Metric("glork", "320", MetricType.Timer, 5),
                parser.ParseMetric("glork:320|ms|@5"));

            Assert.AreEqual(
                new Metric("glork", "frob", MetricType.Set, 5),
                parser.ParseMetric("glork:frob|s|@5"));

            Assert.AreEqual(
                new Metric("online", "hello@example.com", MetricType.Set, 5),
                parser.ParseMetric("online:hello@example.com|s|@5"));
        }
    }
}
