namespace Statsify.Core.Expressions
{
    public abstract class Expression
    {
        public virtual object Evaluate(Environment environment, EvalContext context)
        {
            return null;
        }
    }
}