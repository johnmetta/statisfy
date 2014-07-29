namespace Statsify.Web.Api.Models
{
    using System.Collections.Generic;

    public class Series
    {
        public Series()
        {
            DataPoints = new List<double?[]>();
        }

        public string Target { get; set; }        

        public List<double?[]> DataPoints { get; set; }

        internal MetricInfo Metric { get; set; }        
    }
}
