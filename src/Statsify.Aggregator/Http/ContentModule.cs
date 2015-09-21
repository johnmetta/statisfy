using System;
using System.IO;
using System.Reflection;
using System.Text;
using Nancy;

namespace Statsify.Aggregator.Http
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
            var memoryStream = new MemoryStream();
            using(var cs = GetContentStream("metricsgraphics.css"))
                cs.CopyTo(memoryStream);

            using(var cs = GetContentStream("statsify.css"))
                cs.CopyTo(memoryStream);

            memoryStream.Position = 0;

            return Response.FromStream(memoryStream, "text/css");
        }

        private object GetStatsifyJs(dynamic r, bool min)
        {
            var memoryStream = new MemoryStream();
            using(var cs = GetContentStream(min ? "d3.min.js" : "d3.js"))
                cs.CopyTo(memoryStream);

            var spacer = Encoding.ASCII.GetBytes(";" + Environment.NewLine + Environment.NewLine);
            memoryStream.Write(spacer, 0, spacer.Length);

            using(var cs = GetContentStream(min ? "metricsgraphics.min.js" : "metricsgraphics.js"))
                cs.CopyTo(memoryStream);

            memoryStream.Write(spacer, 0, spacer.Length);

            using(var cs = GetContentStream("statsify.js"))
                cs.CopyTo(memoryStream);

            memoryStream.Position = 0;

            return Response.FromStream(memoryStream, "text/javascript");
        }

        private static Stream GetContentStream(string name)
        {
            return Assembly.GetExecutingAssembly().GetManifestResourceStream("Statsify.Aggregator.Http.Content." + name);
        }
    }
}
