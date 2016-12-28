using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Statsify.Client
{
    public static class MetricNameBuilder
    {
        private static readonly Regex ReservedCharactersRegex;

        static MetricNameBuilder()
        {
            var reservedCharacters = new HashSet<char> { ':', '|', '@', ' ', '\n', '\r', '\t', '\\', };

            foreach(var invalidChar in Path.GetInvalidFileNameChars().Union(Path.GetInvalidPathChars()))
                reservedCharacters.Add(invalidChar);

            var pattern = 
                string.Format(
                    "[{0}]", 
                    string.Join("", reservedCharacters.Select(c => Regex.Escape(c.ToString(CultureInfo.InvariantCulture)))));

            ReservedCharactersRegex = new Regex(pattern, RegexOptions.Compiled);
        }

        public static string BuildMetricName(string @namespace, string name)
        {
            var metric = name.Trim('.').Trim(' ');
            if(!string.IsNullOrWhiteSpace(@namespace))
                metric = string.Format("{0}.{1}", @namespace.Trim('.').Trim(' '), metric);

            metric = SanitizeMetricName(metric);

            return metric;
        }

        public static string SanitizeMetricName(string metric)
        {
            metric = ReservedCharactersRegex.Replace(metric, "_");
            return metric;
        }
    }
}