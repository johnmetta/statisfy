using System.Configuration;
using System.Xml;

namespace Statsify.Aggregator.Configuration
{
    public sealed class StatsifyAggregatorConfigurationSection : ConfigurationSection
    {
        private const string UdpEndpointPropertyName = "udp-endpoint";
        private const string ApiEndpointPropertyName = "api-endpoint";

        [ConfigurationProperty(UdpEndpointPropertyName, IsRequired = true)]
        public UdpEndpointElement UdpEndpoint
        {
            get { return (UdpEndpointElement)this[UdpEndpointPropertyName]; }
            set { this[UdpEndpointPropertyName] = value; }
        }

        [ConfigurationProperty(ApiEndpointPropertyName, IsRequired = true)]
        public ApiEndpointElement ApiEndpoint
        {
            get { return (ApiEndpointElement)this[ApiEndpointPropertyName]; }
            set { this[ApiEndpointPropertyName] = value; }
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

        public StatsifyAggregatorConfigurationSection()
        {
        }
    }
}