using System;
using System.Configuration;
using System.Threading;
using Statsify.Client.Configuration;

namespace Statsify.Client
{
    /// <summary>
    /// Provides a simplified interface to Statsify by creating and configuring <see cref="UdpStatsifyClient"/> with settings from <c>App.config</c>/<c>Web.config</c> behind the scenes. 
    /// </summary>
    public static class Stats
    {
        private static readonly ThreadLocal<IStatsifyClient> StatsifyClient = new ThreadLocal<IStatsifyClient>(GetStatsifyClient);
        
        private static IStatsifyClient Statsify
        {
            get { return StatsifyClient.Value; }
        }
        
        public static void Increment(string metric, double sample = 1)
        {
            Statsify.Increment(metric, sample);
        }

        public static void Decrement(string metric, double sample = 1)
        {
            Statsify.Decrement(metric, sample);
        }

        public static void Counter(string metric, double value, double sample = 1)
        {
            Statsify.Counter(metric, value, sample);
        }

        public static void Gauge(string metric, double value, double sample = 1)
        {
            Statsify.Gauge(metric, value, sample);
        }

        public static void GaugeDiff(string metric, double value, double sample = 1)
        {
            Statsify.GaugeDiff(metric, value, sample);
        }

        public static void Time(string metric, double value, double sample = 1)
        {
            Statsify.Time(metric, value, sample);
        }

        public static void Time(string metric, Action action, double sample = 1)
        {
            Statsify.Time(metric, action, sample);
        }

        private static IStatsifyClient GetStatsifyClient()
        {
            var configuration = ConfigurationManager.GetSection("statsify") as StatsifyConfigurationSection;
            if(configuration == null) throw new ConfigurationErrorsException();

            var statsify = new UdpStatsifyClient(configuration.Host, configuration.Port, configuration.Namespace);
            return statsify;
        }
    }
}