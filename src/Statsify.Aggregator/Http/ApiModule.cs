using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nancy;
using Nancy.Helpers;
using Nancy.Json;
using Nancy.ModelBinding;
using Statsify.Aggregator.ComponentModel;
using Statsify.Aggregator.Datagrams;
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
        public ApiModule(IMetricService metricService, IAnnotationRegistry annotationRegistry, IMetricRegistry metricRegistry, IMetricAggregator metricAggregator,
            ExpressionCompiler expressionCompiler) :
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
                var from = DateTimeParser.ParseDateTime(model.From, now, now.AddHours(-1));
                var until = DateTimeParser.ParseDateTime(model.Until, now, now);

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

            Get["series"] = x => GetSeries(metricRegistry, metricAggregator, expressionCompiler);

            Post["purge"] = x =>
            {

                var model = new PurgeModel();
                this.BindTo(model, new BindingConfig { BodyOnly = false });

                var now = DateTime.UtcNow;
                var from = DateTimeParser.ParseDateTime(model.From, now, now.AddYears(-1));

                metricRegistry.PurgeMetrics(from);

                return 204;
            };

            Post["metrics"] = x => PostMetrics(metricAggregator);
        }

        private object PostMetrics(IMetricAggregator metricAggregator)
        {
            byte[] buffer;
            using(var memoryStream = new MemoryStream())
            {
                Request.Body.CopyTo(memoryStream);
                buffer = memoryStream.ToArray();
            } // using

            var datagramParser = new DatagramParser(new MetricParser());
            var metrics = (MetricDatagram)datagramParser.ParseDatagram(buffer);

            foreach(var metric in metrics.Metrics)
                metricAggregator.Aggregate(metric);

            return 204;
        }

        private dynamic GetSeries(IMetricRegistry metricRegistry, IMetricAggregator metricAggregator, ExpressionCompiler expressionCompiler)
        {
            try
            {
                var model = new SeriesQueryModel();
                this.BindTo(model, new BindingConfig { BodyOnly = false });

                var now = DateTime.UtcNow;
                var from = DateTimeParser.ParseDateTime(model.From, now, now.AddHours(-1));
                var until = DateTimeParser.ParseDateTime(model.Until, now, now);

                var environment = new Statsify.Core.Expressions.Environment
                {
                    MetricRegistry = metricRegistry,
                    QueuedMetricDatapoints = metricAggregator.Queue
                };

                var evalContext = new EvalContext(@from, until);

                var metrics = new List<Core.Model.Metric>();
                foreach(var expression in model.Expression.Select(HttpUtility.UrlDecode))
                {
                    var e = expressionCompiler.Parse(expression).Single();

                    if(e is MetricSelectorExpression)
                    {
                        e = new EvaluatingMetricSelectorExpression(e as MetricSelectorExpression);
                    } // if

                    var r = (Core.Model.Metric[])e.Evaluate(environment, evalContext);

                    metrics.AddRange(r);
                } // foreach

                var seriesViewList = 
                    metrics.
                        Select(m => 
                            new SeriesView {
                                Target = m.Name,
                                Datapoints = 
                                    m.Series.Datapoints.
                                        Select(d => new[] { d.Value, d.Timestamp.ToUnixTimestamp() }).
                                        ToArray()
                            }).
                            ToArray();

                return Response.AsJson(seriesViewList);
            }
            catch(Exception e)
            {
                return Response.AsJson(new {e.Message, e.StackTrace}, HttpStatusCode.InternalServerError);
            }
        }
    }
}
