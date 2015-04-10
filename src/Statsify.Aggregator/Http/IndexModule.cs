using Nancy;
using Nancy.ModelBinding;

namespace Statsify.Aggregator.Http
{
    public class IndexModule : NancyModule
    {
        public IndexModule()
        {
            Get["/"] = GetIndex;
        }

        private object GetIndex(dynamic r)
        {
            var model = new IndexModel();
            model.Version = Application.Version.ToString(2);
            
            this.BindTo(model, new BindingConfig { BodyOnly = false });

            if(string.IsNullOrWhiteSpace(model.Expression))
                model.Expression = "sort_by_name(alias_by_fragment(summarize(servers.*.system.processor.*, \"max\", \"1m\"), 1, 4))";

            if(string.IsNullOrWhiteSpace(model.From))
                model.From = "-1h";

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
    }
}
