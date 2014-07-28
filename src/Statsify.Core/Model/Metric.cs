using System.Diagnostics;

namespace Statsify.Core.Model
{
    [DebuggerDisplay("{Name,nq}")]
    public class Metric
    {
        public string Name { get; private set; }

        public Series Series { get; private set; }

        public Metric(string name, Series series)
        {
            Name = name;
            Series = series;
        }
    }
}