using System;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using Statsify.Aggregator;

namespace Statsify.Tests.Aggregator
{
    [TestFixture]
    public class MetricParserTests
    {
        private static readonly Random random = new Random();

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

        [Test]
        public void ParsePerf()
        {
            var names = Enumerable.Range(0, 10000).Select(n => RandomString(n % 59)).ToArray();
            var metrics = names.SelectMany(n => new[] { n + ":+0|g", n + ":-10|g", n + ":+10|g", n + ":320|ms|@5", n + ":frob|s|@5" }).ToArray();

            var parser = new MetricParser();
            parser.ParseMetric("gaugor:+0|g");

            var stopwatch = Stopwatch.StartNew();

            const int N = 100;
            for(var i = 0; i < N; ++i)
                foreach(var metric in metrics)
                    parser.ParseMetric(metric);

            Console.WriteLine("{0} in {1}", N * metrics.Length, stopwatch.Elapsed);
        }

        
        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789 \\/~!@#$%^&*()_+";
            return new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
