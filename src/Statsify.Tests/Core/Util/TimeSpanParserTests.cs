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
        public TimeSpan TryParseTimeSpan(string text, DateTime? now)
        {
            TimeSpan timeSpan;
            TimeSpanParser.TryParseTimeSpan(text, out timeSpan, now);

            return timeSpan;
        }

        private IEnumerable<TestCaseData> GetTryParseTimeSpanTestCases()
        {
            var now = new DateTime(2016, 7, 21, 15, 42, 15, DateTimeKind.Utc);

            var testCases = new[] {
                new TestCaseData("", null).Returns(TimeSpan.MinValue),

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

                new TestCaseData("1s", null).Returns(TimeSpan.FromSeconds(1)),
                new TestCaseData("-1s", null).Returns(TimeSpan.FromSeconds(-1)),

                new TestCaseData("10s", null).Returns(TimeSpan.FromSeconds(10)),
                new TestCaseData("-10s", null).Returns(TimeSpan.FromSeconds(-10)),

                new TestCaseData("10m", null).Returns(TimeSpan.FromMinutes(10)),
                new TestCaseData("-10m", null).Returns(TimeSpan.FromMinutes(-10)),

                new TestCaseData("10h", null).Returns(TimeSpan.FromHours(10)),
                new TestCaseData("-10h", null).Returns(TimeSpan.FromHours(-10)),

                new TestCaseData("10d", null).Returns(TimeSpan.FromDays(10)),
                new TestCaseData("-10d", null).Returns(TimeSpan.FromDays(-10)),

                new TestCaseData("10w", null).Returns(TimeSpan.FromDays(70)),
                new TestCaseData("-10w", null).Returns(TimeSpan.FromDays(-70)),

                new TestCaseData("3y", null).Returns(TimeSpan.FromDays(1095.75)),
                new TestCaseData("-3y", null).Returns(TimeSpan.FromDays(-1095.75)),

                new TestCaseData("3y", now).Returns(now.AddYears(3) - now),
                new TestCaseData("-3y", now).Returns(now - now.AddYears(3)),

                new TestCaseData("10y", null).Returns(TimeSpan.FromDays(3652.5)),
                new TestCaseData("-10y", null).Returns(TimeSpan.FromDays(-3652.5))
            };

            return testCases;
        } 
    }
}
