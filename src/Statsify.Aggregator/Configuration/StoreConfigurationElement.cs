using System.Configuration;

namespace Statsify.Aggregator.Configuration
{
    public class StoreConfigurationElement : ConfigurationElement
    {
        [ConfigurationProperty("name", IsRequired = true)]
        public string Name
        {
            get { return (string)this["name"]; }
            set { this["name"] = value; }
        }

        [ConfigurationProperty("pattern", IsRequired = true)]
        public string Pattern
        {
            get { return (string)this["pattern"]; }
            set { this["pattern"] = value; }
        }

        [ConfigurationProperty("ignore-pattern", IsRequired = false)]
        public string IgnorePattern
        {
            get { return (string)this["ignore-pattern"]; }
            set { this["ignore-pattern"] = value; }
        }

        [ConfigurationProperty("retentions", IsRequired = true)]
        public RetentionConfigurationElementCollection Retentions
        {
            get { return (RetentionConfigurationElementCollection)this["retentions"]; }
            set { this["retentions"] = value; }
        }
    }
}