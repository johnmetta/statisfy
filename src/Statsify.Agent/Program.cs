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
            var version = Assembly.GetExecutingAssembly().GetName().Version;

            Log.Info("starting up");

            var configurationManager = ConfigurationManager.Instance;

            var host =
                HostFactory.New(x => {
                    x.Service<StatsifyAgentService>(sc => {
                        sc.ConstructUsing(() => new StatsifyAgentService(configurationManager));
                        sc.WhenStarted((service, hostControl) => service.Start(hostControl));
                        sc.WhenStopped((service, hostControl) => service.Stop(hostControl));
                        sc.WhenShutdown((service, hostControl) => service.Shutdown(hostControl));
                    });

                    x.SetServiceName("statsify-agent");
                    x.SetDisplayName("Statsify Agent " + version.ToString(2));
                    x.SetDescription("Collects machine-level metrics and sends them off to Statsify Aggregator or any StatsD-compatible server.");

                    x.StartAutomaticallyDelayed();

                    x.RunAsNetworkService();
                });

            return (int)host.Run();
        }
    }
}
