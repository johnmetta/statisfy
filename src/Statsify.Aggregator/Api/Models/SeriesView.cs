namespace Statsify.Aggregator.Api.Models
{
    public class SeriesView
    {
        public string Target { get; set; }

        public double?[][] Datapoints { get; set; }
    }
}
