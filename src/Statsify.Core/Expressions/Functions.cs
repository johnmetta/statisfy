using System;
using System.Collections.Generic;
using System.Linq;
using Statsify.Core.Model;
using Statsify.Core.Util;

namespace Statsify.Core.Expressions
{
    public delegate double? DatapointAggregationFunction(IEnumerable<Datapoint> values);

    public enum Order
    {
        Ascending,

        Descending
    }

    public static class Functions
    {
        [Function("timeshift")]
        [Function("timeShift")]
        public static MetricSelector Timeshift(EvalContext context, MetricSelector selector, string offset)
        {
            var offsetDuration = ParseTimeSpan(offset);
            if(offsetDuration == null) return selector;

            return new MetricSelector(selector.Selector, selector.From.Add(offsetDuration.Value), selector.Until.Add(offsetDuration.Value));
        }

        [Function("abs")]
        public static Metric[] Abs(EvalContext context, Metric[] metrics)
        {
            return 
                metrics.Select(m => new Metric(m.Name, m.Series.Transform(v => v.HasValue ? Math.Abs(v.Value) : (double?)null))).ToArray();
        }

        [Function("scale")]
        public static Metric[] Scale(EvalContext context, Metric[] metrics, double scale)
        {
            return 
                metrics.Select(m => new Metric(m.Name, m.Series.Transform(v => v.HasValue ? v.Value * scale : (double?)null))).ToArray();
        }

        [Function("integral")]
        public static Metric[] Integral(EvalContext context, Metric[] metrics)
        {
            return 
                metrics.Select(m => {
                    var accumulator = 0d;
                    return new Metric(m.Name, m.Series.Transform(v => accumulator += (v ?? 0d)));
                }).
                ToArray();
        }

        [Function("alias_by_fragment")]
        [Function("aliasByNode")]
        public static Metric[] AliasByFragment(EvalContext context, Metric[] metrics, params int[] fragmentIndices)
        {
            return
                metrics.Select(m => {
                    var fragments = m.Name.Split('.').Where((s, i) => fragmentIndices.Contains(i));
                    var name = string.Join(".", fragments);

                    return new Metric(name, m.Series);
                }).
                ToArray();
        }

        [Function("alias")]
        public static Metric[] Alias(EvalContext context, Metric[] metrics, string alias)
        {
            return 
                metrics.
                    Select(m => new Metric(alias, m.Series)).
                    ToArray();
        }

        [Function("summarize")]
        public static Metric[] Summarize(EvalContext context, Metric[] metrics, string aggregationFunction, string bucket)
        {
            //
            // The double-parsing is due to legacy reasons. Statsify expects `summarize(metrics, aggregationFunction, bucket)`,
            // whereas Graphite-compatible clients expect `summarize(metrics, bucket, aggregationFunction)`.
            var bucketDuration = ParseTimeSpan(bucket) ?? ParseTimeSpan(aggregationFunction);
            if(!bucketDuration.HasValue) return metrics;

            var fn = ParseAggregationFunction(aggregationFunction) ?? ParseAggregationFunction(bucket);
            if(fn == null) return metrics;

            var until = context.Until.RoundToNearest(bucketDuration.Value);
            var from = context.From.RoundToNearest(bucketDuration.Value);

            return
                metrics.Select(m => {
                    if(m.Series.Interval > bucketDuration)
                        return m;

                    var series = new Queue<Datapoint>(m.Series.Datapoints.OrderByDescending(d => d.Timestamp));
                    var datapoints = new List<Datapoint>();

                    var timestamp = until.Subtract(bucketDuration.Value);
                    while(timestamp > from)
                    {
                        var value = fn(series.DequeueWhile(d => d.Timestamp >= timestamp));
                        var datapoint = new Datapoint(timestamp.Add(bucketDuration.Value), value);
                        datapoints.Add(datapoint);

                        timestamp = timestamp.Subtract(bucketDuration.Value);
                    } // while

                    return new Metric(m.Name, new Series(m.Series.From, m.Series.Until, bucketDuration.Value, datapoints.OrderBy(d => d.Timestamp)));
                }).
                ToArray();
        }

        [Function("aggregated_above")]
        public static Metric[] AggregatedAbove(EvalContext context, Metric[] metrics, string aggregationFunction, double threshold)
        {
            var fn = ParseAggregationFunction(aggregationFunction);
            if(fn == null) return metrics;

            return
                metrics.
                    Where(m => {
                        var aggregated = fn(m.Series.Datapoints);
                        return aggregated.HasValue && aggregated.Value > threshold;
                    }).
                    ToArray();
        }

        [Function("aggregated_below")]
        public static Metric[] AggregatedBelow(EvalContext context, Metric[] metrics, string aggregationFunction, double threshold)
        {
            var fn = ParseAggregationFunction(aggregationFunction);
            if(fn == null) return metrics;

            return
                metrics.
                    Where(m => {
                        var aggregated = fn(m.Series.Datapoints);
                        return aggregated.HasValue && aggregated.Value < threshold;
                    }).
                    ToArray();
        }

        [Function("window_aggregated_above")]
        public static Metric[] WindowAggregatedAbove(EvalContext context, Metric[] metrics, string window, string aggregationFunction, double threshold)
        {
            var timeSpan = ParseTimeSpan(window);
            if(timeSpan == null) return metrics;

            var fn = ParseAggregationFunction(aggregationFunction);
            if(fn == null) return metrics;

            var windowStart = DateTime.UtcNow.Subtract(timeSpan.Value);

            return
                metrics.
                    Where(m => {
                        var aggregated = fn(m.Series.Datapoints.Where(d => d.Timestamp >= windowStart));
                        return aggregated.HasValue && aggregated.Value > threshold;
                    }).
                    ToArray();
        }

        private static DatapointAggregationFunction ParseAggregationFunction(string aggregationFunction)
        {
            Func<IEnumerable<Datapoint>, double?> fn = null;

            switch(aggregationFunction.ToLowerInvariant())
            {
                case "max":
                    fn = vs => vs.Max(v => v.Value);
                    break;
                case "min":
                    fn = vs => vs.Min(v => v.Value);
                    break;
                case "avg":
                case "average":
                    fn = vs => vs.Average(v => v.Value);
                    break;
                case "median":
                    fn = vs => vs.Median(v => v.Value, (a, b) => a + b, (a, b) => a / b);
                    break;
                case "sum":
                    fn = vs => vs.Sum(v => v.Value);
                    break;
                case "first":
                    fn = vs => vs.First().Value;
                    break;
                case "last":
                    fn = vs => vs.Last().Value;
                    break;
            } // switch

            var @default = new Datapoint(DateTime.MinValue, 0);

            return vs => fn(vs.Where(v => v.Value.HasValue).DefaultIfEmpty(@default));
        }

        private static Order? ParseOrder(string order)
        {
            if(string.IsNullOrWhiteSpace(order)) return null;

            switch(order.ToLowerInvariant())
            {
                case "asc":
                case"ascending":
                    return Order.Ascending;
                case "des":
                case "desc":
                case "descending":
                    return Order.Ascending;
            } // switch

            return null;
        }

        private static TimeSpan? ParseTimeSpan(string bucket)
        {
            var i = 0;

            if(int.TryParse(bucket, out i))
                return TimeSpan.FromSeconds(i);
            
            var specifier = bucket.Substring(bucket.Length - 1).ToLowerInvariant();
            bucket = bucket.Substring(0, bucket.Length - 1);

            if(!int.TryParse(bucket, out i)) return null;

            switch(specifier)
            {
                case "s":
                    return TimeSpan.FromSeconds(i);
                case "m":
                    return TimeSpan.FromMinutes(i);
                case "h":
                    return TimeSpan.FromHours(i);
                case "d":
                    return TimeSpan.FromDays(i);
                default:
                    return null;
            } // switch
        }

        [Function("sort_by_name")]
        [Function("sortByName")]
        public static Metric[] SortByName(EvalContext context, Metric[] metrics)
        {
            return metrics.OrderBy(m => m.Name).ToArray();
        }

        [Function("sort_by_fragment")]
        public static Metric[] SortByFragment(EvalContext context, Metric[] metrics, int fragmentIndex)
        {
            return
                metrics.OrderBy(m => {
                    var fragment = m.Name.Split('.').Where((s, i) => i == fragmentIndex).SingleOrDefault();
                    return fragment;
                }).
                ToArray();
        }

        [Function("sort_by_aggregated")]
        public static Metric[] SortByAggregated(EvalContext context, Metric[] metrics, string aggregationFunction, string order)
        {
            var fn = ParseAggregationFunction(aggregationFunction);
            if(fn == null) return metrics;

            var o = ParseOrder(order);
            if(o == null) return metrics;

            var aggregated =
                metrics.
                    Select(m => {
                        var value = fn(m.Series.Datapoints);
                        return new { metric = m, value };
                    });

            aggregated = 
                o.Value == Order.Ascending ? 
                    aggregated.OrderBy(m => m.value) : 
                    aggregated.OrderByDescending(m => m.value);

            return aggregated.Select(m => m.metric).ToArray();
        }

        [Function("random_metrics")]
        public static Metric[] RandomMetric(EvalContext context, string name, int number)
        {
            var r = new Random();

            var from = context.From.ToUnixTimestamp();
            var until = context.Until.ToUnixTimestamp();

            var metrics = 
                Enumerable.Range(0, number).
                    Select(n => {
                        double value = 0;

                        var datapoints = 
                            Enumerable.Range(0, (int)(until - @from)).
                                Select(v => new Datapoint(context.From.AddSeconds(v), value = value + r.NextDouble() - 0.5));

                        var series = new Series(context.From, context.Until, TimeSpan.FromSeconds(1), datapoints);

                        return new Metric(number == 1 ? name : string.Format("{0}.{1}", name, n + 1), series);
                    }).
                    ToArray();

            return metrics;
        }

        [Function("random_metric")]
        public static Metric[] RandomMetric(EvalContext context, string name)
        {
            return RandomMetric(context, name, 1);
        }

        [Function("derivative")]
        public static Metric[] Derivative(EvalContext context, Metric[] metrics)
        {
            return 
                metrics.Select(m => {
                    double? prev = null;

                    return 
                        Metric.Transform(m,
                            (d => {
                                if(!d.HasValue || !prev.HasValue)
                                {
                                    prev = d;
                                    return (double?)null;
                                } // if

                                var v = d.Value - prev.Value;
                                prev = d.Value;

                                return v;
                            }));
                }).
                ToArray();
        }

        [Function("nonnegative_derivative")]
        public static Metric[] NonnegativeDerivative(EvalContext context, Metric[] metrics)
        {
            return 
                metrics.Select(m => {
                    double? prev = null;

                    return 
                        Metric.Transform(m,
                            (d => {
                                if(!d.HasValue || !prev.HasValue)
                                {
                                    prev = d;
                                    return (double?)null;
                                } // if

                                var diff = d.Value - prev.Value;
                                prev = d.Value;

                                return diff >= 0 ?
                                    (double?)diff :
                                    null;
                            }));
                }).
                ToArray();
        }


        [Function("ema")]
        [Function("exponential_moving_average")]
        public static Metric[] Ema(EvalContext context, Metric[] metrics, double smoothingFactor)
        {
            return
                metrics.Select(m => {
                    double? ema = 0, prevV = 0, prevEma = 0;
                    var n = 0;

                    return new Metric(m.Name, m.Series.Transform(v => {
                        if(!v.HasValue) return null;

                        if(n++ == 0)
                        {
                            prevV = prevEma = v.Value;
                            return v.Value;
                        } // if
                        else
                        {
                            ema = smoothingFactor * prevV + (1 - smoothingFactor) * prevEma;
                            prevV = v.Value;
                            prevEma = ema;

                            return ema;
                        } // else
                    }));
                }).
                ToArray();
        }

        [Function("keep_last_value")]
        public static Metric[] KeepLastValue(EvalContext context, Metric[] metrics)
        {
            return 
                metrics.Select(m => {
                    double? prev = null;

                    return 
                        Metric.Transform(m,
                            d => {
                                if(!d.HasValue)
                                    return prev;

                                prev = d.Value;
                                return d;
                            });
                }).
                ToArray();

        }

        [Function("sum")]
        public static Metric Sum(EvalContext context, Metric[] metrics)
        {
            if(metrics.Length == 0) return null;
            if(!metrics.AllEqual(m => m.Series.Interval)) return null;

            var interval = metrics[0].Series.Interval;
            var from = metrics.Min(m => m.Series.From);
            var until = metrics.Max(m => m.Series.Until);

            var datapoints = new List<Datapoint>();
            for(var timestamp = from; timestamp <= until; timestamp += interval)
            {
                datapoints.Add(new Datapoint(timestamp, metrics.SelectMany(m => m.Series.Datapoints.Where(d => d.Timestamp == timestamp)).Select(d => d.Value).Sum()));
            } // for

            return new Metric("", new Series(from, until, interval, datapoints));
        }

        [Function("group_by_fragment")]
        [Function("groupByNode")]
        public static Metric[] GroupByFragment(EvalContext context, Metric[] metrics, int fragmentIndex, string callback)
        {
            var metaMetrics =
                metrics.
                    Where(m => m.Name.Split('.').Length > fragmentIndex).
                    GroupBy(m => m.Name.Split('.')[fragmentIndex]);

            if(callback == "sum")
            {
                var result = 
                    metaMetrics.
                        Select(m => {
                            var grouped = Sum(context, m.ToArray());
                            var series = new Series(context.From, context.Until, grouped.Series.Interval, grouped.Series.Datapoints);

                            var nameFragments = m.First().Name.Split('.');
                            nameFragments[fragmentIndex] = "*";
                                        var name = nameFragments[fragmentIndex];// string.Join(".", nameFragments.Where((s, i) => i != fragmentIndex));

                            return new Metric(name, series);
                        }).
                        ToArray();

                return result;
            } // if
                
            return null;
        }

        [Function("most_deviant")]
        [Function("mostDeviant")]
        public static Metric[] MostDeviant(EvalContext context, Metric[] metrics, int n)
        {
            return 
                metrics.
                    Where(m => m.Series.Datapoints.Count > 0 && m.Series.Datapoints.Any(d => d.Value.HasValue)).
                    Select(m =>
                    {
                        var length = m.Series.Datapoints.Count;
                    
                        var sum = 
                            m.Series.Datapoints.
                                Where(d => d.Value.HasValue).
                                Select(d => d.Value.Value).
                                Aggregate<double, double>(0f, (current, value) => current + value);
                    
                        var mean = sum / length;
                        var squareSum = m.Series.Datapoints.Where(d => d.Value.HasValue).Select(d => Math.Pow(d.Value.Value - mean, 2)).Sum();
                        var sigma = squareSum / length;

                        return new { sigma, metric = m };
                    }).
                    OrderByDescending(m => m.sigma).
                    Take(n).
                    Select(m => m.metric).
                    ToArray();
        }

        [Function("offset_to_zero")]
        [Function("offsetToZero")]
        public static Metric[] OffsetToZero(EvalContext context, Metric[] metrics)
        {
            var min = 
                metrics.
                    Where(m => m.Series.Datapoints.Count > 0 && m.Series.Datapoints.Any(d => d.Value.HasValue)).
                    DefaultIfEmpty().
                    Min(m => m.Series.Datapoints.Where(d => d.Value.HasValue).Min(d => d.Value));

            return 
                metrics.
                    Select(m => Metric.Transform(m, d => d.HasValue ? d.Value - min : null)).
                    ToArray();
        }
    }
}
