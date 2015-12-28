using System.Configuration;

namespace Statsify.Client.Configuration
{
    public class StatsifyConfigurationSection : ConfigurationSection, IStatsifyClientConfiguration
    {
        [ConfigurationProperty("host", IsRequired = true)]
        public string Host
        {
            get { return (string)this["host"]; }
            set { this["host"] = value; }
        }

        [ConfigurationProperty("port", IsRequired = false, DefaultValue = UdpStatsifyClient.DefaultPort)]
        public int Port
        {
            get { return (int)this["port"]; }
            set { this["port"] = value; }
        }

        [ConfigurationProperty("namespace", IsRequired = false, DefaultValue = "")]
        public string Namespace
        {
            get { return (string)this["namespace"]; }
            set { this["namespace"] = value; }
        }
    }
}
