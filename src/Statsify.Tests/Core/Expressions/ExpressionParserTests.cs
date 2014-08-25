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
            var tokens = scanner.Scan("asPercent(Server01.connections.{failed,succeeded}, Server01.connections.attempted, 5, \"85\")");

            var parser = new ExpressionParser();
            var expression = parser.Parse(new TokenStream(tokens));
        }
    }
}