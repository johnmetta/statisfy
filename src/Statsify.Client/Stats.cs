using System;
using System.Configuration;
using System.Threading;
using Statsify.Client.Configuration;

namespace Statsify.Client
{
    /// <summary>
    /// Provides a simplified interface to Statsify by creating and configuring <see cref="IStatsifyClient"/> with settings from
    /// <c>App.config</c>/<c>Web.config</c>/Environment Variables behind the scenes. 
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>Stats</c> expects either a configuration section named <c>statsify</c> with a required <see cref="StatsifyClientConfiguration.Host"/> property 
    /// (<c>@host</c> attribute) or <c>STATSIFY_HOST</c> Environment Variable.
    /// </para>
    /// <para>
    /// To initialize <see cref="Stats"/> using standard .NET configuration infrastructure, add the following entries to <c>App.config</c>/<c>Web.config</c>:
    /// <code>
    /// &lt;configuration> 
    ///   &lt;configSections>
    ///     &lt;section name=&quot;statsify&quot; 
    ///              type=&quot;Statsify.Client.Configuration.StatsifyConfigurationSection, Statsify.Client&quot; />
    ///   &lt;/configSections>
    ///   
    ///   &lt;!-- ... -->
    ///   
    ///   &lt;statsify host=&quot;127.0.0.1&quot; port=&quot;...&quot; namespace=&quot;...&quot; />
    /// &lt;/configuration>
    /// </code>
    /// <see cref="StatsifyConfigurationSection.Host"/> and <see cref="StatsifyConfigurationSection.Namespace"/> can use
    /// environment variables - for example:
    /// <code>
    /// &lt;statsify host="%STATSIFY_HOST%" ... />
    /// </code>
    /// </para>
    /// <para>Alternatively, <see cref="Stats"/> can use Environment Variables to configure itself. It expects at least <c>STATSIFY_HOST</c> Environment Variable
    /// and can also use <c>STATSIFY_PORT</c> and <c>STATSIFY_NAMESPACE</c>.</para>
    /// <para>
    /// If a <see cref="IStatsifyClient"/> cannot be configured properly, a no-op <see cref="NullStatsifyClient"/> is used.
    /// </para>
    /// </remarks>
    public static class Stats
    {
        private static readonly ThreadLocal<IStatsifyClient> StatsifyClient = new ThreadLocal<IStatsifyClient>(GetStatsifyClient);

        internal static Func<string, string> EnvironmentVariableResolver { get; set; }
        internal static Func<StatsifyConfigurationSection> ConfigurationSectionResolver { get; set; }
        
        private static IStatsifyClient Statsify
        {
            get { return StatsifyClient.Value; }
        }

        static Stats()
        {
            EnvironmentVariableResolver = Environment.GetEnvironmentVariable;
            ConfigurationSectionResolver = () => ConfigurationManager.GetSection("statsify") as StatsifyConfigurationSection;
        }
        
        /// <summary>
        /// Increment a counter <paramref name="metric"/> by <c>1</c>.
        /// </summary>
        /// <param name="metric"></param>
        /// <param name="sample"></param>
        public static void Increment(string metric, double sample = 1)
        {
            Statsify.Increment(metric, sample);
        }

        /// <summary>
        /// Decrement a counter <paramref name="metric"/> by <c>1</c>.
        /// </summary>
        /// <param name="metric"></param>
        /// <param name="sample"></param>
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

        public static T Time<T>(string metric, Func<T> action, double sample = 1)
        {
            var result = default(T);
            Statsify.Time(metric, () => { result = action(); }, sample);

            return result;
        }

        internal static IStatsifyClient GetStatsifyClient()
        {
            var clientFactory = new StatsifyClientFactory();
            var configuration = GetConfigStatsifyConfiguration() ?? GetEnvironmentStatsifyConfiguration();
            
            var statsifyClient = clientFactory.CreateStatsifyClient(configuration);
            return statsifyClient;
        }

        internal static IStatsifyClientConfiguration GetConfigStatsifyConfiguration()
        {
            var configuration = ConfigurationSectionResolver() as IStatsifyClientConfiguration;
            return configuration;
        }

        internal static IStatsifyClientConfiguration GetEnvironmentStatsifyConfiguration()
        {
            var host = EnvironmentVariableResolver("STATSIFY_HOST");
            if(string.IsNullOrWhiteSpace(host)) return null;

            //
            // int.TryParse() will set @port to 0 if it fails to parse
            var port = UdpStatsifyClient.DefaultPort;
            if(!int.TryParse(EnvironmentVariableResolver("STATSIFY_PORT"), out port))
                port = UdpStatsifyClient.DefaultPort;

            var @namespace = EnvironmentVariableResolver("STATSIFY_NAMESPACE");

            return new StatsifyClientConfiguration(host, port, @namespace);
        }
    }
}