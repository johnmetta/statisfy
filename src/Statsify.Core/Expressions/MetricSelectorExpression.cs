using System.Collections.Generic;
using System.Diagnostics;
using Statsify.Core.Model;

namespace Statsify.Core.Expressions
{
    [DebuggerDisplay("{Selector,nq}")]
    public class MetricSelectorExpression : Expression
    {
        public string Selector { get; private set; }

        public MetricSelectorExpression(string selector)
        {
            Selector = selector;
        }

        public override object Evaluate(Environment environment, EvalContext context)
        {
            return new MetricSelector(Selector, context.From, context.Until);
        }
    }

    public class EvaluatingMetricSelectorExpression : Expression
    {
        public MetricSelectorExpression Expression { get; private set; }

        public EvaluatingMetricSelectorExpression(MetricSelectorExpression expression)
        {
            Expression = expression;
        }

        public override object Evaluate(Environment environment, EvalContext context)
        {
            var metrics = new List<Metric>();

            foreach(var metricName in environment.MetricRegistry.ResolveMetricNames(Expression.Selector))
            {
                var metric = 
                    environment.MetricRegistry.ReadMetric(metricName, context.From, context.Until);

                metrics.Add(metric);
            } // foreach

            return metrics.ToArray();
        }
    }
}