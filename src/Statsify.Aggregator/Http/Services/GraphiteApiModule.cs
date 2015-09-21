using System;
using System.Collections.Generic;
using System.Linq;
using Nancy;
using Nancy.Helpers;
using Nancy.ModelBinding;
using Statsify.Aggregator.ComponentModel;
using Statsify.Aggregator.Http.Models;
using Statsify.Core.Components;
using Statsify.Core.Expressions;
using Statsify.Core.Util;

namespace Statsify.Aggregator.Http.Services
{
    /// <summary>
    /// See https://github.com/brutasse/graphite-api/blob/master/docs/api.rst
    /// </summary>
    public class GraphiteApiModule : NancyModule
    {
        private readonly IMetricService metricService;
        private readonly IMetricRegistry metricRegistry;
        private readonly IMetricAggregator metricAggregator;

        public GraphiteApiModule(IMetricService metricService, IMetricRegistry metricRegistry, IMetricAggregator metricAggregator) :
            base("/api/graphite/v1")
        {
            this.metricService = metricService;
            this.metricRegistry = metricRegistry;
            this.metricAggregator = metricAggregator;

            Get["/metrics/find"] = GetMetrics;
            Get["/metrics"] = GetMetrics;
            Post["/render"] = GetSeries;
        }


        private object GetMetrics(dynamic r)
        {
            var model = new MetricsQueryModel();
            this.BindTo(model, new BindingConfig { BodyOnly = false });

            if(string.IsNullOrWhiteSpace(model.Query))
                return HttpStatusCode.BadRequest;

            var now = DateTime.UtcNow;
            var from = Parser.ParseDateTime(model.From, now, now.AddHours(-1));
            var until = Parser.ParseDateTime(model.Until, now, now);

            var metrics = 
                metricService.Find(model.Query).
                    Select(m => new {
                        is_leaf = m.IsLeaf ? 1 : 0,
                        name = m.Name,
                        path = m.IsLeaf ? m.Path.TrimEnd('.') : (m.Path.TrimEnd('.') + '.')
                    });

            return Response.AsJson(new { metrics = metrics.ToArray() });
        }

            public class GraphiteRenderModel
    {
        public string From { get; set; }

        public string Until { get; set; }

        public string Target { get; set; }
    }


        private dynamic GetSeries(dynamic req)
        {
            try
            {
                var model = new GraphiteRenderModel();
                this.BindTo(model, new BindingConfig { BodyOnly = false });

                //
                // FIXME: Nancy

                var now = DateTime.UtcNow;
                var from = Parser.ParseDateTime(model.From, now, now.AddHours(-1));
                var until = Parser.ParseDateTime(model.Until, now, now);

                var environment = new Statsify.Core.Expressions.Environment
                {
                    MetricRegistry = metricRegistry,
                    QueuedMetricDatapoints = metricAggregator.Queue
                };

                var evalContext = new EvalContext(@from, until);

                var metrics = new List<Core.Model.Metric>();
                //foreach(var expression in model.Target)
                {
                    var scanner = new ExpressionScanner();
                    var tokens = scanner.Scan(model.Target);

                    var parser = new ExpressionParser();
                    var e = parser.Parse(new TokenStream(tokens));

                    if(e is MetricSelectorExpression)
                    {
                        e = new EvaluatingMetricSelectorExpression(e as MetricSelectorExpression);
                    } // if

                    var r = (Core.Model.Metric[])e.Evaluate(environment, evalContext);

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

    public class MetricsQueryModel
    {
        public string Query { get; set; }
        public string Format { get; set; }
        public int Wildcards { get; set; }
        public string From { get; set; }
        public string Until { get; set; }
        public string Jsonp { get; set; }
    }
}
