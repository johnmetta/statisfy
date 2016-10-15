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

                new TestCaseData("s", null).Returns(TimeSpan.MinValue),
                new TestCaseData("m", null).Returns(TimeSpan.MinValue),
                new TestCaseData("h", null).Returns(TimeSpan.MinValue),
                new TestCaseData("d", null).Returns(TimeSpan.MinValue),
                new TestCaseData("w", null).Returns(TimeSpan.MinValue),
                new TestCaseData("y", null).Returns(TimeSpan.MinValue),

                new TestCaseData("1", null).Returns(TimeSpan.MinValue),
                new TestCaseData("-1", null).Returns(TimeSpan.MinValue),

                new TestCaseData("0s", null).Returns(TimeSpan.FromSeconds(0)),
                new TestCaseData("-0s", null).Returns(TimeSpan.FromSeconds(-0)),

                new TestCaseData("1s", null).Returns(TimeSpan.FromSeconds(1)),
                new TestCaseData("-1s", null).Returns(TimeSpan.FromSeconds(-1)),

                new TestCaseData("10s", null).Returns(TimeSpan.FromSeconds(10)),
                new TestCaseData("-10s", null).Returns(TimeSpan.FromSeconds(-10)),

                new TestCaseData("10m", null).Returns(TimeSpan.FromMinutes(10)),
                new TestCaseData("-10m", null).Returns(TimeSpan.FromMinutes(-10)),

                new TestCaseData("10min", null).Returns(TimeSpan.FromMinutes(10)),
                new TestCaseData("-10min", null).Returns(TimeSpan.FromMinutes(-10)),

                new TestCaseData("10h", null).Returns(TimeSpan.FromHours(10)),
                new TestCaseData("-10h", null).Returns(TimeSpan.FromHours(-10)),

                new TestCaseData("10d", null).Returns(TimeSpan.FromDays(10)),
                new TestCaseData("-10d", null).Returns(TimeSpan.FromDays(-10)),

                new TestCaseData("10w", null).Returns(TimeSpan.FromDays(70)),
                new TestCaseData("-10w", null).Returns(TimeSpan.FromDays(-70)),

                new TestCaseData("10M", null).Returns(TimeSpan.FromDays(TimeSpanParser.AvgDaysInMonth * 10)),
                new TestCaseData("-10M", null).Returns(TimeSpan.FromDays(TimeSpanParser.AvgDaysInMonth * -10)),

                new TestCaseData("10M", now).Returns(now.AddMonths(10) - now),
                new TestCaseData("-10M", now).Returns(now - now.AddMonths(10)),

                new TestCaseData("10mon", now).Returns(now.AddMonths(10) - now),
                new TestCaseData("-10mon", now).Returns(now - now.AddMonths(10)),

                new TestCaseData("3y", null).Returns(TimeSpan.FromDays(TimeSpanParser.AvgDaysInYear * 3)),
                new TestCaseData("-3y", null).Returns(TimeSpan.FromDays(TimeSpanParser.AvgDaysInYear * -3)),

                new TestCaseData("3y", now).Returns(now.AddYears(3) - now),
                new TestCaseData("-3y", now).Returns(now - now.AddYears(3)),

                new TestCaseData("10y", null).Returns(TimeSpan.FromDays(TimeSpanParser.AvgDaysInYear * 10)),
                new TestCaseData("-10y", null).Returns(TimeSpan.FromDays(TimeSpanParser.AvgDaysInYear * -10))
            };

            return testCases;
        } 
    }
}
