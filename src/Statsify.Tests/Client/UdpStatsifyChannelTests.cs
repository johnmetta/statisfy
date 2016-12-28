using System;
using NUnit.Framework;
using Statsify.Client;

namespace Statsify.Tests.Client
{
    [TestFixture]
    public class UdpStatsifyChannelTests
    {
        [Test]
        [TestCase("udp:")]
        [TestCase("udp://localhost")]
        [TestCase("udp://localhost:8800")]
        public void ParseEndpoint(string uri)
        {
            var endpoint = UdpStatsifyChannel.ParseEndpoint(new Uri(uri), 
                UdpStatsifyChannel.DefaultUdpHost, UdpStatsifyChannel.DefaultUdpPort);
            Console.WriteLine(endpoint);
        }
    }
}
