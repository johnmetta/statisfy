namespace Statsify.Client.Configuration
{
    public interface IStatsifyClientConfiguration
    {
        string Host { get; }

        int Port { get; }

        string Namespace { get; }
    }
}