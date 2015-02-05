using System;
using System.Linq;
using NUnit.Framework;
using Statsify.Core.Expressions;
using Statsify.Core.Model;

namespace Statsify.Tests.Core.Expressions
{
    [TestFixture]
    public class FunctionsTests
    {
        [Test]
        public void Derivative()
        {
            var from = DateTime.UtcNow.AddMinutes(-1);
            var to = DateTime.UtcNow;

            var metrics = new [] {
                new Metric(
                    "values", 
                    new Series(@from, to, TimeSpan.FromSeconds(1), new[] {
                        new Datapoint(DateTime.UtcNow, 1), 
                        new Datapoint(DateTime.UtcNow, 2), 
                        new Datapoint(DateTime.UtcNow, 3), 
                        new Datapoint(DateTime.UtcNow, null),
                        new Datapoint(DateTime.UtcNow, 4), 
                        new Datapoint(DateTime.UtcNow, 5), 
                    } ))
            };

            var result = Functions.Derivative(new EvalContext(@from, to), metrics);

            foreach(var d in result[0].Series.Datapoints)
                Console.WriteLine("* " + d.Value);
        }

        [Test]
        public void KeepLastValue()
        {
            var from = DateTime.UtcNow.AddMinutes(-1);
            var to = DateTime.UtcNow;

            var metrics = new [] {
                new Metric(
                    "values", 
                    new Series(@from, to, TimeSpan.FromSeconds(1), new[] {
                        new Datapoint(DateTime.UtcNow, null), 
                        new Datapoint(DateTime.UtcNow, 1), 
                        new Datapoint(DateTime.UtcNow, 2), 
                        new Datapoint(DateTime.UtcNow, 3), 
                        new Datapoint(DateTime.UtcNow, null),
                        new Datapoint(DateTime.UtcNow, null),
                        new Datapoint(DateTime.UtcNow, 4), 
                        new Datapoint(DateTime.UtcNow, null),
                        new Datapoint(DateTime.UtcNow, 5), 
                    } ))
            };

            var result = Functions.KeepLastValue(new EvalContext(@from, to), metrics);

            CollectionAssert.AreEqual(
                new double?[] { null, 1, 2, 3, 3, 3, 4, 4, 5 },
                result[0].Series.Datapoints.Select(d => d.Value).ToArray());
        }
    }
}
