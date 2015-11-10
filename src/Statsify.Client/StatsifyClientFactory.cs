using System;
using Statsify.Client.Configuration;

namespace Statsify.Client
{
    public class StatsifyClientFactory
    {
        private static readonly IStatsifyClient NullStatsifyClient = new NullStatsifyClient();
        private readonly Func<string, string> environmentVariableResolver;

        public StatsifyClientFactory() :
            this(null)
        {
        }

        public StatsifyClientFactory(Func<string, string> environmentVariableResolver)
        {
            this.environmentVariableResolver = environmentVariableResolver ?? ResolveEnvironmentVariable;
        }

        public IStatsifyClient CreateStatsifyClient(IStatsifyClientConfiguration configuration)
        {
            if(configuration == null) return NullStatsifyClient;

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
            if(!value.Contains("%")) return value;

            value = environmentVariableResolver(value);            

            return value;
        }

        public static string ResolveEnvironmentVariable(string value)
        {
            if(string.IsNullOrWhiteSpace(value)) return "";
            
            return Environment.ExpandEnvironmentVariables(value);
        }
    }
}