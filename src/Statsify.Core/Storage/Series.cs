using System;

namespace Statsify.Core.Storage
{
    public class Series
    {
        public DateTime From { get; set; }

        public DateTime Until { get; set; }

        public TimeSpan Interval { get; set; }

        public double?[] Values { get; set; }

        public Series(DateTime @from, DateTime until, TimeSpan interval, double?[] values)
        {
            From = @from;
            Until = until;
            Interval = interval;
            Values = values;
        }
    }
}