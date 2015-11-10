using System;
using System.Net;
using Statsify.Client.Configuration;

namespace Statsify.Client
{
    public class StatsifyClientFactory
    {
        private static readonly IStatsifyClient NullStatsifyClient = new NullStatsifyClient();
        private readonly Func<string, string> environmentVariableResolver;
        private readonly Func<string, bool> hostnameValidator; 

        public StatsifyClientFactory() :
            this(null, null)
        {
        }

        public StatsifyClientFactory(Func<string, string> environmentVariableResolver, Func<string, bool> hostnameValidator)
        {
            this.hostnameValidator = hostnameValidator ?? ValidateHostname;
            this.environmentVariableResolver = environmentVariableResolver ?? ResolveEnvironmentVariable;
        }

        public IStatsifyClient CreateStatsifyClient(IStatsifyClientConfiguration configuration)
        {
            if(configuration == null) return NullStatsifyClient;

            var host = GetResolvedValue(configuration.Host);
            var @namespace = GetResolvedValue(configuration.Namespace);

            if(string.IsNullOrWhiteSpace(host) || !hostnameValidator(host))
                return NullStatsifyClient;

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

        private static string ResolveEnvironmentVariable(string value)
        {
            if(string.IsNullOrWhiteSpace(value)) return "";
            
            return Environment.ExpandEnvironmentVariables(value);
        }

        private static bool ValidateHostname(string host)
        {
            try
            {
                var hostAddresses = Dns.GetHostAddresses(host);
                return hostAddresses.Length > 0;
            } // try
            catch(Exception)
            {
                return false;
            } // catch
        }
    }
}