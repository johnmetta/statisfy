using System.Diagnostics;

namespace Statsify.Web.Api.Models
{
    [DebuggerDisplay("{Name,nq}")]
    public class Target
    {
        public string Name { get; private set; }

        public Core.Model.Series Series { get; private set; }

        public Target(string name, Core.Model.Series series)
        {
            Name = name;
            Series = series;
        }
    }

    [DebuggerDisplay("{Name,nq}")]
    public class Metric
    {
        public string Name { get; private set; }

        public Core.Model.Series Series { get; private set; }

        public Metric(string name, Core.Model.Series series)
        {
            Name = name;
            Series = series;
        }
    }
}
