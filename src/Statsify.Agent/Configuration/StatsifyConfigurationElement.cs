﻿using System.Configuration;

namespace Statsify.Agent.Configuration
{
    public class StatsifyConfigurationElement : ConfigurationElement
    {
        [ConfigurationProperty("host", IsRequired = true)]
        public string Host
        {
            get { return (string)this["host"]; }
            set { this["host"] = value; }
        }

        [ConfigurationProperty("port", IsRequired = true)]
        public int Port
        {
            get { return (int)this["port"]; }
            set { this["port"] = value; }
        }

        [ConfigurationProperty("namespace", IsRequired = true)]
        public string Namespace
        {
            get { return (string)this["namespace"]; }
            set { this["namespace"] = value; }
        }
    }
}