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
            if(buffer.Take(DatagramSignature.Length).SequenceEqual(DatagramSignature))
            {
                buffer = buffer.Skip(DatagramSignature.Length).ToArray();

                if(buffer.Take(AnnotationDatagramSignature.Length).SequenceEqual(AnnotationDatagramSignature))
                {
                    buffer = buffer.Skip(AnnotationDatagramSignature.Length).ToArray();

                    using(var memoryStream = new MemoryStream(buffer))
                    using(var binaryReader = new BinaryReader(memoryStream))
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
            } // if
            else
            {
                var metrics = metricParser.ParseMetrics(buffer).ToArray();
                return new MetricDatagram(metrics);
            } // else

            return null;
        }
    }
}