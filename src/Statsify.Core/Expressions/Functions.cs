using System;
using System.Linq;
using System.Text;
using Statsify.Core.Model;

namespace Statsify.Core.Expressions
{
    public static class Functions
    {
        [Function("timeshift")]
        public static MetricSelector Timeshift(EvalContext context, MetricSelector selector, string offset)
        {
            var ts = TimeSpan.Parse(offset);
            return new MetricSelector(selector.Selector, selector.From.Add(ts), selector.Until.Add(ts));
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

                        if(n == 0)
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

    public class MetricSelector
    {
        public string Selector { get; private set; }

        public DateTime From { get; private set; }

        public DateTime Until { get; private set; }

        public MetricSelector(string selector, DateTime from, DateTime until)
        {
            Selector = selector;
            From = from;
            Until = until;
        }
    }

    public class Metric2
    {
        public string Name { get; private set; }

        public Series Series { get; private set; }

        public Metric2(string name, Series series)
        {
            Name = name;
            Series = series;
        }
    }

    public class EvalContext
    {
        public DateTime From { get; private set; }

        public DateTime Until { get; private set; }

        public EvalContext(DateTime @from, DateTime until)
        {
            From = @from;
            Until = until;
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class FunctionAttribute : Attribute
    {
        public string Name { get; private set; }

        public FunctionAttribute(string name)
        {
            Name = name;
        }
    }
}
