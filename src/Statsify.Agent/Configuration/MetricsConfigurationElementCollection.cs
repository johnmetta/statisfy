using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;

namespace Statsify.Agent.Configuration
{
    [ConfigurationCollection(typeof(MetricConfigurationElement), AddItemName = "metric", CollectionType = ConfigurationElementCollectionType.BasicMap)]
    public class MetricsConfigurationElementCollection : ConfigurationElementCollection, IEnumerable<MetricConfigurationElement>
    {
        [ConfigurationProperty("collection-interval", IsRequired = false, DefaultValue = "00:00:05")]
        public TimeSpan CollectionInterval
        {
            get { return (TimeSpan)this["collection-interval"]; }
            set { this["collection-interval"] = value; }
        }

        public MetricConfigurationElement this[int index]
        {
            get { return (MetricConfigurationElement)BaseGet(index); }
            set
            {
                if(BaseGet(index) != null)
                    BaseRemoveAt(index);

                base.BaseAdd(index, value);
            }
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new MetricConfigurationElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((MetricConfigurationElement)element).Name;
        }

        public override ConfigurationElementCollectionType CollectionType
        {
            get { return ConfigurationElementCollectionType.BasicMap; }
        }

        protected override string ElementName
        {
            get { return "metric"; }
        }

        IEnumerator<MetricConfigurationElement> IEnumerable<MetricConfigurationElement>.GetEnumerator()
        {
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var value in ((IEnumerable)this))
            {
                var element = (MetricConfigurationElement)value;
                if (element != null)
                    yield return element;
            }      
        }
    }
}