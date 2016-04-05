using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Statsify.Aggregator;

namespace Statsify.Tests.Aggregator
{
    [TestFixture]
    public class MetricsBufferTests
    {
        [Test]
        public void AggregateTimers()
        {
            var buffer = new MetricsBuffer();

            Func<string, IList<float>> timer = s => {
                var value = buffer.Timers.SingleOrDefault(kvp => kvp.Key == s);
                return value.Equals(default(KeyValuePair<string, IList<float>>)) ? null : value.Value;
            };

            buffer.Aggregate(Metric.Timer("a", 1));
            buffer.Aggregate(Metric.Timer("b", 2));

            CollectionAssert.AreEquivalent(new[] { 1f }, timer("a"));
            CollectionAssert.AreEquivalent(new[] { 2f }, timer("b"));
            Assert.IsNull(timer("c"));

            buffer.Aggregate(Metric.Timer("a", 2));
            buffer.Aggregate(Metric.Timer("a", 2));
            buffer.Aggregate(Metric.Timer("a", 3));

            CollectionAssert.AreEquivalent(new[] { 1f, 2f, 2f, 3f }, timer("a"));

            buffer.Aggregate(Metric.Timer("b", -12));

            CollectionAssert.AreEquivalent(new[] { 2f, -12f }, timer("b"));
        }

        [Test]
        public void AggregateGauges()
        {
            var buffer = new MetricsBuffer();

            Func<string, float?> gauge = s => {
                var value = buffer.Gauges.SingleOrDefault(kvp => kvp.Key == s);
                return value.Equals(default(KeyValuePair<string, float>)) ? (float?)null : value.Value;
            };

            buffer.Aggregate(Metric.Gauge("a", "10"));
            buffer.Aggregate(Metric.Gauge("b", "0"));

            // ReSharper disable PossibleInvalidOperationException
            Assert.AreEqual(10, gauge("a").Value);
            Assert.AreEqual(0, gauge("b").Value);

            buffer.Aggregate(Metric.Gauge("a", "+1"));
            buffer.Aggregate(Metric.Gauge("b", "1"));

            Assert.AreEqual(11, gauge("a").Value);
            Assert.AreEqual(1, gauge("b").Value);

            buffer.Aggregate(Metric.Gauge("a", "-7"));
            buffer.Aggregate(Metric.Gauge("b", "-10"));

            Assert.AreEqual(4, gauge("a").Value);
            Assert.AreEqual(-9, gauge("b").Value);
            // ReSharper restore PossibleInvalidOperationException
        }
    }
}
