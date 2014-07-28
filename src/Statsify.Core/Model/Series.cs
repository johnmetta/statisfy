using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Statsify.Core.Model
{
    public class Series
    {
        public DateTime From { get; private set; }

        public DateTime Until { get; private set; }

        public TimeSpan Interval { get; private set; }

        public ReadOnlyCollection<Datapoint> Datapoints { get; private set; }

        public Series(DateTime @from, DateTime until, TimeSpan interval, IEnumerable<Datapoint> datapoints)
        {
            Interval = interval;
            Until = until;
            From = @from;
            Datapoints = new ReadOnlyCollection<Datapoint>(new List<Datapoint>(datapoints));
        }
    }
}