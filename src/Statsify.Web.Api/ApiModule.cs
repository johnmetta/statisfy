

using Nancy.Helpers;

namespace Statsify.Web.Api
{
    using System;
    using Nancy.Json;
    using Models;
    using Services;
    using Nancy;
    using System.Linq;
    using Nancy.ModelBinding;
    using Extensions;
   
    public class ApiModule : NancyModule
    {
        public ApiModule(IMetricService metricService, ISeriesService seriesService, IAnnotationService annotationService)
        {

            Get["/api/find/{query}"] = x =>
            {
                string query = x.query;
                var metrics = metricService.Find(query);

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
                    return Response.AsJson(new { Success = false, e.Message });
                }

                return Response.AsJson(new { Success = true });
            };

            Get["/api/series"] = x =>
            {
                try
                {
                    JsonSettings.MaxJsonLength = int.MaxValue;

                    var model = new SeriesQueryModel();

                    this.BindTo(model, new BindingConfig { BodyOnly = false });

                    
                    var now = DateTime.UtcNow;

                    var start = model.Start.GetValueOrDefault(now.AddHours(-1)).ToUniversalTime();
                    var stop = model.Stop.GetValueOrDefault(now).ToUniversalTime();

                    var seriesList = model.Expression.SelectMany(q => seriesService.GetSeries(HttpUtility.UrlDecode(q), start, stop));

                    var seriesViewList = (from series in seriesList
                                          let @from = series.From.ToUnixTimestamp()
                                          let interval = series.Interval.ToUnixTimestamp()
                                          select new SeriesView
                                          {
                                              Target = series.Target,
                                              DataPoints = series.Values.Select((v, i) => new[] { v, @from + i * interval }).ToArray()
                                          }
                        ).OrderBy(s=>s.Target).ToArray();

                    return Response.AsJson(seriesViewList);                    
                }
                catch (Exception e)
                {
                    return Response.AsJson(new { e.Message, e.StackTrace });
                }
            };
        }
    }
}