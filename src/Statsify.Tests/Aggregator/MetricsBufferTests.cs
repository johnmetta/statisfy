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
            Assert.AreEqual(1f, buffer.GetTimerCounter("a"));
            Assert.AreEqual(1f, buffer.GetTimerCounter("b"));

            buffer.Aggregate(Metric.Timer("a", 2));
            buffer.Aggregate(Metric.Timer("a", 2));
            buffer.Aggregate(Metric.Timer("a", 3));

            CollectionAssert.AreEquivalent(new[] { 1f, 2f, 2f, 3f }, timer("a"));
            Assert.AreEqual(4f, buffer.GetTimerCounter("a"));

            buffer.Aggregate(Metric.Timer("b", -12));

            CollectionAssert.AreEquivalent(new[] { 2f, -12f }, timer("b"));
            Assert.AreEqual(2f, buffer.GetTimerCounter("b"));
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

        [Test]
        public void AggregateCounters()
        {
            var buffer = new MetricsBuffer();

            Func<string, float?> counter = s => {
                var value = buffer.Counters.SingleOrDefault(kvp => kvp.Key == s);
                return value.Equals(default(KeyValuePair<string, float>)) ? (float?)null : value.Value;
            };

            buffer.Aggregate(Metric.Counter("a", 10));
            buffer.Aggregate(Metric.Counter("b", 0));

            // ReSharper disable PossibleInvalidOperationException
            Assert.AreEqual(10, counter("a").Value);
            Assert.AreEqual(0, counter("b").Value);

            buffer.Aggregate(Metric.Counter("a", 1));
            buffer.Aggregate(Metric.Counter("b", -1));

            Assert.AreEqual(11, counter("a").Value);
            Assert.AreEqual(-1, counter("b").Value);
            // ReSharper restore PossibleInvalidOperationException
        }

        [Test]
        public void AggregateSets()
        {
            var buffer = new MetricsBuffer();

            Func<string, int?> set = s => {
                var value = buffer.Sets.SingleOrDefault(kvp => kvp.Key == s);
                return value.Equals(default(KeyValuePair<string, int>)) ? (int?)null : value.Value;
            };

            buffer.Aggregate(Metric.Set("a", "1"));
            buffer.Aggregate(Metric.Set("b", "1"));
            buffer.Aggregate(Metric.Set("users_online", "1"));
            buffer.Aggregate(Metric.Set("users_online", "1"));
            buffer.Aggregate(Metric.Set("users_online", "1"));
            buffer.Aggregate(Metric.Set("users_online", "2"));
            buffer.Aggregate(Metric.Set("users_online", "1"));
            buffer.Aggregate(Metric.Set("users_online", "3"));

            // ReSharper disable PossibleInvalidOperationException
            Assert.AreEqual(1, set("a").Value);
            Assert.AreEqual(1, set("b").Value);
            Assert.IsFalse(set("c").HasValue);

            buffer.Aggregate(Metric.Set("a", "2"));
            buffer.Aggregate(Metric.Set("b", "1"));

            Assert.AreEqual(2, set("a").Value);
            Assert.AreEqual(1, set("b").Value);

            buffer.Aggregate(Metric.Set("a", "3"));
            buffer.Aggregate(Metric.Set("a", "4"));
            buffer.Aggregate(Metric.Set("a", "5"));
            buffer.Aggregate(Metric.Set("b", "2"));

            Assert.AreEqual(5, set("a").Value);
            Assert.AreEqual(2, set("b").Value);
            // ReSharper restore PossibleInvalidOperationException
        }
    }
}
