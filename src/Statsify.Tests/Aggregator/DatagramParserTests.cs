using System.Text;
using NUnit.Framework;
using Statsify.Aggregator;
using Statsify.Aggregator.Datagrams;

namespace Statsify.Tests.Aggregator
{
    [TestFixture]
    public class DatagramParserTests
    {
        [Test]
        public void ParseAnnotationDatagram()
        {
            var datagramParser = new DatagramParser(new MetricParser());
            var datagram = datagramParser.ParseDatagram(Encoding.UTF8.GetBytes("datagram:annotation-v1:\n\0\0\0Deployment" + (char)24 + "\0\0\0Statsify Core Deployment"));

            Assert.IsInstanceOf<AnnotationDatagram>(datagram);

            var annotationDatagram = (AnnotationDatagram)datagram;

            Assert.AreEqual("Deployment", annotationDatagram.Title);
            Assert.AreEqual("Statsify Core Deployment", annotationDatagram.Message);
        }
    }
}
