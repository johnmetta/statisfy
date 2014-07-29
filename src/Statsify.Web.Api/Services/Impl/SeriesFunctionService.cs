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

        public Series[] AsPercent(Series[] series)
        {
            foreach(var metric in series)
            {
                var total = metric.DataPoints.Sum(p => p[0]);

                if(total.HasValue)
                    metric.DataPoints = metric.DataPoints.Select(p => new[] { p[0].HasValue ? (100 * p[0] / total) : (double?)null, p[1] }).ToList();
            }
                

            return series;
        }

        public Series[] Limit(Series[] series,int n)
        {
            return series.Take(n).ToArray();
        }

        public Series[] AsPercent(Series[] series, double total)
        {            
            foreach(var metric in series)
                metric.DataPoints = metric.DataPoints.Select(p => new[] { p[0].HasValue ? (100 * p[0] / total) : (double?)null, p[1] }).ToList();

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

        public Series[] Sum(Series[] series)
        {
            if (series == null)
                return null;

            var sl = new Series
            {
                Target = String.Format("Sum(\"{0}\")", seriesListExpression),
                DataPoints = series.SelectMany(s => s.DataPoints)
                    .GroupBy(p => p[1])
                    .Select(x => new[] { x.Sum(p => p[0]), x.Key })
                    .ToList()
            };

            return new[] { sl };
        }

        public Series[] List(string expression)
        {
            seriesListExpression = expression;

            var metrics = metricService.Find(expression);

            return (from metric in metrics.Where(m => m.IsLeaf)
                    let databaseFilePath = metric.Info.FullName
                    let db = Database.Open(databaseFilePath)
                    let data = db.ReadSeries(@from, until)
                    let s = data.From.ToUnixTimestamp()
                    let interval = data.Interval.ToUnixTimestamp()
                    select new Series
                    {
                        Metric = metric,
                        Target = metric.Path,
                        DataPoints = data.Values.Select((v, i) => new[] { v, s + i * interval }).ToList()
                    }).ToArray();
        }

        public Series[] AverageSeries(Series[] series)
        {
            if (series == null)
                return null;

            var sl = new Series
            {
                Target = seriesListExpression,

                DataPoints = series.SelectMany(s => s.DataPoints)
                            .GroupBy(p => p[1])
                            .Select(x => new[] {x.Average(p => p[0]), x.Key})
                            .ToList()
            };

            return new[] { sl };
        }

        public Series[] Absolute(Series[] series)
        {
            if (series == null)
                return null;

            foreach (var metric in series)            
                metric.DataPoints = metric.DataPoints.Select(p => new[] {p[0].HasValue?Math.Abs(p[0].Value):(double?)null, p[1]}).ToList();

            return series;
        }

        public static ISeriesFunctionService GetSeriesFunctionService(DateTime start, DateTime stop,
            IMetricService metricService)
        {
            return new SeriesFunctionService(start, stop, metricService);
        }
    }
}
