using System;
using NUnit.Framework;
using Statsify.Client;

namespace Statsify.Tests.Client
{
    [TestFixture]
    public class HttpStatsifyChannelTests
    {
        [Test]
        [TestCase("http://statsify.int", Result = "http://statsify.int/api/v1/metrics")]
        [TestCase("http://statsify.int/", Result = "http://statsify.int/api/v1/metrics")]
        [TestCase("http://localhost/statsify", Result = "http://localhost/statsify/api/v1/metrics")]
        [TestCase("http://localhost/statsify/", Result = "http://localhost/statsify/api/v1/metrics")]
        [TestCase("http://localhost/statsify/api", Result = "http://localhost/statsify/api/v1/metrics")]
        [TestCase("http://localhost/statsify/api/v1/", Result = "http://localhost/statsify/api/v1/metrics")]
        public string GetPostUri(string uri)
        {
            var statsifyAggregatorApiUri = HttpStatsifyChannel.GetPostUri(new Uri(uri));
            return statsifyAggregatorApiUri.ToString();
        }
    }
}
