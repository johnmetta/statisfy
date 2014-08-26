using System.Diagnostics;

namespace Statsify.Core.Model
{
    [DebuggerDisplay("{Name,nq} - {Datapoint.Timestamp}: {Datapoint.Value}")]
    public struct Sample
    {
        public string Name { get; private set; }

        public Datapoint Datapoint { get; private set; }

        public Sample(string name, Datapoint datapoint) : this()
        {
            Name = name;
            Datapoint = datapoint;
        }
    }
}