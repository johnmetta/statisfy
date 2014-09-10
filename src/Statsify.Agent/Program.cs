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
            Log.Info("starting up");

            var configurationManager = new ConfigurationManager();

            var host =
                HostFactory.New(x => {
                    x.Service<StatsifyAgentService>(sc => {
                        sc.ConstructUsing(() => new StatsifyAgentService(configurationManager));
                        sc.WhenStarted((service, hostControl) => service.Start(hostControl));
                        sc.WhenStopped((service, hostControl) => service.Stop(hostControl));
                        sc.WhenShutdown((service, hostControl) => service.Shutdown(hostControl));
                    });

                    x.SetServiceName("statsify-agent");
                    x.SetDisplayName("Statsify Agent");
                    x.SetDescription("Statsify Agent collects machine-level metrics and sends them off to Statsify Aggregator or any Statsd-compatible server.");

                    x.StartAutomaticallyDelayed();

                    x.RunAsNetworkService();
                });

            return (int)host.Run();
        }
    }
}
