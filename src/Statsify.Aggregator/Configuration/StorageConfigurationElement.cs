using System.Configuration;

namespace Statsify.Aggregator.Configuration
{
    public class StorageConfigurationElement : ConfigurationElement
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

        [ConfigurationProperty("retention", IsRequired = true)]
        public string Retention
        {
            get { return (string)this["retention"]; }
            set { this["retention"] = value; }
        }

    }
}