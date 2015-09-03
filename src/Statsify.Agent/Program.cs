using System;
using System.Reflection;
using NLog;
using Statsify.Agent.Configuration;
using Topshelf;

namespace Statsify.Agent
{
    class Program
    {
        private static readonly Logger Log = LogManager.GetLogger("Statsify.Agent");

        static int Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += AppDomainUnhandledExceptionHandler;

            var version = Assembly.GetExecutingAssembly().GetName().Version;
            var serviceDisplayName = "Statsify Agent v" + version.ToString(3);

            Log.Info("starting up " + serviceDisplayName);

            var configurationManager = ConfigurationManager.Instance;

            var host =
                HostFactory.New(x => {
                    x.Service<StatsifyAgentService>(sc => {
                        sc.ConstructUsing(() => new StatsifyAgentService(configurationManager));
                        sc.WhenStarted((service, hostControl) => service.Start(hostControl));
                        sc.WhenStopped((service, hostControl) => service.Stop(hostControl));
                    });

                    x.SetServiceName("statsify-agent");
                    x.SetDisplayName(serviceDisplayName);
                    x.SetDescription("Collects machine-level metrics and sends them off to Statsify Aggregator or any StatsD-compatible server.");

                    x.StartAutomaticallyDelayed();

                    x.RunAsNetworkService();
                });

            return (int)host.Run();
        }

        private static void AppDomainUnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
            Log.FatalException("unhandled exception", e.ExceptionObject as Exception);
        }
    }
}
