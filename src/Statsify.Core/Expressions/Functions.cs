using System;
using System.Collections.Generic;
using System.Linq;
using Statsify.Core.Model;
using Statsify.Core.Util;

namespace Statsify.Core.Expressions
{
    public delegate double? DatapointAggregationFunction(IEnumerable<Datapoint> values);

    public static class Functions
    {
        [Function("timeshift")]
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
        public static Metric[] Summarize(EvalContext context, Metric[] metrics, string aggregationFunction, string bucket, bool align = false)
        {
            var bucketDuration = ParseTimeSpan(bucket);
            if(!bucketDuration.HasValue) return metrics;

            var fn = ParseAggregationFunction(aggregationFunction);
            if(fn == null) return metrics;

            var until = context.Until;
            var from = context.From.Subtract(bucketDuration.Value);
            var ranges = EnumerableUtil.Generate(until, dt => dt.Subtract(bucketDuration.Value), dt => from < dt).Reverse().ToRanges().ToArray();

            return
                metrics.Select(m => {
                    var datapoints = 
                        m.Series.Interval > bucketDuration ?
                            m.Series.Datapoints.ToList() :
                            ranges.Select(r => {
                                var datapoint = fn(m.Series.Datapoints.Where(d => d.Timestamp > r.From && d.Timestamp <= r.Until));
                                return new Datapoint(r.Until, datapoint);
                            }).ToList();

                    return new Metric(m.Name, new Series(m.Series.From, m.Series.Until, bucketDuration.Value, datapoints));
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
    }
}
