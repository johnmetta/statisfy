using System;
using System.Linq;
using Nancy;
using Nancy.Helpers;
using Nancy.Json;
using Nancy.ModelBinding;
using Statsify.Web.Api.Extensions;
using Statsify.Web.Api.Models;
using Statsify.Web.Api.Services;

namespace Statsify.Web.Api
{
    public class ApiModule : NancyModule
    {
        public ApiModule(IMetricService metricService, ISeriesService seriesService, IAnnotationService annotationService)
        {
            Get["/api/find/{query}"] = x => {
                string query = x.query;
                var metrics = metricService.Find(query);

                return Response.AsJson(metrics);
            };

            Get["/api/annotations"] = x => {

                JsonSettings.MaxJsonLength = int.MaxValue;

                DateTime start = Request.Query.start;
                DateTime stop = Request.Query.stop;

                var data = annotationService.List(start.ToUniversalTime(), stop.ToUniversalTime());
                var annotations = data.Select(a => new { Timestamp = a.Timestamp.ToUniversalTime().ToUnixTimestamp(), a.Message });

                return Response.AsJson(annotations);
            };

            Post["/api/annotations"] = x => {
                string title = Request.Form.title;
                string message = Request.Form.message;

                try
                {
                    annotationService.AddAnnotation(title, message);
                }
                catch (Exception e)
                {
                    return Response.AsJson(new { Success = false, e.Message });
                }

                return Response.AsJson(new { Success = true });
            };

            Get["/api/series"] = x => {
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
                        ).OrderBy(s => s.Target).ToArray();

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