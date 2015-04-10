using Statsify.Aggregator.Http.Models;

namespace Statsify.Aggregator.Http.Services
{
    public interface IMetricService
    {
        /// <summary>
        /// Finde metrics by query string.
        /// </summary>
        /// <param name="query"></param>
        /// <returns>Metrics.</returns>
        MetricInfo[] Find(string query);
    }
}
