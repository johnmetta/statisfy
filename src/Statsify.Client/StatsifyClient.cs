using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Statsify.Client
{
    public class StatsifyClient : IStatsifyClient
    {
        private static readonly Random Sampler = new Random();

        private readonly string @namespace;
        private readonly IStatsifyChannel statsifyChannel;
        private readonly MetricSerializer metricSerializer = new MetricSerializer();

        public StatsifyClient(string @namespace, IStatsifyChannel statsifyChannel)
        {
            this.@namespace = @namespace;
            this.statsifyChannel = statsifyChannel;
        }

        public void Counter(string metric, double value, double sample = 1)
        {
            Publish(Metric.Counter(metric, value, sample));
        }

        public void Gauge(string metric, double value, double sample = 1)
        {
            Publish(Metric.Gauge(metric, value, sample));
        }

        public void GaugeDiff(string metric, double value, double sample = 1)
        {
            Publish(Metric.GaugeDiff(metric, value, sample));
        }

        public void Time(string metric, double value, double sample = 1)
        {
            Publish(Metric.Time(metric, value, sample));
        }

        public void Set(string metric, string value, double sample = 1)
        {
            Publish(Metric.Set(metric, value, sample));
        }

        public void Annotation(string title, string message)
        {
            throw new System.NotImplementedException();
        }

        public void Batch(IEnumerable<Metric> metrics)
        {
            if(!statsifyChannel.SupportsBatchedWrites)
            {
                foreach(var metric in metrics)
                    Publish(metric);
            } // if
            else
            {
                var payload = string.Join("\n", 
                    metrics.
                        Select(m => metricSerializer.SerializeMetric(@namespace, m)));

                var buffer = Encoding.UTF8.GetBytes(payload);

                statsifyChannel.WriteBuffer(buffer);
            } // else
        }

        private void Publish(Metric metric)
        {
            if(metric.Sample < 1 && metric.Sample < Sampler.NextDouble()) return;

            var payload = metricSerializer.SerializeMetric(@namespace, metric);
            var buffer = Encoding.UTF8.GetBytes(payload);

            statsifyChannel.WriteBuffer(buffer);
        }
    }
}
