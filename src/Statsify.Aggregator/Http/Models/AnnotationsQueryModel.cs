namespace Statsify.Aggregator.Http.Models
{
    public class AnnotationsQueryModel
    {
        public string From { get; set; }

        public string Until { get; set; }

        public string[] Tag { get; set; }
    }
}