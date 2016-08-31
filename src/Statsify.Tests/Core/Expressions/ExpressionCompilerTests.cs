using System;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using Statsify.Core.Expressions;

namespace Statsify.Tests.Core.Expressions
{
    [TestFixture]
    public class ExpressionCompilerTests
    {
        [Test]
        public void Parse()
        {
            var sources = new[] {
                "summarize(alias_by_fragment(ema(servers.srv-aps3.system.processor.{total*,user*}, 50), 2, 4), 'sum', '1d', false)",
                "sort_by_name(alias_by_fragment(summarize(servers.*.system.processor.total_time, \"max\", \"10m\"), 1, 4))"
            };

            const int N = 10000;

            var expressionCompiler = new ExpressionCompiler();
            var expressions = expressionCompiler.Parse(sources[0]).ToList();

            var stopwatch = Stopwatch.StartNew();

            for(var n = 0; n < N; ++n)
                foreach(var source in sources)
                    expressions = expressionCompiler.Parse(source).ToList();

            Console.WriteLine(stopwatch.Elapsed);
        }
    }
}