using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;
using NLog;
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

        private readonly Logger log = LogManager.GetCurrentClassLogger();

        public StatsifyAgentService(ConfigurationManager configurationManager)
        {
            configuration = configurationManager.Configuration;
        }

        public bool Start(HostControl hostControl)
        {
            log.Info("starting up");

            var @namespace = configuration.Statsify.Namespace;
            if(!string.IsNullOrWhiteSpace(@namespace))
                @namespace += ".";

            @namespace += Environment.MachineName.ToLowerInvariant();

            log.Trace("configuring StatsifyClient with host: {0}, port: {1}, namespace: '{2}'", configuration.Statsify.Host, configuration.Statsify.Port, @namespace);

            statsifyClient = new UdpStatsifyClient(configuration.Statsify.Host, configuration.Statsify.Port, @namespace);

            log.Info("creating metrics");

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
                var metric = m.Name;
                var value = m.GetNextValue();

                log.Trace("publishing metric '{0}' with value '{1}'", metric, value);

                switch(m.AggregationStrategy)
                {
                    case AggregationStrategy.Gauge:
                        statsifyClient.Gauge(metric, value);
                        break;
                    case AggregationStrategy.Counter:
                        statsifyClient.Counter(metric, value);
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

        private PerformanceCounter ParsePerformanceCounter(string s)
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
                log.Error("could not create performance counter: {0}", e.Message);
                return null;
            }

            return pc;
        }
    }
}