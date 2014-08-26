using NUnit.Framework;
using Statsify.Core.Expressions;

namespace Statsify.Tests.Core.Expressions
{
    [TestFixture]
    public class ExpressionParserTests
    {
        [Test]
        public void Parse()
        {
            var scanner = new ExpressionScanner();
            var tokens = scanner.Scan("alias_by_fragment(a.bb.ccc.ddddd.eeeee, 0, 2, 4)");

            var parser = new ExpressionParser();
            var expression = parser.Parse(new TokenStream(tokens));

            Environment.RegisterFunction("abs", new Function(typeof(Functions).GetMethod("Abs")));
            Environment.RegisterFunction("alias_by_fragment", new Function(typeof(Functions).GetMethod("AliasByFragment")));

            var env = new Environment();
            var r = expression.Evaluate(env);
        }
    }
}