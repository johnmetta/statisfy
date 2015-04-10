using System;
using System.Collections.Generic;
using System.Linq;
using Nancy;
using Nancy.Helpers;
using Nancy.Json;
using Nancy.ModelBinding;
using Statsify.Aggregator.Http.Models;
using Statsify.Aggregator.Http.Services;
using Statsify.Core.Components;
using Statsify.Core.Expressions;
using Statsify.Core.Storage;
using Statsify.Core.Util;

namespace Statsify.Aggregator.Http
{
    public class ApiModule : NancyModule
    {
        public ApiModule(IMetricService metricService, IAnnotationRegistry annotationRegistry, IMetricRegistry metricRegistry) :
            base("/api/v1")
        {
            JsonSettings.MaxJsonLength = int.MaxValue;

            Get["find/{query}"] = x => {
                string query = x.query;
                var metrics = metricService.Find(query);

                return Response.AsJson(metrics);
            };

            Get["annotations"] = x => {

                var model = new AnnotationsQueryModel();
                this.BindTo(model, new BindingConfig { BodyOnly = false });
                
                var now = DateTime.UtcNow;
                var from = Parser.ParseDateTime(model.From, now, now.AddHours(-1));
                var until = Parser.ParseDateTime(model.Until, now, now);

                var annotations = 
                    annotationRegistry.
                        ReadAnnotations(from, until).
                        Where(a => model.Tag == null || model.Tag.Length == 0 || a.Tags.Intersect(model.Tag).Any()).
                        Select(a => new { Timestamp = a.Timestamp.ToUnixTimestamp(), a.Title, a.Message });;
                
                return Response.AsJson(annotations);
            };

            Post["annotations"] = x => {

                string title = Request.Form.title;
                string message = Request.Form.message;

                annotationRegistry.WriteAnnotation(new Annotation(DateTime.UtcNow, title, message));

                return Response.AsJson(new { Success = true });
            };

            Get["series"] = x => GetSeries(metricRegistry);
        }

        private dynamic GetSeries(IMetricRegistry metricRegistry)
        {
            try
            {
                var model = new SeriesQueryModel();
                this.BindTo(model, new BindingConfig {BodyOnly = false});

                var now = DateTime.UtcNow;
                var from = Parser.ParseDateTime(model.From, now, now.AddHours(-1));
                var until = Parser.ParseDateTime(model.Until, now, now);

                var environment = new Statsify.Core.Expressions.Environment
                {
                    MetricRegistry = metricRegistry
                };

                var evalContext = new EvalContext(@from, until);

                var metrics = new List<Core.Model.Metric>();
                foreach(var expression in model.Expression.Select(HttpUtility.UrlDecode))
                {
                    var scanner = new ExpressionScanner();
                    var tokens = scanner.Scan(expression);

                    var parser = new ExpressionParser();
                    var e = parser.Parse(new TokenStream(tokens));

                    if(e is MetricSelectorExpression)
                    {
                        e = new EvaluatingMetricSelectorExpression(e as MetricSelectorExpression);
                    } // if

                    var r = (Core.Model.Metric[]) e.Evaluate(environment, evalContext);

                    metrics.AddRange(r);
                } // foreach

                var seriesViewList = (from metric in metrics
                    let f = metric.Series.From.ToUnixTimestamp()
                    let interval = metric.Series.Interval.ToUnixTimestamp()
                    select new SeriesView
                    {
                        Target = metric.Name,
                        Datapoints = metric.Series.Datapoints.Select((v, i) => new[] {v.Value, f + i*interval}).ToArray()
                    }
                    ).OrderBy(s => s.Target).ToArray();

                return Response.AsJson(seriesViewList);
            }
            catch(Exception e)
            {
                return Response.AsJson(new {e.Message, e.StackTrace}, HttpStatusCode.InternalServerError);
            }
        }
    }
}
