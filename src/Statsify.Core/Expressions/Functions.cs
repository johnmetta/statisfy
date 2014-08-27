using System;
using System.Collections.Generic;
using System.Linq;
using Statsify.Core.Model;

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
                    var name = 
                        string.Join(
                            ".",
                            m.Name.
                                Split('.').
                                Where((s, i) => fragmentIndices.Contains(i)));

                    return new Metric(name, m.Series);
                }).
                ToArray();
        }

        [Function("summarize")]
        public static Metric[] Summarize(EvalContext context, Metric[] metrics, string aggregationFunction, string bucket)
        {
            var bucketDuration = ParseTimeSpan(bucket);
            if(bucketDuration == null) return metrics;

            DatapointAggregationFunction fn = null;

            switch(aggregationFunction.ToLowerInvariant())
            {
                case "max":
                    fn = vs => vs.Where(v => v.Value.HasValue).Max(v => v.Value);
                    break;
                case "min":
                    fn = vs => vs.Where(v => v.Value.HasValue).Min(v => v.Value);
                    break;
                case "avg":
                case "average":
                    fn = vs => vs.Where(v => v.Value.HasValue).Average(v => v.Value);
                    break;
                case "sum":
                    fn = vs => vs.Where(v => v.Value.HasValue).Sum(v => v.Value);
                    break;
            } // switch

            return
                metrics.Select(m => {
                    var datapoints = 
                        m.Series.Datapoints.
                            GroupBy(d => (long)TimeSpan.FromTicks(d.Timestamp.Ticks).TotalSeconds / (long)bucketDuration.Value.TotalSeconds,
                                (ts, ds) => {
                                    var tst = DateTime.MinValue.AddSeconds(ts * (long)bucketDuration.Value.TotalSeconds);
                                    var dpt = fn(ds);

                                    return new Datapoint(tst, dpt);
                                }).ToList();

                    return new Metric(m.Name, new Series(m.Series.From, m.Series.Until, bucketDuration.Value, datapoints));
                }).
                ToArray();
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

        [Function("ema")]
        [Function("exponential_moving_average")]
        public static Metric[] Ema(EvalContext context, Metric[] metrics, int smoothingFactor)
        {
            var sf = 1d / smoothingFactor;

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
                            ema = sf * prevV + (1 - sf) * prevEma;
                            prevV = v.Value;
                            prevEma = ema;

                            return ema;
                        } // else
                    }));
                }).
                ToArray();
        }
    }
}
