namespace Statsify.Web.Api.Services
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using Series = Models.Series;


    internal sealed class SeriesFunctionService : ISeriesFunctionService
    {        
        private readonly DateTime from;

        private readonly DateTime until;  
              
        private readonly string seriesListExpression;

        public SeriesFunctionService(DateTime start, DateTime stop, string seriesListExpression)
        {            
            from = start;

            until = stop;            

            this.seriesListExpression = seriesListExpression;
        }

        public Series[] Alias(Series[] seriesList, string alias)
        {
            if (seriesList == null)
                return null;

            foreach (var series in seriesList)
                series.Target = alias;

            return seriesList;
        }

        public Series[] AsPercent(Series[] seriesList)
        {
            if (seriesList == null)
                return null;

            foreach (var series in seriesList)
            {
                var total = series.Values.Sum();

                if(total.HasValue)
                    series.Values = series.Values.Select(value => value.HasValue ? (100 * value / total) : null).ToArray();

                series.Target = String.Format("AsPercent({0})", series.Target);
            }
                

            return seriesList;
        }

        public Series[] AsPercent(Series[] seriesList, double total)
        {
            if (seriesList == null)
                return null;

            foreach(var series in seriesList)
            {
                series.Values = series.Values.Select(value => value.HasValue ? (100 * value / total) : null).ToArray();

                series.Target = String.Format("AsPercent({0},{1})", series.Target, total);
            }                

            return seriesList;
        }

        public Series[] Limit(Series[] seriesList, int n)
        {
            if (seriesList == null)
                return null;

            return seriesList.Take(n).ToArray();
        }

        public Series[] AliasByMetric(Series[] seriesList)
        {
            if (seriesList == null)
                return null;

            foreach (var series in seriesList)
                series.Target = series.Metric.Name;

            return seriesList;
        }

        public Series[] AliasByNode(Series[] seriesList, params int[] nodes)
        {
            if (seriesList == null)
                return null;

            foreach (var series in seriesList)
            {
                var parts = series.Metric.Path.Split('.');

                var alias = nodes.Where(n => n <= (parts.Length - 1)).Select(n => parts[n]).ToArray();

                series.Target = String.Join(".", alias);
            }

            return seriesList;
        }

        public Series[] SumSeries(Series[] seriesList)
        {
            if (seriesList == null)
                return null;

            var series = seriesList.FirstOrDefault();

            var values = new List<double?>();

            if (series == null)
                return seriesList;

            for (var i = 0; i < series.Values.Length; i++)
            {
                double? value = null;

                foreach (var s in seriesList)
                {
                    if (s.Values[i].HasValue)
                        value = value.GetValueOrDefault() + s.Values[i];
                }

                values.Add(value);
            }

            return new[]
            {
                new Series(series.From, series.Until, series.Interval, values.ToArray())
                {
                    Target = String.Format("Sum({0})", seriesListExpression)
                }
            };
        }

        public Series[] Sum(Series[] seriesList)
        {
            return SumSeries(seriesList);
        }

        public Series[] CountSeries(Series[] seriesList)
        {
            if (seriesList == null)
                return null;

            var count = seriesList.Length;

            foreach (var series in seriesList)
            {
                series.Target = String.Format("CountSeries({0})", series.Target);

                series.Values = series.Values.Select(value => (double?)count).ToArray();
            }

            return seriesList; 
        }

        public Series[] Integral(Series[] seriesList)
        {
            if (seriesList == null)
                return null;

            foreach (var series in seriesList)
            {
                double? current = 0.0;
                var values = new List<double?>();

                foreach (var value in series.Values)
                {
                    if (value.HasValue)
                    {
                        current += value;
                        values.Add(current);
                    }
                    else
                    {
                        values.Add(null);
                    }
                }

                series.Target = String.Format("Integral({0})", series.Target);
                series.Values = values.ToArray();
            }

            return seriesList;
        }

        public Series[] NonNegativeDerivative(Series[] seriesList)
        {
            return NonNegativeDerivative(seriesList, null);
        }

        public Series[] NonNegativeDerivative(Series[] seriesList, double? maxValue)
        {
            if (seriesList == null)
                return null;

            foreach (var series in seriesList)
            {                
                var values = new List<double?>();

                double? prev = null;

                foreach (var value in series.Values)
                {
                    if(!(prev ?? value).HasValue)
                    {
                        values.Add(null);
                        prev = value;
                        continue;
                    }

                    var diff = value - prev;

                    if(diff >= 0)
                    {
                        values.Add(diff);
                    }
                    else
                    {
                        if(maxValue.HasValue && maxValue >= value)
                        {
                            values.Add((maxValue - prev) + value + 1);
                        }
                        else
                        {
                            values.Add(null);
                        }
                    }

                    prev = value;
                }

                series.Target = String.Format("NonNegativeDerivative({0})", series.Target);

                series.Values = values.ToArray();
            }

            return seriesList;
        }

        public Series[] MinimumAbove(Series[] seriesList, double n)
        {
            if (seriesList == null)
                return null;

            return seriesList.Where(s => s.Values.Min() > n).ToArray();
        }

        public Series[] MaximumBelow(Series[] seriesList, double n)
        {
            if (seriesList == null)
                return null;

            return seriesList.Where(s => s.Values.Max() <= n).ToArray();
        }

        public Series[] AverageSeries(Series[] seriesList)
        {
            if (seriesList == null)
                return null;

            var series = seriesList.FirstOrDefault();

            var values = new List<double?>();

            if (series == null)
                return seriesList;

            var length = seriesList.Length;

            for (var i = 0; i < series.Values.Length; i++)
            {
                double? value = null;

                foreach (var s in seriesList)
                {
                    if (s.Values[i].HasValue)
                        value = value.GetValueOrDefault() + s.Values[i];
                }

                value = value / length;

                values.Add(value);
            }

            return new[]
            {
                new Series(series.From, series.Until, series.Interval, values.ToArray())
                {
                    Target = String.Format("AverageSeries({0})", seriesListExpression)
                }
            };
        }

        public Series[] Avg(Series[] seriesList)
        {
            return AverageSeries(seriesList);
        }

        public Series[] Absolute(Series[] seriesList)
        {            
            if (seriesList == null)
                return null;

            foreach (var series in seriesList)
                series.Values = series.Values.Select(value => value.HasValue ? Math.Abs(value.Value) : (double?)null).ToArray();

            return seriesList;
        }

        public Series[] Abs(Series[] seriesList)
        {
            return Absolute(seriesList);
        }

    }
}
