using System;
using System.Collections.Generic;
using System.Linq;
using Statsify.Core.Model;

namespace Statsify.Web.Api.Models
{
    public class Series : Core.Model.Series
    {
        public string Target { get; set; }
        
        internal MetricInfo Metric { get; set; }

        public IEnumerable<double?> Values
        {
            get { return Datapoints.Select(d => d.Value); }
        }

        public Series(DateTime @from, DateTime until, TimeSpan interval, IEnumerable<Datapoint> datapoints): 
            base(@from, until, interval, datapoints)
        {
        }
    }
}
