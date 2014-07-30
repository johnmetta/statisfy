namespace Statsify.Web.Api.Models
{
    public class SeriesView
    {
        public string Target { get; set; }

        public double?[][] DataPoints { get; set; }
    }
}
