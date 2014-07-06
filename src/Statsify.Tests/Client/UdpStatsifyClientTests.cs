using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using NUnit.Framework;
using Statsify.Client;

namespace Statsify.Tests.Client
{
    // ReSharper disable AccessToDisposedClosure
    [TestFixture]
    public class UdpStatsifyClientTests
    {
        private const int AggregationPort = 8125;

        [Test]
        public void Increment()
        {
            using(var stats = new UdpStatsifyClient())
                AssertDatagram(AggregationPort, "sample_counter:1|c", () => stats.Increment("sample_counter"));
        }

        [Test]
        public void Decrement()
        {
            using(var stats = new UdpStatsifyClient())
                AssertDatagram(AggregationPort, "sample_counter:-1|c", () => stats.Decrement("sample_counter"));
        }

        [Test]
        public void Gauge()
        {
            using(var stats = new UdpStatsifyClient("127.0.0.1", @namespace: "Telemetry.tests."))
            {
                AssertDatagram(AggregationPort, "Telemetry.tests.sample_gauge:300.05|g", () => stats.Gauge("sample_gauge", 300.05));
                AssertDatagram(AggregationPort, "Telemetry.tests.sample_gauge:1|g", () => stats.Gauge(".sample_gauge.", 1));
            } // using
        }

        [Test]
        public void GaugeDiff()
        {
            using(var stats = new UdpStatsifyClient("127.0.0.1", @namespace: "Telemetry.tests."))
            {
                AssertDatagram(AggregationPort, "Telemetry.tests.sample_gauge:+3.0055|g", () => stats.GaugeDiff("sample_gauge", 3.0055));
                AssertDatagram(AggregationPort, "Telemetry.tests.sample_gauge:-3.0055|g", () => stats.GaugeDiff("sample_gauge", -3.0055));

                AssertDatagram(AggregationPort, "Telemetry.tests.sample_gauge:+1|g", () => stats.GaugeDiff(".sample_gauge.", 1));
                AssertDatagram(AggregationPort, "Telemetry.tests.sample_gauge:-1|g", () => stats.GaugeDiff(".sample_gauge.", -1));

                AssertDatagram(AggregationPort, "Telemetry.tests.sample_gauge:+0|g", () => stats.GaugeDiff(".sample_gauge.", 0));
            } // using
        }

        [Test]
        public void Timer()
        {
            using(var stats = new UdpStatsifyClient("127.0.0.1", @namespace: "Telemetry.tests."))
            {
                AssertDatagram(AggregationPort, "Telemetry.tests.sample_timer:3.5|ms", () => stats.Time("sample_timer", 3.5));
                AssertDatagram(AggregationPort, "Telemetry.tests.sample_timer:1|ms", () => stats.Time(".sample_timer.", 1));
            } // using
        }

        private void AssertDatagram(int port, string expectedDatagram, Action action)
        {
            var completionEvent = new ManualResetEvent(false);
            var actualDatagram = "";

            var ipEndpoint = new IPEndPoint(IPAddress.Loopback, port);
            using(var udpClient = new UdpClient(ipEndpoint))
            {
                udpClient.BeginReceive(ar => {
                    var buffer = udpClient.EndReceive(ar, ref ipEndpoint);
                    actualDatagram = Encoding.UTF8.GetString(buffer);

                    completionEvent.Set();
                }, null);

                action();

                completionEvent.WaitOne();
            } // using

            Assert.AreEqual(expectedDatagram, actualDatagram);
        }
    }

    // ReSharper restore AccessToDisposedClosure
}
