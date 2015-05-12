namespace Statsify.Aggregator.ComponentModel
{
    public interface IMetricAggregator
    {
        int QueueBacklog { get; }
    }
}
