using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;

namespace Statsify.Aggregator.Configuration
{
    [ConfigurationCollection(typeof(StoreConfigurationElement), CollectionType = ConfigurationElementCollectionType.BasicMap)]
    public class StorageConfigurationElementCollection : ConfigurationElementCollection, IEnumerable<StoreConfigurationElement>
    {
        [ConfigurationProperty("path", IsRequired = true)]
        public string Path
        {
            get { return (string)this["path"]; }
            set { this["path"] = value; }
        }

        [ConfigurationProperty("flush-interval", IsRequired = true)]
        public TimeSpan FlushInterval
        {
            get { return (TimeSpan)this["flush-interval"]; }
            set { this["flush-interval"] = value; }
        }

        public StoreConfigurationElement this[int index]
        {
            get { return (StoreConfigurationElement)BaseGet(index); }

            set
            {
                if(BaseGet(index) != null)
                    BaseRemoveAt(index);

                base.BaseAdd(index, value);
            }
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new StoreConfigurationElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((StoreConfigurationElement)element).Name;
        }

        public override ConfigurationElementCollectionType CollectionType
        {
            get { return ConfigurationElementCollectionType.BasicMap; }
        }

        protected override string ElementName
        {
            get { return "store"; }
        }

        IEnumerator<StoreConfigurationElement> IEnumerable<StoreConfigurationElement>.GetEnumerator()
        {
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var value in ((IEnumerable)this))
            {
                var element = (StoreConfigurationElement)value;
                if (element != null)
                    yield return element;
            }            
        }
    }
}