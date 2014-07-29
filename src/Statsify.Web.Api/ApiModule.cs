namespace Statsify.Web.Api
{
    using System;
    using Nancy.Json;
    using Models;
    using Services;
    using Nancy;

    public class ApiModule : NancyModule
    {        
        public ApiModule(IMetricService metricService, ISeriesService seriesService)
        {

            Get["/api/find/{query}"] = x =>
            {
                MetricInfo[] metrics = metricService.Find(x.query);
               
                return Response.AsJson(metrics);
            };

            Get["/api/series"] = x =>
            {                           
                try
                {
                    JsonSettings.MaxJsonLength = int.MaxValue;  

                    string expression = Request.Query.expression;

                    DateTime? start = Request.Query.start;

                    DateTime? stop = Request.Query.stop;

                    if (String.IsNullOrWhiteSpace(expression))// ReSharper disable once NotResolvedInText                           
                        throw new ArgumentNullException("expression");

                    var now = DateTime.Now;

                    var series = seriesService.GetSeries(expression,
                        start.GetValueOrDefault(now.AddHours(-1)).ToUniversalTime(),
                        stop.GetValueOrDefault(now).ToUniversalTime());

                    return Response.AsJson(series);
                }
                catch(Exception e)
                {
                    return Response.AsJson(new { e.Message, e.StackTrace });
                }                                                
            };
        }
    }
}