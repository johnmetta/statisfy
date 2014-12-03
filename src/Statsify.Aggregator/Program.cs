﻿using System.Reflection;
using NLog;
using Statsify.Aggregator.Configuration;
using Statsify.Core.Storage;
using Topshelf;

namespace Statsify.Aggregator
{
    class Program
    {
        private static readonly Logger Log = LogManager.GetLogger("Statsify.Aggregator");

        static int Main(string[] args)
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;

            Log.Info("starting up");

            var configurationManager = new ConfigurationManager();
            
            // Validate configuration on startup
            foreach(StorageConfigurationElement storage in configurationManager.Configuration.Storage)
            {
                var retentionPolicy = RetentionPolicy.Parse(storage.Retention);
                RetentionPolicyValidator.EnsureRetentionPolicyValid(retentionPolicy);
            }

            var host = 
                HostFactory.New(x => {

                    x.Service<StatsifyAggregatorService>(sc => {
                        sc.ConstructUsing(hostSettings => new StatsifyAggregatorService(hostSettings, configurationManager));
                        sc.WhenStarted((service, hostControl) => service.Start(hostControl));
                        sc.WhenStopped((service, hostControl) => service.Stop(hostControl));
                        sc.WhenShutdown((service, hostControl) => service.Shutdown(hostControl));
                    });

                    x.SetServiceName("statsify-aggregator");
                    x.SetDisplayName("Statsify Aggregator " + version);
                    x.SetDescription("Statsify Aggregator aggregates and stores metrics sent to it.");

                    x.StartAutomaticallyDelayed();

                    x.RunAsNetworkService();
                });

            return (int)host.Run();

        }
    }
}
