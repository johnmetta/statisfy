using Statsify.Aggregator.Api.Models;

namespace Statsify.Aggregator.Api.Services
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
