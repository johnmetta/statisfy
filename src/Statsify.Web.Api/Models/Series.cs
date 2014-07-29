namespace Statsify.Web.Api.Models
{
    using System.Collections.Generic;

    public class Series
    {
        public string Target { get; set; }

        public List<double?[]> Datapoints { get; set; }

        internal MetricInfo Metric { get; set; }

        public Series()
        {
            Datapoints = new List<double?[]>();
        }
    }
}
