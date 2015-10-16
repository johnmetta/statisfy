﻿using System;
using System.Collections.Generic;
using System.Linq;
using Nancy;
using Nancy.ModelBinding;
using Statsify.Aggregator.ComponentModel;
using Statsify.Aggregator.Http.Models;
using Statsify.Aggregator.Http.Services;
using Statsify.Core.Components;
using Statsify.Core.Expressions;
using Statsify.Core.Util;

namespace Statsify.Aggregator.Http
{
    /// <summary>
    /// See:
    /// * https://github.com/brutasse/graphite-api/blob/master/docs/api.rst
    /// * http://graphite-api.readthedocs.org/en/latest/api.html
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

            Get["/metrics/find"] = Get["/metrics"] = Post["/metrics/find"] = Post["/metrics"] = QueryMetrics;
            Get["/render"] = Post["/render"] = RenderSeries;
        }

        private object QueryMetrics(dynamic r)
        {
            var model = new QueryMetricsModel();
            this.BindTo(model, new BindingConfig { BodyOnly = false });

            if(string.IsNullOrWhiteSpace(model.Query))
                return HttpStatusCode.BadRequest;

            if((model.Format ?? "").ToLowerInvariant() == "treejson")
                return new Response { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = "'treejson' format is not currently supported" };

            var now = DateTime.UtcNow;
            var from = Parser.ParseDateTime(model.From, now, now.AddHours(-1));
            var until = Parser.ParseDateTime(model.Until, now, now);

            var metrics = 
                metricService.Find(model.Query).
                    Select(m => new QueryMetricsResultModel {
                        is_leaf = m.IsLeaf ? 1 : 0,
                        name = m.Name,
                        path = m.IsLeaf ? m.Path.TrimEnd('.') : (m.Path.TrimEnd('.') + '.')
                    }).
                    ToList();

            if(model.Wildcards == 1)
                metrics.Add(new QueryMetricsResultModel { name = "*" });

            return Response.AsJson(new { metrics = metrics });
        }

        private dynamic RenderSeries(dynamic req)
        {
            try
            {
                var model = new RenderSeriesModel();
                this.BindTo(model, new BindingConfig { BodyOnly = false });

                var now = DateTime.UtcNow;
                var from = Parser.ParseDateTime(model.From, now, now.AddHours(-1));
                var until = Parser.ParseDateTime(model.Until, now, now);

                var environment = new Statsify.Core.Expressions.Environment {
                    MetricRegistry = metricRegistry,
                    QueuedMetricDatapoints = metricAggregator.Queue
                };

                var evalContext = new EvalContext(@from, until);

                var metrics = new List<Core.Model.Metric>();
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

        public class RenderSeriesModel
        {
            public string From { get; set; }
            public string Until { get; set; }
            public string Target { get; set; }
        }

        public class QueryMetricsModel
        {
            public string Query { get; set; }
            public string Format { get; set; }
            public int Wildcards { get; set; }
            public string From { get; set; }
            public string Until { get; set; }
            public string Jsonp { get; set; }
        }

        public class QueryMetricsResultModel
        {
            // ReSharper disable InconsistentNaming
            public int is_leaf { get; set; }
            public string name { get; set; }
            public string path { get; set; }
            // ReSharper restore InconsistentNaming
        }
    }
}
