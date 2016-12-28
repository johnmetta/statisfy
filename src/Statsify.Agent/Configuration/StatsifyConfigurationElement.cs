using System;
using System.Configuration;

namespace Statsify.Agent.Configuration
{
    public class StatsifyConfigurationElement : ConfigurationElement
    {
        [ConfigurationProperty("host", IsRequired = false)]
        public string Host
        {
            get { return (string)this["host"]; }
            set { this["host"] = value; }
        }

        [ConfigurationProperty("port", IsRequired = false)]
        public int Port
        {
            get { return (int)this["port"]; }
            set { this["port"] = value; }
        }

        [ConfigurationProperty("uri", IsRequired = false, DefaultValue = "")]
        public Uri Uri
        {
            get { return (Uri)this["uri"]; }
            set { this["uri"] = value; }
        }

        [ConfigurationProperty("namespace", IsRequired = true)]
        public string Namespace
        {
            get { return (string)this["namespace"]; }
            set { this["namespace"] = value; }
        }
    }
}