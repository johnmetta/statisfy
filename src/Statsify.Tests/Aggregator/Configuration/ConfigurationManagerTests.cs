using System;
using NUnit.Framework;
using Statsify.Aggregator.Configuration;
using Statsify.Core.Storage;

namespace Statsify.Tests.Aggregator.Configuration
{
    [TestFixture]
    public class ConfigurationManagerTests
    {
        [Test]
        public void Configure()
        {
            var configuration = ConfigurationManager.Instance;

            Assert.IsNotNull(configuration);

            Assert.AreEqual("192.168.0.1", configuration.Configuration.UdpEndpoint.Address);
            Assert.AreEqual(18125, configuration.Configuration.UdpEndpoint.Port);

            Assert.AreEqual("192.168.0.1", configuration.Configuration.HttpEndpoint.Address);
            Assert.AreEqual(8081, configuration.Configuration.HttpEndpoint.Port);

            Assert.AreEqual(@"d:\statsify", configuration.Configuration.Storage.Path);

            Assert.AreEqual(TimeSpan.FromMinutes(10), 
                ((IRetentionDefinition)configuration.Configuration.Storage[0].Retentions[2]).Precision);
            Assert.AreEqual(TimeSpan.FromDays(365.25 * 5), 
                ((IRetentionDefinition)configuration.Configuration.Storage[0].Retentions[2]).History);

            var apdex = configuration.Configuration.Apdex;

            Assert.AreEqual("one.minute", apdex[0].Metric);
            Assert.AreEqual(TimeSpan.FromMinutes(1), apdex[0].Threshold);

            Assert.AreEqual("two.seconds", apdex[1].Metric);
            Assert.AreEqual(TimeSpan.FromSeconds(2), apdex[1].Threshold);
        }
    }
}
