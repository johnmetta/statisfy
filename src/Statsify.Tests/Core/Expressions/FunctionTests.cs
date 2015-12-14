using System;
using NUnit.Framework;
using Statsify.Core.Expressions;
using Environment = Statsify.Core.Expressions.Environment;

namespace Statsify.Tests.Core.Expressions
{
    [TestFixture]
    public class FunctionTests
    {
        [Test]
        public void Invoke()
        {
            var fn = new Function(typeof(FunctionTests).GetMethod("Fn"));
            var result = fn.Invoke(new Environment(), new EvalContext(DateTime.UtcNow, DateTime.UtcNow), new [] { "1d" });

            Console.WriteLine(result);
        }

        public static int Fn(EvalContext context, string bucket, bool aligned = true)
        {
            return bucket.Length;
        }
    }
}
