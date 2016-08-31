using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Statsify.Core.Expressions
{
    public class ExpressionCompiler
    {
        private static readonly ExpressionScanner ExpressionScanner = new ExpressionScanner();
        private static readonly ConcurrentDictionary<string, IList<Expression>> ExpressionsCache =
            new ConcurrentDictionary<string, IList<Expression>>();

        public IEnumerable<Expression> Parse(string source)
        {
            var key = string.Format("source:{0}", source);

            var result =
                ExpressionsCache.GetOrAdd(
                    key,
                    s => {
                        var tokens = ExpressionScanner.Scan(source);

                        var expressionParser = new ExpressionParser();
                        var expressions = expressionParser.Parse(new TokenStream(tokens)).ToList();

                        return expressions;
                    });

            return result.ToList();
        }
    }
}
