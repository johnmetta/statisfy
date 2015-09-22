using System;
using NLog;
using Statsify.Aggregator.Configuration;
using Statsify.Core.Expressions;
using Statsify.Core.Storage;
using Topshelf;

namespace Statsify.Aggregator
{
    class Program
    {
        private static readonly Logger Log = LogManager.GetLogger("Statsify.Aggregator");

        static int Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += AppDomainUnhandledExceptionHandler;

            var serviceDisplayName = "Statsify Aggregator v" + Application.Version.ToString(3);

            Log.Info("starting up " + serviceDisplayName);

            //
            // Validate configuration on startup
            var configurationManager = ConfigurationManager.Instance;
            foreach(StoreConfigurationElement storage in configurationManager.Configuration.Storage)
            {
                var retentionPolicy = new RetentionPolicy(storage.Retentions);
                RetentionPolicyValidator.EnsureRetentionPolicyValid(retentionPolicy);
            } // foreach

            Core.Expressions.Environment.RegisterFunctions(typeof(Functions));

            var host = 
                HostFactory.New(x => {

                    x.Service<StatsifyAggregatorService>(sc => {
                        sc.ConstructUsing(hostSettings => new StatsifyAggregatorService(hostSettings, configurationManager));
                        sc.WhenStarted((service, hostControl) => service.Start(hostControl));
                        sc.WhenStopped((service, hostControl) => service.Stop(hostControl));
                    });

                    x.SetServiceName("statsify-aggregator");
                    x.SetDisplayName(serviceDisplayName);
                    x.SetDescription("Listens to StatsD-compatible UDP datagrams and aggregates and stores metrics sent to it.");

                    x.StartAutomaticallyDelayed();

                    x.RunAsNetworkService();

                    x.UseNLog();
                });

            return (int)host.Run();
        }

        private static void AppDomainUnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
            Log.FatalException("unhandled exception", e.ExceptionObject as Exception);
        }
    }
}
