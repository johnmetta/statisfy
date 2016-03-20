namespace Statsify.Aggregator.Datagrams
{
    public class MetricDatagram : Datagram
    {
        public Metric[] Metrics { get; private set; }

        public MetricDatagram(Metric[] metrics)
        {
            Metrics = metrics;
        }
    }
}