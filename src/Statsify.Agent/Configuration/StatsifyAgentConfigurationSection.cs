using System.Configuration;
using System.Xml;

namespace Statsify.Agent.Configuration
{
    public class StatsifyAgentConfigurationSection : ConfigurationSection
    {
        [ConfigurationProperty("statsify", IsRequired = true)]
        public StatsifyConfigurationElement Statsify
        {
            get { return (StatsifyConfigurationElement)this["statsify"]; }
            set { this["statsify"] = value; }
        }

        [ConfigurationProperty("metrics", IsRequired = true)]
        public MetricsConfigurationElementCollection Metrics
        {
            get { return (MetricsConfigurationElementCollection)this["metrics"]; }
            set { this["metrics"] = value; }
        }

        public StatsifyAgentConfigurationSection(XmlReader xmlReader)
        {
            DeserializeSection(xmlReader);
        }
    }
}