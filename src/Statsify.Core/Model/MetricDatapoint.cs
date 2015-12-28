using System;
using System.Diagnostics;

namespace Statsify.Core.Model
{
    [DebuggerDisplay("{Name,nq} - {Datapoint.Timestamp}:{Datapoint.Value}")]
    public class MetricDatapoint
    {
        public string Name { get; private set; }

        public Datapoint Datapoint { get; private set; }

        public MetricDatapoint(string name, Datapoint datapoint)
        {
            Name = name;
            Datapoint = datapoint;
        }

        public MetricDatapoint(string name, DateTime timestamp, double? value) :
            this(name, new Datapoint(timestamp, value))
        {
        }
    }
}