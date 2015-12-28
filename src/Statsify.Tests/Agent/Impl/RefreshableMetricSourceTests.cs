using System;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using Statsify.Agent.Impl;

namespace Statsify.Tests.Agent.Impl
{
    [TestFixture]
    public class RefreshableMetricSourceTests
    {
        [Test]
        public void GetMetricDefinitions()
        {
            var refreshed = false;
            var currentTime = DateTime.UtcNow;
            var metricSource = new RefreshableMetricSource(Enumerable.Repeat(new NullMetricDefinition(), 1), TimeSpan.FromMinutes(1),
                () => {
                    refreshed = true;
                    return Enumerable.Repeat(new NullMetricDefinition(), 3);
                },
                () => currentTime);

            var metricDefinitions = metricSource.GetMetricDefinitions();
            Assert.AreEqual(1, metricDefinitions.Count());

            Thread.Sleep(TimeSpan.FromSeconds(1));

            Assert.IsFalse(refreshed);

            currentTime = currentTime.Add(TimeSpan.FromMinutes(2));

            metricSource.GetMetricDefinitions();
            Thread.Sleep(TimeSpan.FromSeconds(1));

            Assert.IsTrue(refreshed);

            metricDefinitions = metricSource.GetMetricDefinitions();
            Assert.AreEqual(3, metricDefinitions.Count());
        }
    }
}
