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
        private const int Port = 8125;

        [Test]
        public void Increment()
        {
            using(var stats = new UdpStatsifyClient(port: Port))
            {
                stats.Increment("sample_counter");
                AssertDatagram(Port, "sample_counter:1|c", () => stats.Increment("sample_counter"));
            }
        }

        [Test]
        public void Decrement()
        {
            using(var stats = new UdpStatsifyClient(port: Port))
                AssertDatagram(Port, "sample_counter:-1|c", () => stats.Decrement("sample_counter"));
        }

        [Test]
        public void Gauge()
        {
            using(var stats = new UdpStatsifyClient("127.0.0.1", Port, "Telemetry.tests."))
            {
                AssertDatagram(Port, "Telemetry.tests.sample_gauge:300.05|g", () => stats.Gauge("sample_gauge", 300.05));
                AssertDatagram(Port, "Telemetry.tests.sample_gauge:1|g", () => stats.Gauge(".sample_gauge.", 1));
            } // using
        }

        [Test]
        public void GaugeDiff()
        {
            using(var stats = new UdpStatsifyClient("127.0.0.1", Port, "Telemetry.tests."))
            {
                AssertDatagram(Port, "Telemetry.tests.sample_gauge:+3.0055|g", () => stats.GaugeDiff("sample_gauge", 3.0055));
                AssertDatagram(Port, "Telemetry.tests.sample_gauge:-3.0055|g", () => stats.GaugeDiff("sample_gauge", -3.0055));

                AssertDatagram(Port, "Telemetry.tests.sample_gauge:+1|g", () => stats.GaugeDiff(".sample_gauge.", 1));
                AssertDatagram(Port, "Telemetry.tests.sample_gauge:-1|g", () => stats.GaugeDiff(".sample_gauge.", -1));

                AssertDatagram(Port, "Telemetry.tests.sample_gauge:+0|g", () => stats.GaugeDiff(".sample_gauge.", 0));
            } // using
        }

        [Test]
        public void Timer()
        {
            using (var stats = new UdpStatsifyClient("127.0.0.1", Port, "Telemetry.tests."))
            {
                AssertDatagram(Port, "Telemetry.tests.sample_timer:3.5|ms", () => stats.Time("sample_timer", 3.5));
                AssertDatagram(Port, "Telemetry.tests.sample_timer:1|ms", () => stats.Time(".sample_timer.", 1));
            } // using
        }

        [Test]
        public void Annotation()
        {
            using(var stats = new UdpStatsifyClient(port: Port))
            {
                AssertDatagram(Port, 
                    Encoding.UTF8.GetBytes("datagram:annotation-v1:\n\0\0\0Deployment" + (char)24 + "\0\0\0Statsify Core Deployment"), 
                    () => stats.Annotation("Deployment", "Statsify Core Deployment"));
            }
        }

        private void AssertDatagram(int port, string expectedDatagram, Action action)
        {
            AssertDatagram(port, Encoding.UTF8.GetBytes(expectedDatagram), action);
        }

        private void AssertDatagram(int port, byte[] expectedDatagram, Action action)
        {
            var completionEvent = new ManualResetEvent(false);
            byte[] actualDatagram = null;

            var ipEndpoint = new IPEndPoint(IPAddress.Any, port);
            using(var udpClient = new UdpClient(port))
            {
                udpClient.BeginReceive(ar => {
                    actualDatagram = udpClient.EndReceive(ar, ref ipEndpoint);
                    
                    completionEvent.Set();
                }, null);

                action();

                completionEvent.WaitOne();
            } // using

            CollectionAssert.AreEqual(expectedDatagram, actualDatagram);
        }
    }

    // ReSharper restore AccessToDisposedClosure
}
