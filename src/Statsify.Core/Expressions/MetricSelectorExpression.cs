using System.Diagnostics;

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
            var from = context.From;
            var until = context.Until;
            var metricNames = environment.MetricRegistry.ResolveMetricNames(Expression.Selector);

            var metrics = MetricReader.ReadMetrics(environment, metricNames, from, until);
            return metrics.ToArray();
        }
    }
}