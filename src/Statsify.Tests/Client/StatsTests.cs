using System;
using NUnit.Framework;
using Statsify.Client;
using Statsify.Client.Configuration;

namespace Statsify.Tests.Client
{
    [TestFixture]
    public class StatsTests
    {
        [Test]
        public void GetStatsifyClient()
        {
            Stats.EnvironmentVariableResolver = s => "";
            Stats.ConfigurationSectionResolver = () => null;

            Assert.IsInstanceOf<NullStatsifyClient>(Stats.GetStatsifyClient());
        }

        [Test]
        public void GetEnvironmentStatsifyConfiguration()
        {
            Stats.EnvironmentVariableResolver = s => null;
            Assert.IsNull(Stats.GetEnvironmentStatsifyConfiguration());

            var host = "statsify.local";
            var port = "8081";
            var @namespace = "stats";

            Stats.EnvironmentVariableResolver = s => {
                switch(s)
                {
                    case "STATSIFY_HOST":
                        return host;
                    case "STATSIFY_PORT":
                        return port;
                    case "STATSIFY_NAMESPACE":
                        return @namespace;
                    default:
                        throw new ArgumentOutOfRangeException("s");
                } // switch
            };

            var configuration = Stats.GetEnvironmentStatsifyConfiguration();

            Assert.AreEqual("statsify.local", configuration.Host);
            Assert.AreEqual(8081, configuration.Port);
            Assert.AreEqual("stats", configuration.Namespace);

            port = "port";

            configuration = Stats.GetEnvironmentStatsifyConfiguration();

            Assert.AreEqual("statsify.local", configuration.Host);
            Assert.AreEqual(UdpStatsifyClient.DefaultPort, configuration.Port);
            Assert.AreEqual("stats", configuration.Namespace);
        }

        [Test]
        public void GetConfigStatsifyConfiguration()
        {
            Stats.ConfigurationSectionResolver = () => null;
            Assert.IsNull(Stats.GetConfigStatsifyConfiguration());

            var host = "statsify.local";
            int port = 8081;
            var @namespace = "stats";

            Stats.ConfigurationSectionResolver = () => new StatsifyConfigurationSection { Host = host, Port = port, Namespace = @namespace };

            var configuration = Stats.GetConfigStatsifyConfiguration();

            Assert.AreEqual("statsify.local", configuration.Host);
            Assert.AreEqual(8081, configuration.Port);
            Assert.AreEqual("stats", configuration.Namespace);
        }
    }
}
