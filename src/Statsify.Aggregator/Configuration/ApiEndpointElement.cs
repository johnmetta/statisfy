using System.Configuration;

namespace Statsify.Aggregator.Configuration
{
    public class ApiEndpointElement : EndpointElement
    {
        [ConfigurationProperty("relative-url", IsRequired = true)]
        public string RelativeUrl
        {
            get { return (string)this["relative-url"]; }
            set { this["relative-url"] = value; }
        }
    }
}