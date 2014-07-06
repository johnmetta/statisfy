using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Statsify.Aggregator
{
    public class MetricParser
    {
        public IEnumerable<Metric> ParseMetrics(byte[] buffer)
        {
            var datagram = Encoding.UTF8.GetString(buffer);

            var metrics = datagram.Contains("\n") ?
                datagram.Split('\n') :
                new[] { datagram };

            return metrics.Select(ParseMetric).Where(m => m != null);
        }

        public Metric ParseMetric(string metric)
        {
            if(string.IsNullOrWhiteSpace(metric)) return null;

            var bits = metric.Split(':');
            var name = bits[0].RegexReplace(@"\s+", "_").RegexReplace(@"\/", "-").RegexReplace(@"\\", "-").RegexReplace(@"[^a-zA-Z_\-0-9\.]", "").Trim('.');

            var fields = bits[1].Split('|');

            float value;
            if(!float.TryParse(fields[0], NumberStyles.Any, CultureInfo.InvariantCulture, out value))
            {
                Console.WriteLine("couldn't parse '{0}'", metric);
                return null;
            }
            
            var type = GetMetricType(fields[1]);

            var explicitlySigned = fields[0].StartsWith("-") || fields[0].StartsWith("+");
            float sample = 1;

            if(fields.Length == 3)
                sample = float.Parse(fields[2].Substring(1), CultureInfo.InvariantCulture);

            return new Metric { Name = name, Type = type, Value = value, Sample = sample, ExplicitlySigned = explicitlySigned };
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
            } // switch
        }
    }
}