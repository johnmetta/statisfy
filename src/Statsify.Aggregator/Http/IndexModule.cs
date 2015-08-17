using System.Globalization;
using Nancy;
using Nancy.ModelBinding;
using Statsify.Aggregator.ComponentModel;

namespace Statsify.Aggregator.Http
{
    public class IndexModule : NancyModule
    {
        private readonly IMetricAggregator metricAggregator;

        public IndexModule(IMetricAggregator metricAggregator)
        {
            this.metricAggregator = metricAggregator;
            Get["/"] = GetIndex;
        }

        private object GetIndex(dynamic r)
        {
            var model = new IndexModel();
            model.Version = Application.Version.ToString(2);
            
            this.BindTo(model, new BindingConfig { BodyOnly = false });

            if(string.IsNullOrWhiteSpace(model.Expression))
                model.Expression = "sort_by_name(alias_by_fragment(summarize(servers.*.system.processor.total_time, \"max\", \"10m\"), 1, 4))";

            if(string.IsNullOrWhiteSpace(model.From))
                model.From = "-8h";

            model.QueueBacklog = metricAggregator.QueueBacklog.ToString("N0", CultureInfo.InvariantCulture);

            return View["index.html", model];
        }
    }

    public class IndexModel
    {
        public string Expression { get; set; }

        public string From { get; set; }

        public bool HasExpression
        {
            get { return !string.IsNullOrWhiteSpace(Expression); }
        }

        public string Version { get; set; }

        public string QueueBacklog { get; set; }
    }
}
