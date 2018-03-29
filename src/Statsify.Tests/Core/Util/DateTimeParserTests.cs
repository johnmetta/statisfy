using System;
using System.Collections.Generic;
using System.Globalization;
using NUnit.Framework;
using Statsify.Core.Util;

namespace Statsify.Tests.Core.Util
{
    [TestFixture]
    public class DateTimeParserTests
    {
        [Test]
        [TestCaseSource("GetParseDateTimeTestCases")]
        public DateTime ParseDateTime(string text, DateTime now, DateTime @default)
        {
            return DateTimeParser.ParseDateTime(text, now, @default);
        }

        private static IEnumerable<TestCaseData> GetParseDateTimeTestCases()
        {
            var now = new DateTime(2016, 7, 21, 15, 42, 15, DateTimeKind.Utc);
            var @default = now.AddDays(-1);

            var timestamp = now.AddHours(1).ToUnixTimestamp();

            var testCases = new[] {
                new TestCaseData(null, now, @default).Returns(@default),
                new TestCaseData("", now, @default).Returns(@default),

                new TestCaseData("-30m", now, @default).Returns(now.AddMinutes(-30)),

                new TestCaseData(timestamp.ToString(CultureInfo.InvariantCulture), 
                    now, @default).Returns(now.AddHours(1)),

                new TestCaseData("2016-07-21T16:42:15", 
                    now, @default).Returns(now.AddHours(1).ToLocalTime()),
            };

            return testCases;
        } 
    }
}
