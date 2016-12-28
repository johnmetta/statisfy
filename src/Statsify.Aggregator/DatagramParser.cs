using System;
using System.IO;
using System.Linq;
using System.Text;
using Statsify.Aggregator.Datagrams;

namespace Statsify.Aggregator
{
    public class DatagramParser
    {
        private static readonly byte[] DatagramSignature = Encoding.ASCII.GetBytes("datagram:");
        private static readonly byte[] AnnotationDatagramSignature = Encoding.ASCII.GetBytes("annotation-v1:");

        private readonly MetricParser metricParser;

        public DatagramParser(MetricParser metricParser)
        {
            this.metricParser = metricParser;
        }

        public Datagram ParseDatagram(byte[] buffer)
        {
            var dsl = DatagramSignature.Length;
            var adsl = AnnotationDatagramSignature.Length;

            if(Eq(buffer, DatagramSignature, 0, dsl) && Eq(buffer, AnnotationDatagramSignature, dsl, adsl))
            {
                var index = dsl + adsl;

                using (var memoryStream = new MemoryStream(buffer, index, buffer.Length - index))
                using (var binaryReader = new BinaryReader(memoryStream))
                {
                    var length = binaryReader.ReadInt32();
                    var bytes = binaryReader.ReadBytes(length);
                    var title = Encoding.UTF8.GetString(bytes);

                    length = binaryReader.ReadInt32();
                    bytes = binaryReader.ReadBytes(length);
                    var message = Encoding.UTF8.GetString(bytes);

                    return new AnnotationDatagram(title, message);
                } // using
            } // if
            else
            {
                var metrics = metricParser.ParseMetrics(buffer).ToArray();
                return new MetricDatagram(metrics);
            } // else

            return null;
        }

        private static bool Eq(byte[] a1, byte[] a2, int a1offset, int length)
        {
            if(a1 == null || a2 == null) return false;

            for(var i = 0; i < length; i++)
                if (a1[i + a1offset] != a2[i])
                    return false;

            return true;
        }
    }
}