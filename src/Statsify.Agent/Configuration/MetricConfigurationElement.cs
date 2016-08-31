using System;
using System.ComponentModel;
using System.Configuration;

namespace Statsify.Agent.Configuration
{
    public class MetricConfigurationElement : ConfigurationElement, IMetricConfiguration
    {
        [ConfigurationProperty("name", IsRequired = true)]
        public string Name
        {
            get { return (string)this["name"]; }
            set { this["name"] = value; }
        }

        [ConfigurationProperty("type", IsRequired = true)]
        public string Type
        {
            get { return (string)this["type"]; }
            set { this["type"] = value; }
        }

        [ConfigurationProperty("path", IsRequired = true)]
        public string Path
        {
            get { return (string)this["path"]; }
            set { this["path"] = value; }
        }

        [ConfigurationProperty("aggregation-strategy", IsRequired = true)]
        [TypeConverter(typeof(EnumConfigurationConverter<AggregationStrategy>))]
        public AggregationStrategy AggregationStrategy
        {
            get { return (AggregationStrategy)this["aggregation-strategy"]; }
            set { this["aggregation-strategy"] = value; }
        }

        [ConfigurationProperty("refresh-every", IsRequired = false)]
        public TimeSpan? RefreshEvery
        {
            get { return (TimeSpan?)this["refresh-every"]; }
            set { this["refresh-every"] = value; }
        }
    }
}