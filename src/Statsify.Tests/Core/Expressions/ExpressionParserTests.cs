using System;
using NUnit.Framework;
using Statsify.Core.Expressions;
using Environment = Statsify.Core.Expressions.Environment;

namespace Statsify.Tests.Core.Expressions
{
    [TestFixture]
    public class ExpressionParserTests
    {
        [Test]
        public void Parse()
        {
            var scanner = new ExpressionScanner();
            var tokens = scanner.Scan("alias_by_fragment(servers.*.system.processor.total*, 2, 4)");

            var parser = new ExpressionParser();
            var expression = parser.Parse(new TokenStream(tokens));

            Environment.RegisterFunction("abs", new Function(typeof(Functions).GetMethod("Abs")));
            Environment.RegisterFunction("alias_by_fragment", new Function(typeof(Functions).GetMethod("AliasByFragment")));

            var env = new Environment { SeriesReader = new X(@"c:\statsify"), MetricProvider = new X(@"c:\statsify") };
            var r = expression.Evaluate(env, new EvalContext(DateTime.UtcNow.AddHours(-1), DateTime.UtcNow));
        }
    }
}