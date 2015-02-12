using Nancy;

namespace Statsify.Aggregator.Api
{
    public class ContentModule : NancyModule
    {
        public ContentModule()
        {
            Get["/content/statsify.css"] = GetStatsifyCss;

            Get["/content/statsify.js"] = r => GetStatsifyJs(r, false);
            Get["/content/statsify.min.js"] = r => GetStatsifyJs(r, true);
        }

        private object GetStatsifyCss(dynamic r)
        {
            throw new System.NotImplementedException();
        }

        private object GetStatsifyJs(dynamic r, bool min)
        {
            throw new System.NotImplementedException();
        }
    }
}
