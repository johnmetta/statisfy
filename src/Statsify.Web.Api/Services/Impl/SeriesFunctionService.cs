using Statsify.Web.Api.Models;
using Metric = Statsify.Core.Model.Metric;

namespace Statsify.Web.Api.Services
{
    using System;
    using System.Linq;
    using Core.Storage;
    using Extensions;
    using Series = Models.Series;


    internal sealed class SeriesFunctionService : ISeriesFunctionService
    {
        private readonly IMetricService metricService;
        private readonly DateTime from;
        private readonly DateTime until;        
        private string seriesListExpression;

        private SeriesFunctionService(DateTime start, DateTime stop, IMetricService metricService)
        {
            this.metricService = metricService;

            from = start;

            until = stop;
        }        

        public Series[] Alias(Series[] series, string alias)
        {
            if (series == null)
                return null;

            foreach (var s in series)            
                s.Target = alias;

            return series;
        }

        public Series[] AliasByMetric(Series[] series)
        {
            if (series == null)
                return null;

            foreach (var s in series)
                s.Target = s.Metric.Name;

            return series;
        }

        public Series[] AliasByNode(Series[] series, params int[] nodes)
        {
            if (series == null)
                return null;

            foreach (var s in series)
            {
                var parts = s.Metric.Path.Split('.');

                var alias = nodes.Where(n => n <= (parts.Length - 1)).Select(n => parts[n]).ToArray();

                s.Target = String.Join(".", alias);
            }
            return series;
        }

        public Target[] List(string expression)
        {
            seriesListExpression = expression;

            var metrics = metricService.Find(expression);

            return (from metric in metrics.Where(m => m.IsLeaf)
                let databaseFilePath = metric.Info.FullName
                let db = Database.Open(databaseFilePath)
                let data = db.ReadSeries(@from, until)
                select new Target(metric.Name, data)).
                ToArray();
        }

        public Target[] Coalesce(Target[] targets)
        {
            if(targets == null) return null;

            var result = 
                targets.
                    Select(t => new Target(t.Name, t.Series.Transform(v => v ?? 0))).
                    ToArray();
        }

        public Series[] AverageSeries(Series[] seriesList)
        {
            if (seriesList == null)
                return null;

            var series = new Series
            {
                Target = seriesListExpression,

                Datapoints = seriesList.SelectMany(s => s.Datapoints)
                            .GroupBy(p => p[1])
                            .Select(x => new[] {x.Average(p => p[0]), x.Key})
                            .ToList()
            };            
            
            return new[] {series};
        }

        public Target[] Absolute(Target[] targets)
        {
            if(targets == null) return null;

            var result =
                targets.
                    Select(t => new Target(t.Name, t.Series.Transform(v => v.HasValue ? Math.Abs(v.Value) : (double?)null))).
                    ToArray();

            return result;
        }

        /*private Series[] Ema(Series[] series, float smoothingFactor)
        {
            float ema = 0, prevV = 0, prevEma = 0;
            var n = 0;

            foreach (var v in series)
            {
                if (n == 0)
                {
                    yield return v;
                    prevV = prevEma = v;
                } // if
                else
                {
                    ema = smoothingFactor * prevV + (1 - smoothingFactor) * prevEma;
                    yield return ema;

                    prevV = v;
                    prevEma = ema;
                } // else

                n++;
            }
        }

        private Series Ema(Series serie, float smoothingFactor)
        {
            float ema = 0, prevV = 0, prevEma = 0;
            var n = 0;

            foreach (var v in serie)
            {
                if (n == 0)
                {
                    yield return v;
                    prevV = prevEma = v;
                } // if
                else
                {
                    ema = smoothingFactor * prevV + (1 - smoothingFactor) * prevEma;
                    yield return ema;

                    prevV = v;
                    prevEma = ema;
                } // else

                n++;
            }
        }


        private Series[] Sma(Series[] series, int k)
        {
            float sma = 0;
            var n = 0;

            var prevs = new Queue<float>();

            foreach (var v in series)
            {
                if (n < k)
                {
                    sma += v / k;
                    prevs.Enqueue(v);
                    n++;
                    yield return v;
                } // if
                else
                {
                    yield return sma;
                    sma = sma - prevs.Dequeue() / k + v / k;
                    prevs.Enqueue(v);
                } // else
            } // foreach
        }*/

        public static ISeriesFunctionService GetSeriesFunctionService(DateTime start, DateTime stop,
            IMetricService metricService)
        {
            return new SeriesFunctionService(start, stop, metricService);
        }
    }
}
