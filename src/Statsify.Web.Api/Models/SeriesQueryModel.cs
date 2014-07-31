namespace Statsify.Web.Api.Models
{
    using System;

    public class SeriesQueryModel
    {
        public DateTime? Start { get; set; }

        public DateTime? Stop { get; set; }

        public string[] Expression { get; set; }
    }
}
