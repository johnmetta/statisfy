using System;
using System.Diagnostics;
using NUnit.Framework;
using Statsify.Agent.Configuration;
using Statsify.Agent.Impl;

namespace Statsify.Tests.Agent.Impl
{
    [TestFixture]
    public class MetricDefinitionFactoryTests
    {
        [Test]
        public void CreateMetricDefinitions()
        {
            var metricDefinition = new MetricDefinitionFactory().CreateMetricDefinitions(new MetricConfigurationElement {
                Name = "sql_server.lock_timeouts_sec",
                Type = "performance-counter",
                Path = @"\SQLServer:Locks(_Total)\Lock Timeouts/sec",
                AggregationStrategy = AggregationStrategy.Gauge
            });
        }

        [Test]
        public void ParsePerformanceCounterDefinition()
        {
            string machineName;
            string categoryName;
            string instanceName;
            string counterName;

            MetricDefinitionFactory.ParsePerformanceCounterDefinition(@"\\Local-Host\System\Threads",
                out machineName, out categoryName, out instanceName, out counterName);

            Assert.AreEqual("Local-Host", machineName);
            Assert.AreEqual("System", categoryName);
            Assert.IsNull(instanceName);
            Assert.AreEqual("Threads", counterName);

            MetricDefinitionFactory.ParsePerformanceCounterDefinition(@"\\Local-Host\LogicalDisk(C:)\Disk Read Bytes/sec",
                out machineName, out categoryName, out instanceName, out counterName);

            Assert.AreEqual("Local-Host", machineName);
            Assert.AreEqual("LogicalDisk", categoryName);
            Assert.AreEqual("C:", instanceName);
            Assert.AreEqual("Disk Read Bytes/sec", counterName);

            MetricDefinitionFactory.ParsePerformanceCounterDefinition(@"\LogicalDisk(C:)\Disk Read Bytes/sec",
                out machineName, out categoryName, out instanceName, out counterName);

            Assert.IsNull(machineName);
            Assert.AreEqual("LogicalDisk", categoryName);
            Assert.AreEqual("C:", instanceName);
            Assert.AreEqual("Disk Read Bytes/sec", counterName);
        }
    }
}
