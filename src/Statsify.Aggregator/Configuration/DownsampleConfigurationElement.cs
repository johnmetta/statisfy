using System.ComponentModel;
using System.Configuration;

using Statsify.Core.Storage;

namespace Statsify.Aggregator.Configuration
{
    public class DownsampleConfigurationElement : ConfigurationElement
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

        [ConfigurationProperty("factor", IsRequired = true)]
        public float Factor
        {
            get { return (float)this["factor"]; }
            set { this["factor"] = value; }
        }

        [ConfigurationProperty("method", IsRequired = true)]
        [TypeConverter(typeof(EnumConfigurationConverter<DownsamplingMethod>))]
        public DownsamplingMethod Method
        {
            get { return (DownsamplingMethod)this["method"]; }
            set { this["method"] = value; }
        }
    }
}