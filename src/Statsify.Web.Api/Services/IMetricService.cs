namespace Statsify.Web.Api.Services
{
    using Models;

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
