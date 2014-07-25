namespace Statsify.Web.Api.Configuration
{
    using System.Configuration;
    using System.Xml;

    internal sealed class StatsifyConfigurationSection : ConfigurationSection
    {
        [ConfigurationProperty("storage", IsRequired = true)]
        public StorageConfigurationElement Storage
        {
            get { return (StorageConfigurationElement)this["storage"]; }
            set { this["storage"] = value; }
        }
      
        public StatsifyConfigurationSection(XmlReader xmlReader)
        {
            DeserializeSection(xmlReader);
        }
    }
}