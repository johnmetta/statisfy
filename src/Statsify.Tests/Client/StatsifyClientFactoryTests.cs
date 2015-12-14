using System.Text.RegularExpressions;
using NUnit.Framework;
using Statsify.Client;
using Statsify.Client.Configuration;

namespace Statsify.Tests.Client
{
    [TestFixture]
    public class StatsifyClientFactoryTests
    {
        [Test]
        public void CreateNullStatsifyClient()
        {
            var factory = new StatsifyClientFactory();
            
            Assert.IsInstanceOf<NullStatsifyClient>(factory.CreateStatsifyClient(null));
            Assert.IsInstanceOf<NullStatsifyClient>(factory.CreateStatsifyClient(new StatsifyConfigurationSection()));
        }

        [Test]
        public void CreateStatsifyClient()
        {
            var factory = new StatsifyClientFactory(
                s => Regex.Replace(s, @"\%([^%]+)\%", m => m.Groups[1].Value.ToLowerInvariant()),
                s => true);

            var configuration = new StatsifyConfigurationSection {
                Host = "%STATSIFY_HOST%.local",
                Namespace = "%STATSIFY_NAMESPACE%.subnamespace"
            };

            var statsifyClient = factory.CreateStatsifyClient(configuration);
            Assert.IsInstanceOf<UdpStatsifyClient>(statsifyClient);

            var clientConfiguration = (IStatsifyClientConfiguration)statsifyClient;

            Assert.AreEqual("statsify_host.local", clientConfiguration.Host);
            Assert.AreEqual("statsify_namespace.subnamespace", clientConfiguration.Namespace);
        }

        [Test]
        public void CreateStatsifyClientWithInvalidHostname()
        {
            var factory = new StatsifyClientFactory(s => s, s => false);

            var configuration = new StatsifyConfigurationSection {
                Host = "statsify.local",
            };

            Assert.IsInstanceOf<NullStatsifyClient>(factory.CreateStatsifyClient(configuration));
        }
    }
}
