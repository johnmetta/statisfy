using System;
using System.Collections.Generic;
using NUnit.Framework;
using Statsify.Core.Util;

namespace Statsify.Tests.Core.Util
{
    [TestFixture]
    public class TimeSpanParserTests
    {
        [Test]
        [TestCaseSource("GetTryParseTimeSpanTestCases")]
        public TimeSpan TryParseTimeSpan(string text)
        {
            TimeSpan timeSpan;
            TimeSpanParser.TryParseTimeSpan(text, out timeSpan);

            return timeSpan;
        }

        private IEnumerable<TestCaseData> GetTryParseTimeSpanTestCases()
        {
            var testCases = new[] {
                new TestCaseData("").Returns(TimeSpan.MinValue),

                new TestCaseData("s").Returns(TimeSpan.MinValue),
                new TestCaseData("m").Returns(TimeSpan.MinValue),
                new TestCaseData("h").Returns(TimeSpan.MinValue),
                new TestCaseData("d").Returns(TimeSpan.MinValue),
                new TestCaseData("w").Returns(TimeSpan.MinValue),
                new TestCaseData("y").Returns(TimeSpan.MinValue),

                new TestCaseData("1").Returns(TimeSpan.MinValue),
                new TestCaseData("-1").Returns(TimeSpan.MinValue),

                new TestCaseData("0s").Returns(TimeSpan.FromSeconds(0)),
                new TestCaseData("-0s").Returns(TimeSpan.FromSeconds(-0)),

                new TestCaseData("1s").Returns(TimeSpan.FromSeconds(1)),
                new TestCaseData("-1s").Returns(TimeSpan.FromSeconds(-1)),

                new TestCaseData("10s").Returns(TimeSpan.FromSeconds(10)),
                new TestCaseData("-10s").Returns(TimeSpan.FromSeconds(-10)),

                new TestCaseData("10m").Returns(TimeSpan.FromMinutes(10)),
                new TestCaseData("-10m").Returns(TimeSpan.FromMinutes(-10)),

                new TestCaseData("10h").Returns(TimeSpan.FromHours(10)),
                new TestCaseData("-10h").Returns(TimeSpan.FromHours(-10)),

                new TestCaseData("10d").Returns(TimeSpan.FromDays(10)),
                new TestCaseData("-10d").Returns(TimeSpan.FromDays(-10)),

                new TestCaseData("10w").Returns(TimeSpan.FromDays(70)),
                new TestCaseData("-10w").Returns(TimeSpan.FromDays(-70)),

                new TestCaseData("10y").Returns(TimeSpan.FromDays(3652.5)),
                new TestCaseData("-10y").Returns(TimeSpan.FromDays(-3652.5))
            };

            return testCases;
        } 
    }
}
