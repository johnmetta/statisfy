using System;
using NUnit.Framework;
using Statsify.Client;

namespace Statsify.Tests.Client
{
    [TestFixture]
    public class HttpStatsifyChannelTests
    {
        [Test]
        [TestCase("http://statsify.int", ExpectedResult = "http://statsify.int/api/v1/metrics")]
        [TestCase("http://statsify.int/", ExpectedResult = "http://statsify.int/api/v1/metrics")]
        [TestCase("http://localhost/statsify", ExpectedResult = "http://localhost/statsify/api/v1/metrics")]
        [TestCase("http://localhost/statsify/", ExpectedResult = "http://localhost/statsify/api/v1/metrics")]
        [TestCase("http://localhost/statsify/api", ExpectedResult = "http://localhost/statsify/api/v1/metrics")]
        [TestCase("http://localhost/statsify/api/v1/", ExpectedResult = "http://localhost/statsify/api/v1/metrics")]
        public string GetPostUri(string uri)
        {
            var statsifyAggregatorApiUri = HttpStatsifyChannel.GetPostUri(new Uri(uri));
            return statsifyAggregatorApiUri.ToString();
        }
    }
}
