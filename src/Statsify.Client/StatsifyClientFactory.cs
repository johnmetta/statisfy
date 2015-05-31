using System;
using Statsify.Client.Configuration;

namespace Statsify.Client
{
    public class StatsifyClientFactory
    {
        private readonly Func<string, string> environmentVariableResolver;

        public StatsifyClientFactory() :
            this(null)
        {
        }

        public StatsifyClientFactory(Func<string, string> environmentVariableResolver)
        {
            this.environmentVariableResolver = environmentVariableResolver ?? Environment.GetEnvironmentVariable;
        }

        public IStatsifyClient CreateStatsifyClient(IStatsifyClientConfiguration configuration)
        {
            if(configuration == null) return new NullStatsifyClient();

            var host = GetResolvedValue(configuration.Host);
            var @namespace = GetResolvedValue(configuration.Namespace);

            if(string.IsNullOrWhiteSpace(host))
                return new NullStatsifyClient();

            var statsify = new UdpStatsifyClient(host, configuration.Port, @namespace);
            return statsify;
        }

        private string GetResolvedValue(string value)
        {
            if(string.IsNullOrWhiteSpace(value)) return "";
            if(!value.StartsWith("%")) return value;

            value = environmentVariableResolver(value);
            if(string.IsNullOrWhiteSpace(value) || value.StartsWith("%")) return "";

            return value;
        }
    }
}