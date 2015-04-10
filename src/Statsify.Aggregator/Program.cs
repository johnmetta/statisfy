using System.Reflection;
using NLog;
using Statsify.Aggregator.Configuration;
using Statsify.Core.Expressions;
using Statsify.Core.Storage;
using Topshelf;
using Environment = Statsify.Core.Expressions.Environment;

namespace Statsify.Aggregator
{
    class Program
    {
        private static readonly Logger Log = LogManager.GetLogger("Statsify.Aggregator");

        static int Main(string[] args)
        {
            Log.Info("starting up");

            //
            // Validate configuration on startup
            var configurationManager = ConfigurationManager.Instance;
            foreach(StoreConfigurationElement storage in configurationManager.Configuration.Storage)
            {
                var retentionPolicy = new RetentionPolicy(storage.Retentions);
                RetentionPolicyValidator.EnsureRetentionPolicyValid(retentionPolicy);
            } // foreach

            Environment.RegisterFunctions(typeof(Functions));

            var host = 
                HostFactory.New(x => {

                    x.Service<StatsifyAggregatorService>(sc => {
                        sc.ConstructUsing(hostSettings => new StatsifyAggregatorService(hostSettings, configurationManager));
                        sc.WhenStarted((service, hostControl) => service.Start(hostControl));
                        sc.WhenStopped((service, hostControl) => service.Stop(hostControl));
                    });

                    x.SetServiceName("statsify-aggregator");
                    x.SetDisplayName("Statsify Aggregator v" + Application.Version.ToString(2));
                    x.SetDescription("Listens to StatsD-compatible UDP datagrams and aggregates and stores metrics sent to it.");

                    x.StartAutomaticallyDelayed();

                    x.RunAsNetworkService();
                });

            return (int)host.Run();
        }
    }
}
