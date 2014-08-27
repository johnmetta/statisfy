using System.Diagnostics;

namespace Statsify.Core.Expressions
{
    [DebuggerDisplay("{Selector,nq}")]
    internal class MetricSelectorExpression : Expression
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
}