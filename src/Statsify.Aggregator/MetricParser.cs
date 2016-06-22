using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NLog;
using Statsify.Aggregator.Extensions;

namespace Statsify.Aggregator
{
    public class MetricParser
    {
        private readonly Logger log = LogManager.GetCurrentClassLogger();
        private readonly IDictionary<string, string> metricNameCache = new Dictionary<string, string>();
        private static readonly Regex SlashesRegex = new Regex(@"[\/|\\]", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        private static readonly Regex NonAlphanumericRegex = new Regex(@"[^a-zA-Z_\-0-9\.]", RegexOptions.Compiled);
        private static readonly Regex WhitespaceRegex = new Regex(@"\s+", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        public IEnumerable<Metric> ParseMetrics(byte[] buffer)
        {
            var datagram = Encoding.UTF8.GetString(buffer);

            return ParseMetrics(datagram);
        }

        public IEnumerable<Metric> ParseMetrics(string datagram)
        {
            var metrics = datagram.Contains("\n")
                ? datagram.Split('\n')
                : new[] { datagram };

            return metrics.Select(ParseMetric).Where(m => m != null);
        }

        public Metric ParseMetric(string metric)
        {
            if(string.IsNullOrWhiteSpace(metric)) return null;
            
            var bits = metric.Split(':');

            string name;

            if(!metricNameCache.TryGetValue(bits[0], out name))
            {
                name = bits[0].
                    RegexReplace(WhitespaceRegex, "_").
                    RegexReplace(SlashesRegex, "-").
                    RegexReplace(NonAlphanumericRegex, String.Empty).
                    Trim('.');

                metricNameCache[bits[0]] = name;
            } // if

            var fields = bits[1].Split('|');

            var value = fields[0];
            var type = GetMetricType(fields[1]);
            var signed = fields[0].StartsWith("-") || fields[0].StartsWith("+");

            float sample = 1;

            if(fields.Length == 3)
                sample = float.Parse(fields[2].Substring(1), CultureInfo.InvariantCulture);

            return new Metric { Name = name, Type = type, Value = value, Sample = sample };
        }

        private static MetricType GetMetricType(string metricType)
        {
            switch(metricType)
            {
                case "c":
                    return MetricType.Counter;
                case "ms":
                    return MetricType.Timer;
                case "g":
                    return MetricType.Gauge;
                case "s":
                    return MetricType.Set;
                default:
                    throw new ArgumentOutOfRangeException("metricType");
            }
        }
    }
}