using System;
using System.Linq;
using System.Threading;
using NLog;
using Statsify.Agent.Configuration;
using Statsify.Agent.Impl;
using Statsify.Client;
using Topshelf;

namespace Statsify.Agent
{
    public class StatsifyAgentService
    {
        private readonly StatsifyAgentConfigurationSection configuration;

        private MetricCollector metricCollector;
        private MetricPublisher metricPublisher;
        private IStatsifyClient statsifyClient;

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

            metricCollector = new MetricCollector(configuration.Metrics.OfType<MetricConfigurationElement>());
            metricPublisher = new MetricPublisher(metricCollector, statsifyClient, configuration.Metrics.CollectionInterval);

            metricPublisher.Start();
            
            return true;
        }

        public bool Stop(HostControl hostControl)
        {
            metricPublisher.Stop();

            hostControl.Stop();

            return true;
        }

        public void Shutdown(HostControl hostControl)
        {
            metricPublisher.Stop();

            hostControl.Stop();
        }
    }
}