using System;

namespace Statsify.Core.Expressions
{
    public class MetricSelector
    {
        public string Selector { get; private set; }

        public DateTime From { get; private set; }

        public DateTime Until { get; private set; }

        public MetricSelector(string selector, DateTime from, DateTime until)
        {
            Selector = selector;
            From = from;
            Until = until;
        }
    }
}