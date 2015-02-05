using System.Diagnostics;

namespace Statsify.Core.Expressions
{
    [DebuggerDisplay("{Value,nq}")]
    public class ConstantExpression : Expression
    {
        public object Value { get; private set; }

        public ConstantExpression(object value)
        {
            Value = value;
        }

        public override object Evaluate(Environment environment, EvalContext context)
        {
            return Value;
        }
    }
}