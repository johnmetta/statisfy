namespace Statsify.Web.Api.Configuration
{
    using System.Configuration;

    internal sealed class StorageConfigurationElement : ConfigurationElement
    {
        [ConfigurationProperty("path", IsRequired = true)]
        public string Path
        {
            get { return (string)this["path"]; }
            set { this["path"] = value; }
        }   
    }
}