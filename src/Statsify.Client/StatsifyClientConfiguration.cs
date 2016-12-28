using System;
using Statsify.Client.Configuration;

namespace Statsify.Client
{
    internal class StatsifyClientConfiguration : IStatsifyClientConfiguration
    {
        public string Host { get; private set; }

        public int Port { get; private set; }

        public string Namespace { get; private set; }

        public Uri Uri { get; private set; }

        public StatsifyClientConfiguration(string host, int port, string ns, Uri uri)
        {
            Uri = uri;
            Host = host;
            Port = port;
            Namespace = ns;
        }
    }
}