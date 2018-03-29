using System.Linq;
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
            var metricDefinitions = new MetricDefinitionFactory().CreateMetricDefinitions(new MetricConfigurationElement {
                Name = "logical_disk.**.percent_free_space",
                Type = "performance-counter",
                Path = @"\LogicalDisk(**)\% Free Space",
                AggregationStrategy = AggregationStrategy.Gauge
            }).ToList();
        }

        [Test]
        public void ParsePerformanceCounters()
        {
            var counters = MetricDefinitionFactory.ParsePerformanceCounters(@"\LogicalDisk(**)\Disk Read Bytes/sec").ToList();
            CollectionAssert.IsNotEmpty(counters);
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

        [Test]
        [TestCase("AllTickets@|sync1.0|AllTickets.svc", ExpectedResult = "alltickets.sync1_0_alltickets_svc")]
        [TestCase("AsiaService@|asiaservice|v5|AsiaService.svc", ExpectedResult = "asiaservice.asiaservice_v5_asiaservice_svc")]
        [TestCase("AsiaService@http:||asiaservice.aeroclub.int:8004|", ExpectedResult = "asiaservice.http_asiaservice_aeroclub_int_8004")]
        [TestCase("CompanyProfilesService@09.1|CompanyProfilesService.svc", ExpectedResult = "companyprofilesservice.09_1_companyprofilesservice_svc")]
        public string NormalizeWcfPerformanceCounterName(string counterName)
        {
            return MetricDefinitionFactory.NormalizeWcfPerformanceCounterName(counterName);
        }
    }
}
