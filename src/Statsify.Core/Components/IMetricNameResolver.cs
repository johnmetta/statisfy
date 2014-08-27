using System.Collections.Generic;

namespace Statsify.Core.Components
{
    public interface IMetricNameResolver
    {
        IEnumerable<string> ResolveMetricNames(string metricNameSelector);
    }
}