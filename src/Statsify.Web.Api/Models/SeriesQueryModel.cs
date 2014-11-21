namespace Statsify.Web.Api.Models
{
    public class SeriesQueryModel
    {
        public string From { get; set; }

        public string Until { get; set; }

        public string[] Expression { get; set; }
    }
}
