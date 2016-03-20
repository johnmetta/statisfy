using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Statsify.Core.Model;

namespace Statsify.Aggregator
{
    public class MetricDatapointQueue : IEnumerable<MetricDatapoint>
    {
        private readonly ConcurrentQueue<MetricDatapoint> queue = new ConcurrentQueue<MetricDatapoint>();

        public int Count
        {
            get { return queue.Count; }
        }

        public void Enqueue(MetricDatapoint datapoint)
        {
            queue.Enqueue(datapoint);
        }

        public IEnumerator<MetricDatapoint> GetEnumerator()
        {
            return queue.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
