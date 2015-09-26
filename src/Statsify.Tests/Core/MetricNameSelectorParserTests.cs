using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Statsify.Core;

namespace Statsify.Tests.Core
{
    [TestFixture]
    public class MetricNameSelectorParserTests
    {
        [Test]
        [TestCaseSource("GetParseTestCases")]
        public bool Parse(string metricNameSelector, string pattern)
        {
            var matcher = Parse(metricNameSelector);
            return matcher(pattern);
        }

        public static IEnumerable<TestCaseData> GetParseTestCases()
        {
            var testCases = new List<TestCaseData> {
                "".ShouldNotMatch(""),
                "".ShouldNotMatch("abcd"),

                "*".ShouldMatch(""),
                "*".ShouldMatch("abcd")
            };

            testCases.AddRange("{a,bc,def}".ShouldMatchAny("a", "bc", "def"));
            testCases.AddRange("{a,bc,def}".ShouldNotMatchAny("xa", "ab", "b", "bcd", "ef", "defg", "hg"));

            testCases.AddRange("{a,b,c}xyz".ShouldMatchAny("axyz", "bxyz", "cxyz"));
            testCases.AddRange("{a,b,c}xyz".ShouldNotMatchAny("dxyz", "maxyz", "mb"));
            testCases.AddRange("mnp{a,b,c}".ShouldMatchAny("mnpa", "mnpb", "mnpc"));

            testCases.AddRange("mnp{a,b,c}xyz".ShouldMatchAny("mnpaxyz", "mnpbxyz", "mnpcxyz"));
            testCases.AddRange("mnp{a,b,c}xyz".ShouldNotMatchAny("mnpaxy", "npbxyz", "mnpdxyz"));

            testCases.AddRange("[2-4]".ShouldMatchAny("2", "3", "4"));
            testCases.AddRange("[2-4]".ShouldNotMatchAny("0", "1", "22", "23", "24", "34", "5", "6"));

            testCases.AddRange("[2-4ace-]".ShouldMatchAny("2", "3", "4", "a", "c", "e", "-"));
            testCases.AddRange("[-2-4ace]".ShouldMatchAny("2", "3", "4", "a", "c", "e", "-"));

            testCases.AddRange("mnp[2-4]".ShouldMatchAny("mnp2", "mnp3", "mnp4"));
            testCases.AddRange("[2-4]xyz".ShouldMatchAny("2xyz", "3xyz", "4xyz"));
            testCases.AddRange("mnp[2-4]xyz".ShouldMatchAny("mnp2xyz", "mnp3xyz", "mnp4xyz"));

            testCases.AddRange("[2-4q-tz]".ShouldMatchAny("2", "3", "4", "q", "r", "s", "t", "z"));
            testCases.AddRange("[2-4q-tz]".ShouldNotMatchAny("5", "6", "a", "b", "c", "u", "v"));

            testCases.AddRange("[c-f]".ShouldMatchAny("c", "d", "e", "f"));
            testCases.AddRange("[c-f]".ShouldNotMatchAny("a", "b", "g"));
            
            return testCases;
        } 

        private Predicate<string> Parse(string metricNameSelector)
        {
            return new MetricNameSelectorParser(StringComparison.Ordinal).Parse(metricNameSelector).Single();
        }
    }

    internal static class TestCaseDataStringExtensions
    {
        public static TestCaseData ShouldMatch(this string metricNameSelector, string pattern)
        {
            var testCaseData =
                new TestCaseData(metricNameSelector, pattern).
                    SetName(string.Format("'{0}' should match '{1}'", metricNameSelector, pattern)).
                    Returns(true);

            return testCaseData;
        }

        public static IEnumerable<TestCaseData> ShouldMatchAny(this string metricNameSelector, params string[] patterns)
        {
            return patterns.Select(metricNameSelector.ShouldMatch);
        }

        public static TestCaseData ShouldNotMatch(this string metricNameSelector, string pattern)
        {
            var testCaseData =
                new TestCaseData(metricNameSelector, pattern).
                    SetName(string.Format("'{0}' should not match '{1}'", metricNameSelector, pattern)).
                    Returns(false);

            return testCaseData;
        }

        public static IEnumerable<TestCaseData> ShouldNotMatchAny(this string metricNameSelector, params string[] patterns)
        {
            return patterns.Select(metricNameSelector.ShouldNotMatch);
        }
    }

}
