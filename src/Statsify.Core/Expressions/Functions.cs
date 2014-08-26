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

    public class Metric
    {
        public string Name { get; private set; }

        public Series Series { get; private set; }

        public Metric(string name, Series series)
        {
            Name = name;
            Series = series;
        }
    }

    public class EvalContext
    {
        public DateTime From { get; private set; }

        public DateTime Until { get; private set; }
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
