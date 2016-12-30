using System;
using System.ComponentModel;
using System.Configuration;

namespace Statsify.Aggregator.Configuration
{
    public class ApdexConfigurationElement : ConfigurationElement
    {
        [ConfigurationProperty("metric", IsRequired = true)]
        public string Metric
        {
            get { return (string)this["metric"]; }
            set { this["metric"] = value; }
        }

        [ConfigurationProperty("threshold", IsRequired = true)]
        [TypeConverter(typeof(TimeSpanConfigurationConverter))]
        public TimeSpan Threshold
        {
            get { return (TimeSpan)this["threshold"]; }
            set { this["threshold"] = value; }
        }
    }
}