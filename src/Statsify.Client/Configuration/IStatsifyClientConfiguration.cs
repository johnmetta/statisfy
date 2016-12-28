using System;

namespace Statsify.Client.Configuration
{
    public interface IStatsifyClientConfiguration
    {
        [Obsolete("Use Uri property instead")]
        string Host { get; }

        [Obsolete("Use Uri property instead")]
        int Port { get; }

        string Namespace { get; }

        Uri Uri { get; }
    }
}