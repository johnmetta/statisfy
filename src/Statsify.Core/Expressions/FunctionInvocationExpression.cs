using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

namespace Statsify.Core.Expressions
{
    [DebuggerDisplay("{Name,nq}()")]
    public class FunctionInvocationExpression : Expression
    {
        public string Name { get; private set; }

        public ReadOnlyCollection<Expression> Parameters { get; private set; }

        public FunctionInvocationExpression(string name, IEnumerable<Expression> parameters)
        {
            Name = name;
            Parameters = new ReadOnlyCollection<Expression>(new List<Expression>(parameters));
        }

        public override object Evaluate(Environment environment, EvalContext context)
        {
            var parameters = Parameters.Select(p => p.Evaluate(environment, context)).ToArray();
            var function = environment.ResolveFunction(Name);

            return function.Invoke(environment, context, parameters);
        }
    }
}