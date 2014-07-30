namespace Statsify.Web.Api.Models
{
    using System;

    public class Series : Core.Storage.Series
    {
        public Series(DateTime @from, DateTime until, TimeSpan interval, double?[] values) : base(@from, until, interval, values){}

        internal MetricInfo Metric { get; set; }

        public string Target { get; set; }  
    }
}
