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

        public ReadOnlyCollection<Argument> Arguments { get; private set; }

        public FunctionInvocationExpression(string name, IEnumerable<Argument> arguments)
        {
            Name = name;
            Arguments = new ReadOnlyCollection<Argument>(new List<Argument>(arguments));
        }

        public override object Evaluate(Environment environment, EvalContext context)
        {
            var parameters = Arguments.Select(a => a.Value.Evaluate(environment, context)).ToArray();
            var function = environment.ResolveFunction(Name);

            return function.Invoke(environment, context, parameters);
        }
    }
}