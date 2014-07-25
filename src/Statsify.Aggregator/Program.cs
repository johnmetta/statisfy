using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Statsify.Aggregator.Configuration;
using Statsify.Core.Storage;
using Topshelf;

namespace Statsify.Aggregator
{
    class Program
    {
        static int Main(string[] args)
        {
            var configurationManager = new ConfigurationManager();

            //
            // Validate configuration on startup
            foreach(StorageConfigurationElement storage in configurationManager.Configuration.Storage)
            {
                var retentionPolicy = RetentionPolicy.Parse(storage.Retention);
                RetentionPolicyValidator.EnsureRetentionPolicyValid(retentionPolicy);
            } // foreach

            var host = 
                HostFactory.New(x => {
                    x.Service<StatsifyAggregatorService>(sc => {
                        sc.ConstructUsing(hostSettings => new StatsifyAggregatorService(hostSettings, configurationManager));
                        sc.WhenStarted((service, hostControl) => service.Start(hostControl));
                        sc.WhenStopped((service, hostControl) => service.Stop(hostControl));
                        sc.WhenShutdown((service, hostControl) => service.Shutdown(hostControl));
                    });

                    x.SetServiceName("statsify-aggregator");
                    x.SetDisplayName("Statsify Aggregator");
                    x.SetDescription("Statsify Aggregator aggregates and stores metrics sent to it.");

                    x.StartAutomaticallyDelayed();

                    x.RunAsNetworkService();
                });

            return (int)host.Run();

        }
    }
}
