using System;
using NUnit.Framework;
using Statsify.Core.Components.Impl;
using Statsify.Core.Expressions;
using Environment = Statsify.Core.Expressions.Environment;

namespace Statsify.Tests.Core.Expressions
{
    [TestFixture]
    public class ExpressionParserTests
    {
        [Test]
        public void ParseFloat()
        {
            var expression = new ExpressionParser().Parse(new TokenStream(new ExpressionScanner().Scan("-293847.2724379324")));

            Assert.IsInstanceOf<ConstantExpression>(expression);
        }

        [Test]
        public void Parse()
        {
            var scanner = new ExpressionScanner();
            var tokens = scanner.Scan("alias_by_fragment(ema(servers.srv-aps3.system.processor.total*, 50), 2, 4)");

            var parser = new ExpressionParser();
            var expression = parser.Parse(new TokenStream(tokens));
            
            tokens = scanner.Scan("alias_by_fragment(ema(servers.srv-aps3.system.processor.{total*,user*}, 50), 2, 4)");
            expression = parser.Parse(new TokenStream(tokens));

            Environment.RegisterFunctions(typeof(Functions));

            //var metricRegistry = new MetricRegistry(@"c:\statsify");

            //var env = new Environment { MetricRegistry = metricRegistry };
            //var r = expression.Evaluate(env, new EvalContext(DateTime.UtcNow.AddMinutes(-20), DateTime.UtcNow));
        }
    }
}