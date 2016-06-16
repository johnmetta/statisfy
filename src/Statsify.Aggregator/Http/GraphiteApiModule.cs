using System;
using System.Collections.Generic;
using System.Linq;
using Nancy;
using Nancy.ModelBinding;
using NLog;
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
        private readonly Logger log = LogManager.GetCurrentClassLogger();
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

            log.Debug("started querying metrics using '{0}'", model.Query);

            var metrics = 
                metricService.Find(model.Query).
                    Select(m => new QueryMetricsResultModel {
                        leaf = m.IsLeaf ? 1 : 0,
                        context = new object(),
                        text = m.Name,
                        expandable = m.IsLeaf ? 0 : 1,
                        id = m.Path, //m.IsLeaf ? m.Path.TrimEnd('.') : (m.Path.TrimEnd('.') + '.')
                        allowChildren = m.IsLeaf ? 0 : 1
                    }).
                    ToArray();

            /*if(model.Wildcards == 1)
                metrics.Add(new QueryMetricsResultModel { name = "*" });*/

            log.Debug("returning {0} metrics: '{1}'", metrics.Length, string.Join("', '", metrics.Select(m => m.text)));

            return Response.AsJson(metrics);
        }

        private dynamic RenderSeries(dynamic req)
        {
            try
            {
                var model = new RenderSeriesModel();
                this.BindTo(model, new BindingConfig { BodyOnly = false });

                var tgt = (string)Request.Form.target;

                var now = DateTime.UtcNow;
                var from = Parser.ParseDateTime(model.From, now, now.AddHours(-1));
                var until = Parser.ParseDateTime(model.Until, now, now);
                
                var targets = string.Join("', '", model.Target);
                log.Debug("started rendering metrics using '{0}' from {1:s} to {2:s} ({3})", targets, from, until, until - from);

                var environment = new Statsify.Core.Expressions.Environment {
                    MetricRegistry = metricRegistry,
                    QueuedMetricDatapoints = metricAggregator.Queue
                };

                var evalContext = new EvalContext(@from, until);

                var metrics = new List<Core.Model.Metric>();
                var scanner = new ExpressionScanner();

                var tokens = scanner.Scan(model.Target);

                var parser = new ExpressionParser();
                var expressions = 
                    parser.
                        Parse(new TokenStream(tokens)).
                            Select(e => 
                                e is MetricSelectorExpression ? 
                                    new EvaluatingMetricSelectorExpression(e as MetricSelectorExpression) : 
                                    e).
                            ToList();

                log.Debug("started evaluating expression");
                var r = expressions.SelectMany(e => (Core.Model.Metric[])e.Evaluate(environment, evalContext)).ToArray();
                log.Debug("evaluated expression");

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

                log.Debug("rendered {0} metrics using '{1}' from {2:s} to {3:s}", seriesViewList.Length, targets, from, until);

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
            public int leaf { get; set; }
            public object context { get; set; }
            public string text { get; set; }
            public int expandable { get; set; }
            public string id { get; set; }
            public int allowChildren { get; set; }
            // ReSharper restore InconsistentNaming
        }
    }
}
