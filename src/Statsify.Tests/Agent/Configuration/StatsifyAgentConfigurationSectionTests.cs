using System;
using NUnit.Framework;
using Statsify.Agent.Configuration;

namespace Statsify.Tests.Agent.Configuration
{
    [TestFixture]
    public class StatsifyAgentConfigurationSectionTests
    {
        [Test]
        public void Configure()
        {
            var configuration = ConfigurationManager.Instance;

            Assert.IsNotNull(configuration);
            Assert.AreEqual("192.168.0.1", configuration.Configuration.Statsify.Host);
            Assert.AreEqual(18125, configuration.Configuration.Statsify.Port);

            Assert.AreEqual(3, configuration.Configuration.Metrics.Count);
            Assert.AreEqual(TimeSpan.FromSeconds(5), configuration.Configuration.Metrics.CollectionInterval);

            Assert.AreEqual("system.processor.total_time", configuration.Configuration.Metrics[0].Name);
        }
    }
}
