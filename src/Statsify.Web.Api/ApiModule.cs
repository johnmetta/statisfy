using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Nancy;
using Nancy.Helpers;
using Nancy.Json;
using Nancy.ModelBinding;
using Statsify.Core.Components;
using Statsify.Core.Expressions;
using Statsify.Core.Model;
using Statsify.Core.Storage;
using Statsify.Core.Util;
using Statsify.Web.Api.Models;
using Statsify.Web.Api.Services;

namespace Statsify.Web.Api
{
    public class ApiModule : NancyModule
    {
        public ApiModule(IMetricService metricService, IAnnotationRegistry annotationRegistry, IMetricRegistry metricRegistry)
        {
            Get["/api/find/{query}"] = x => {
                string query = x.query;
                var metrics = metricService.Find(query);

                return Response.AsJson(metrics);
            };

            Get["/api/annotations"] = x => {

                JsonSettings.MaxJsonLength = int.MaxValue;

                DateTime start = DateTime.SpecifyKind(Request.Query.start, DateTimeKind.Utc);
                DateTime stop = DateTime.SpecifyKind(Request.Query.stop, DateTimeKind.Utc);

                var annotations = 
                    annotationRegistry.
                        ReadAnnotations(start, stop).
                        Select(a => new { Timestamp = a.Timestamp.ToUnixTimestamp(), a.Title, a.Message });;
                
                return Response.AsJson(annotations);
            };

            Post["/api/annotations"] = x => {
                string title = Request.Form.title;
                string message = Request.Form.message;

                annotationRegistry.WriteAnnotation(new Annotation(DateTime.UtcNow, title, message));

                return Response.AsJson(new { Success = true });
            };

            Get["/api/series"] = x => {
                try
                {
                    JsonSettings.MaxJsonLength = int.MaxValue;

                    var model = new SeriesQueryModel();

                    this.BindTo(model, new BindingConfig { BodyOnly = false });

                    var now = DateTime.UtcNow;

                    DateTime from, until;
                    if(string.IsNullOrWhiteSpace(model.From))
                        from = now.AddHours(-1);
                    else if(model.From.StartsWith("-"))
                    {
                        var offset = RetentionPolicy.ParseTimeSpan(model.From.Substring(1));
                        from = offset.HasValue ? now.Subtract(offset.Value) : now.AddHours(-1);
                    } // else
                    else
                        from = DateTime.Parse(model.From, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

                    if(string.IsNullOrWhiteSpace(model.Until))
                        until = now;
                    else if(model.Until.StartsWith("-"))
                    {
                        var offset = RetentionPolicy.ParseTimeSpan(model.Until.Substring(1));
                        until = offset.HasValue ? now.Subtract(offset.Value) : now;
                    } // else
                    else
                        until = DateTime.Parse(model.Until, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

                    var scanner = new ExpressionScanner();
                    var parser = new ExpressionParser();

                    var environment = new Statsify.Core.Expressions.Environment {
                        MetricRegistry = metricRegistry
                    };

                    var evalContext = new EvalContext(from, until);

                    var metrics = new List<Metric>();
                    foreach(var expression in model.Expression.Select(HttpUtility.UrlDecode))
                    {
                        Console.WriteLine("evaluating " + expression);

                        var tokens = scanner.Scan(expression);
                        var e = parser.Parse(new TokenStream(tokens));

                        if(e is MetricSelectorExpression)
                        {
                            e = new EvaluatingMetricSelectorExpression(e as MetricSelectorExpression);
                        } // if

                        var r = (Metric[])e.Evaluate(environment, evalContext);

                        metrics.AddRange(r);
                    } // foreach

                    var seriesViewList = (from metric in metrics
                                          let f = metric.Series.From.ToUnixTimestamp()
                                          let interval = metric.Series.Interval.ToUnixTimestamp()
                                          select new SeriesView
                                          {
                                              Target = metric.Name,
                                              DataPoints = metric.Series.Datapoints.Select((v, i) => new[] { v.Value, f + i * interval }).ToArray()
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