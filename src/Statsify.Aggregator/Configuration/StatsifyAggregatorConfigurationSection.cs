using System.Configuration;
using System.Xml;

namespace Statsify.Aggregator.Configuration
{
    public sealed class StatsifyAggregatorConfigurationSection : ConfigurationSection
    {
        [ConfigurationProperty("endpoint", IsRequired = true)]
        public EndpointElement Endpoint
        {
            get { return (EndpointElement)this["endpoint"]; }
            set { this["endpoint"] = value; }
        }

        [ConfigurationProperty("storage", IsRequired = true)]
        public StorageConfigurationElementCollection Storage
        {
            get { return (StorageConfigurationElementCollection)this["storage"]; }
            set { this["storage"] = value; }
        }

        [ConfigurationProperty("downsampling", IsRequired = true)]
        public DownsampleConfigurationElementCollection Downsampling
        {
            get { return (DownsampleConfigurationElementCollection)this["downsampling"]; }
            set { this["downsampling"] = value; }
        }

        /*[ConfigurationProperty("aggregation", IsRequired = true)]
        public AggregationConfigurationElementCollection Aggregation
        {
            get { return (AggregationConfigurationElementCollection)this["aggregation"]; }
            set { this["aggregation"] = value; }
        }*/

        public StatsifyAggregatorConfigurationSection(XmlReader xmlReader)
        {
            DeserializeSection(xmlReader);
        }

        public StatsifyAggregatorConfigurationSection(){}
    }
}