using System;
using System.Globalization;

namespace Statsify.Client
{
    class MetricSerializer
    {
        private const string MetricDatagramFormat = "{0}:{1}|{2}";
        private const string SampledMetricDatagramFormat = MetricDatagramFormat + "|@{3:N3}";

        private readonly CultureInfo cultureInfo = CultureInfo.InvariantCulture;

        public string SerializeMetric(string @namespace, Metric metric)
        {
            var type = GetMetricTypeSpecifier(metric.Type);

            var doubleMetricValue = metric.Value as MetricValue.DoubleMetricValue;
            if(doubleMetricValue != null)
            {
                var datagram = SerializeMetric(@namespace, metric.Name, type, doubleMetricValue.Value, metric.Sample,
                    doubleMetricValue.ExplicitlySigned);

                return datagram;
            } // if

            var stringMetricValue = metric.Value as MetricValue.StringMetricValue;
            if(stringMetricValue != null)
            {
                var datagram = SerializeMetric(@namespace, metric.Name, type, stringMetricValue.Value,  metric.Sample);

                return datagram;
            } // if

            throw new InvalidOperationException();
        }

        public string SerializeMetric(string @namespace, string metric, string type, double value,
            double sample, bool explicitlySigned = false)
        {
            if(string.IsNullOrWhiteSpace(metric)) throw new ArgumentException("metric");
            if(string.IsNullOrWhiteSpace(type)) throw new ArgumentException("type");
            if(sample < 0 || sample > 1) throw new ArgumentOutOfRangeException("sample");

            var metricValueFormat = explicitlySigned ? "{0:+#.####;-#.####;#}" : "{0}";
            var metricValue =
                Math.Abs(value) < 0.00000001 ?
                    (explicitlySigned ? "+0" : "0") :
                    string.Format(cultureInfo, metricValueFormat, (float)value);

            var datagram = SerializeMetric(@namespace, metric, type, metricValue, sample);

            return datagram;
        }

        public string SerializeMetric(string @namespace, string metric, string type, string value,
            double sample)
        {
            if(string.IsNullOrWhiteSpace(metric)) throw new ArgumentException("metric");
            if(string.IsNullOrWhiteSpace(type)) throw new ArgumentException("type");
            if(sample < 0 || sample > 1) throw new ArgumentOutOfRangeException("sample");

            metric = MetricNameBuilder.BuildMetricName(@namespace, metric);
            var format = sample < 1 ? SampledMetricDatagramFormat : MetricDatagramFormat;

            var datagram = string.Format(cultureInfo, format, metric, value, type, sample);

            return datagram;
        }

        private static string GetMetricTypeSpecifier(MetricType metricType)
        {
            switch(metricType)
            {
                case MetricType.Counter:
                    return  "c";
                case MetricType.Gauge:
                case MetricType.GaugeDiff:
                    return "g";
                case MetricType.Time:
                    return "ms";
                case MetricType.Set:
                    return "s";
                default:
                    throw new ArgumentOutOfRangeException();
            } // switch
        }
    }
}
