using System.Linq;
using NUnit.Framework;
using Statsify.Core.Expressions;

namespace Statsify.Tests.Core.Expressions
{
    [TestFixture]
    public class ExpressionParserTests
    {
        [Test]
        public void ParseFloat()
        {
            var expression = new ExpressionParser().Parse(new TokenStream(new ExpressionScanner().Scan("-293847.2724379324"))).First();
            Assert.IsInstanceOf<ConstantExpression>(expression);
        }

        [Test]
        public void Parse()
        {
            var scanner = new ExpressionScanner();
            var parser = new ExpressionParser();

            var tokens = scanner.Scan("alias_by_fragment(ema(servers.srv-aps3.system.processor.total*, 50), 2, 4)");
            var expressions = parser.Parse(new TokenStream(tokens)).ToArray();

            Assert.AreEqual(1, expressions.Length);
            
            tokens = scanner.Scan("summarize(alias_by_fragment(ema(servers.srv-aps3.system.processor.{total*,user*}, 50), 2, 4), 'sum', '1d', false)");
            expressions = parser.Parse(new TokenStream(tokens)).ToArray();

            Assert.AreEqual(1, expressions.Length);

            tokens = scanner.Scan(
                "aliasByNode(sortByName(servers.mow1aps13.system.network.intel*.bytes_sent_sec), 1, 5)," +
                "scale(aliasByNode(sortByName(servers.mow1aps13.system.network.intel*.bytes_received_sec), 1, 5), -1)");

            expressions = parser.Parse(new TokenStream(tokens)).ToArray();
            
            Assert.AreEqual(2, expressions.Length);
            Assert.IsInstanceOf<FunctionInvocationExpression>(expressions[0]);
            Assert.IsInstanceOf<FunctionInvocationExpression>(expressions[1]);
        }
    }
}