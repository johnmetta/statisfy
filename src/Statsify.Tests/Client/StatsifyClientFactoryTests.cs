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
            var factory = new StatsifyClientFactory(s => s.Trim('%').ToLowerInvariant());
            var configuration = new StatsifyConfigurationSection {
                Host = "%STATSIFY_HOST%",
                Namespace = "%STATSIFY_NAMESPACE%"
            };

            var statsifyClient = factory.CreateStatsifyClient(configuration);
            Assert.IsInstanceOf<UdpStatsifyClient>(statsifyClient);

            var clientConfiguration = (IStatsifyClientConfiguration)statsifyClient;

            Assert.AreEqual("statsify_host", clientConfiguration.Host);
            Assert.AreEqual("statsify_namespace", clientConfiguration.Namespace);
        }
    }
}
