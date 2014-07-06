using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;
using Statsify.Agent.Configuration;
using Statsify.Agent.Impl;
using Statsify.Client;
using Topshelf;
using Topshelf.Runtime;

namespace Statsify.Agent
{
    public class StatsifyAgentService
    {
        private readonly StatsifyAgentConfigurationSection configuration;

        private IList<MetricDefinition> metrics = new List<MetricDefinition>();
        private IStatsifyClient statsifyClient;
        private ManualResetEvent stopEvent;
        private Timer publishingTimer;

        public StatsifyAgentService(ConfigurationManager configurationManager)
        {
            configuration = configurationManager.Configuration;
        }

        public bool Start(HostControl hostControl)
        {
            var @namespace = configuration.Statsify.Namespace;
            if(!string.IsNullOrWhiteSpace(@namespace))
                @namespace += ".";

            @namespace += Environment.MachineName.ToLowerInvariant();

            statsifyClient = new UdpStatsifyClient(configuration.Statsify.Host, configuration.Statsify.Port, @namespace);

            foreach(MetricConfigurationElement m in configuration.Metrics)
            {
                var d = GetMetricDefinition(m);
                if(d != null)
                    metrics.Add(d);
            }

            stopEvent = new ManualResetEvent(false);

            publishingTimer = new Timer(PublishingTimerCallback, null, configuration.Metrics.CollectionInterval, configuration.Metrics.CollectionInterval);
            
            return true;
        }

        public bool Stop(HostControl hostControl)
        {
            stopEvent.Set();

            hostControl.Stop();

            return true;
        }

        public void Shutdown(HostControl hostControl)
        {
            hostControl.Stop();
        }

        private void PublishingTimerCallback(object state)
        {
            foreach(var m in metrics)
            {
                switch(m.AggregationStrategy)
                {
                    case AggregationStrategy.Gauge:
                        statsifyClient.Gauge(m.Name, m.GetNextValue());
                        break;
                    case AggregationStrategy.Counter:
                        statsifyClient.Counter(m.Name, m.GetNextValue());
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                } // switch
            } // foreach
        }

        private MetricDefinition GetMetricDefinition(MetricConfigurationElement m)
        {
            switch(m.Type)
            {
                case "performance-counter":
                    var pc = ParsePerformanceCounter(m.Path);
                    return pc == null ? null : new MetricDefinition(m.Name, () => pc.NextValue(), m.AggregationStrategy);
                default:
                    throw new Exception();
            } // switch
        }

        public static Regex r = new Regex(@"(\\\\(?<computer>([^\\]+)))?(\\(?<object>([^\\]+)))\\(?<counter>(.+))", RegexOptions.Compiled | RegexOptions.Singleline);

        private static PerformanceCounter ParsePerformanceCounter(string s)
        {
            var pc = new PerformanceCounter();

            var m = r.Match(s);
            if (!string.IsNullOrWhiteSpace(m.Groups["computer"].Value))
                pc.MachineName = m.Groups["computer"].Value;

            var category = m.Groups["object"].Value;

            if(category.Contains("("))
            {
                var categoryName = category.Substring(0, category.IndexOf("(", StringComparison.Ordinal)).Trim();

                pc.CategoryName = categoryName;
                pc.InstanceName = category.Substring(category.IndexOf("(", StringComparison.Ordinal)).Trim('(', ')');
            }
            else
            {
                pc.CategoryName = category;
            }

            pc.CounterName = m.Groups["counter"].Value;

            try
            {
                pc.NextValue();
            }
            catch (Exception e)
            {
                Console.WriteLine("could not create " + s + " " + e);
                return null;
            }
            return pc;
        }
    }
}