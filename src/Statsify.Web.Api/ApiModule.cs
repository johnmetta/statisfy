using System.Linq;
using Statsify.Web.Api.Extensions;

namespace Statsify.Web.Api
{
    using System;
    using Nancy.Json;
    using Models;
    using Services;
    using Nancy;

    public class ApiModule : NancyModule
    {        
        public ApiModule(IMetricService metricService, ISeriesService seriesService, IAnnotationService annotationService)
        {

            Get["/api/find/{query}"] = x =>
            {
                MetricInfo[] metrics = metricService.Find(x.query);
               
                return Response.AsJson(metrics);
            };

            Get["/api/annotations"] = x => {

                JsonSettings.MaxJsonLength = int.MaxValue;  

                DateTime start = Request.Query.start;
                DateTime stop = Request.Query.stop;

                var data = annotationService.List(start.ToUniversalTime(), stop.ToUniversalTime());
                var annotations = data.Select(a => new { Timestamp = a.Date.ToUniversalTime().ToUnixTimestamp(), a.Message });

                return Response.AsJson(annotations);
            };

            Post["/api/annotations"] = x => {

                string message = Request.Form.Message;

                try
                {
                    annotationService.AddAnnotation(message);
                }
                catch(Exception e)
                {
                    return Response.AsJson(e);
                }

                return null;
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

                    var seriesList = seriesService.GetSeries(expression,
                        start.GetValueOrDefault(now.AddHours(-1)).ToUniversalTime(),
                        stop.GetValueOrDefault(now).ToUniversalTime());

                    if(seriesList == null)
                        return null;

                    var seriesViewList = (from series in seriesList
                        let @from = series.From.ToUnixTimestamp()
                        let interval = series.Interval.ToUnixTimestamp()
                        select new SeriesView
                            {
                                Target = series.Target,
                                DataPoints = series.Values.Select((v, i) => new[] { v, @from + i * interval }).ToArray()
                            }
                        ).ToArray();                    

                    return Response.AsJson(seriesViewList);
                }
                catch(Exception e)
                {
                    return Response.AsJson(new { e.Message, e.StackTrace });
                }                                                
            };
        }
    }
}