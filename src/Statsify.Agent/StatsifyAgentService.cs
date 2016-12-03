using System;
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
        private readonly Logger log = LogManager.GetCurrentClassLogger();

        private MetricCollector metricCollector;
        private MetricPublisher metricPublisher;
        private IStatsifyClient statsifyClient;

        public StatsifyAgentService(ConfigurationManager configurationManager)
        {
            configuration = configurationManager.Configuration;
        }

        public bool Start(HostControl hostControl)
        {
            log.Info("starting up");

            var statsify = configuration.Statsify;

            var @namespace = statsify.Namespace;
            if(!string.IsNullOrWhiteSpace(@namespace))
                @namespace += ".";

            @namespace += Environment.MachineName.ToLowerInvariant();

            var uri = statsify.Uri;
            if(uri != null && !string.IsNullOrWhiteSpace(uri.OriginalString))
            {
                log.Trace("configuring StatsifyClient with uri: {0}, namespace: '{1}', collection interval: '{2}' ",
                    statsify.Uri, @namespace, configuration.Metrics.CollectionInterval);

                var statsifyChannelFactory = new StatsifyChannelFactory();
                var statsifyChannel = statsifyChannelFactory.CreateChannel(uri);

                statsifyClient = new StatsifyClient(@namespace, statsifyChannel);
            } // if
            else
            {
                log.Trace("configuring StatsifyClient with host: {0}, port: {1}, namespace: '{2}', collection interval: '{3}' ", 
                    configuration.Statsify.Host, configuration.Statsify.Port, @namespace, configuration.Metrics.CollectionInterval);

                statsifyClient = new UdpStatsifyClient(statsify.Host, statsify.Port, @namespace);
            } // else

            metricCollector = new MetricCollector(configuration.Metrics);
            
            metricPublisher = new MetricPublisher(metricCollector, statsifyClient, configuration.Metrics.CollectionInterval);
            metricPublisher.Start();

            return true;
        }

        public bool Stop(HostControl hostControl)
        {
            log.Info("stopping service");

            metricPublisher.Stop();

            log.Info("stopped service");

            return true;
        }
    }
}