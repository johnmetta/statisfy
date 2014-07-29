using System;
using Nancy;
using Nancy.Json;
using Statsify.Web.Api.Services;

namespace Statsify.Web.Api
{
    public class ApiModule : NancyModule
    {
        public ApiModule(IMetricService metricService, ISeriesService seriesService)
        {
            Get["/api/find/{query}"] = x =>
            {
                string query = x.query;
                var metrics = metricService.Find(query);

                return Response.AsJson(metrics);
            };

            Get["/api/metrics"] = x =>
            {
                try
                {
                    var expression = (string)Request.Query.expression;
                    
                    if(string.IsNullOrWhiteSpace(expression))
                        // ReSharper disable once NotResolvedInText
                        throw new ArgumentNullException("expression");

                    DateTime? start = Request.Query.start;
                    DateTime? stop = Request.Query.stop;

                    var now = DateTime.UtcNow;

                    var series = seriesService.GetSeries(expression,
                        start.GetValueOrDefault(now.AddHours(-1)),
                        stop.GetValueOrDefault(now));

                    JsonSettings.MaxJsonLength = int.MaxValue;
                    return Response.AsJson(series);
                }
                catch (Exception e)
                {
                    return Response.AsJson(new { e.Message, e.StackTrace });
                }
            };
        }
    }
}